using Newtonsoft.Json;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using System.Data.Entity;
using FluentScheduler;
using OceanEduSlide.OEDongBo;
using Z.EntityFramework.Plus;
using OceanEduSlide.Migrations;
using System.Text;
using OceanEduSlide.Utils;

namespace OceanEduSlide.DAL
{
    public class CongNoService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();

        public void SyncCongNo(DateTime day)
        {
            var monthCheck = day.Month;
            var yearCheck = day.Year;
            var dayCheck = day.Day;

            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (config == null || !config.AutoDebt)
            {
                return;
            }
            //Chuẩn bị các list dữ liệu
            var listCongNoDBTrungGian = _dongBoTuyenSinh.BC_CongNo.AsNoTracking().ToList();
            var listMDHOld = _unitOfWork.DebtRepository.GetQuery(a => a.TypeData == TypeData.New).Select(a => a.MaDonHang).ToHashSet();
            var listCongNoTake = listCongNoDBTrungGian.Where(a => !listMDHOld.Contains(a.MaDonHang)).ToList();
            //var listMDHNew = listCongNoTake.Select(a => a.MaDonHang);
            //var listPhieuThu = _dongBoTuyenSinh.BC_PhieuThu.Where(a => listMDHNew.Contains(a.DonHang)).AsNoTracking().ToList();
            var listUser = _unitOfWork.UserRepository.GetQuery().ToList();
            var listOffice = _unitOfWork.OfficeRepository.GetQuery().ToList();

            // Đồng bộ công nợ từ DB trung gian => DB dự án
            TakeCongNoToDataBase(listCongNoTake, listUser, listOffice);

            // Lấy ra các list mới sau khi đồng bộ công nợ từ DB trung gian => DB dự án
            var listCongNoNew = _unitOfWork.DebtRepository.GetQuery(a => a.TypeData == TypeData.New);
            var congNoNewList = listCongNoNew.ToList();
            var listMDHNew = listCongNoNew.Select(a => a.MaDonHang);
            var listPhieuThu = _unitOfWork.PhieuThuRepository.GetQuery(a => listMDHNew.Contains(a.DonHang) && (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm")).AsNoTracking().ToList();

            // Tính toán số tiền đã cọc, bổ sung, còn lại
            CalculateMoney(listCongNoDBTrungGian, congNoNewList, listPhieuThu);

        }
        public void TakeCongNoToDataBase(List<BC_CongNo> listCongNoTake, List<User> listUser, List<Office> listOffice)
        {
            var listCongNo = new List<Debt>();
            foreach (var item in listCongNoTake)
            {
                if (item.NgayLenDon == null)
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai cot NgayLenDon");
                    continue;
                }
                if (!item.TongTien.HasValue)
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai cot TongTien");
                    continue;
                }
                if (string.IsNullOrEmpty(item.MaNVChotSale))
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai cot MaNVChotSale");
                    continue;
                }
                var user = listUser.FirstOrDefault(a => a.MaNhanVien == item.MaNVChotSale);
                if (user == null)
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai MaNhanVien: " + item.MaNVChotSale);
                    continue;
                }
                if (string.IsNullOrEmpty(item.ChiNhanh))
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai cot ChiNhanh");
                    continue;
                }
                item.ChiNhanh = VietnameseCodeHelper.NormalizeVietnameseCode(item.ChiNhanh);
                var office = listOffice.FirstOrDefault(a => a.ShortName == item.ChiNhanh);
                if (office == null)
                {
                    logger.Error("MaDonHang " + item.MaDonHang + ": Khong ton tai CN: " + item.ChiNhanh);
                    continue;
                }
                var congno = new Debt()
                {
                    NgayLenDon = item.NgayLenDon,
                    MaDonHang = item.MaDonHang,
                    NgayPhatSinhCoc = item.NgayLenDon,
                    Month = item.NgayLenDon.Value.Month,
                    Year = item.NgayLenDon.Value.Year,
                    TotalMoney = item.TongTien.Value,
                    StudentCode = item.MaHV,
                    UserOriginId = user.Id,
                    UserId = user.Id,
                    OfficeId = office.Id,
                    TypeData = TypeData.New,
                    TypeDebt = TypeDebt.Type2,
                };
                listCongNo.Add(congno);
            }
            if (listCongNo.Any())
            {
                _unitOfWork.DebtRepository.InsertRange(listCongNo);
            }
            _unitOfWork.Save();
        }
        public void CalculateMoney(List<BC_CongNo> listCongNoDBTrungGian, List<Debt> congNoNewList, List<BC_PhieuThu_DB> listPhieuThu)
        {
            foreach (var item in congNoNewList)
            {
                var phieuThus = listPhieuThu.Where(a => a.DonHang == item.MaDonHang).ToList();
                var tongCoc = 0m;
                var boSungPhi = 0m;

                foreach (var phieuThu in phieuThus)
                {
                    if (phieuThu.Loai == "Đặt cọc")
                        tongCoc += phieuThu.SUD ?? 0;

                    else if (phieuThu.Loai == "Bổ Sung Phí")
                        boSungPhi += phieuThu.SUD ?? 0;
                    else if (phieuThu.Loai == "Học phí" || phieuThu.Loai == "Phiếu gộp")
                        item.TypeDebt = TypeDebt.Type6;

                }
                var daDong = tongCoc + boSungPhi;
                item.DebtMoney = daDong;
                item.RemainMoney = item.TotalMoney - daDong;

            }
            _unitOfWork.Save();
        }



        public async Task SyncCongNoAsync(DateTime day)
        {
            await Task.Run(() =>
            {
                SyncCongNo(day);
            });
        }
    }
}
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

namespace OceanEduSlide.DAL
{
    public class CongNoService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();

        public void SyncPhieuThu()
        {
            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (config == null || !config.AutoDebt)
            {
                return;
            }
            var day = DateTime.Today;

            var phieuThuTakeList = _dongBoTuyenSinh.BC_PhieuThu.Where(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year).AsNoTracking().ToList();
            //var phieuThuKeToanList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month).Select(a => a.PhieuThuKeToan).ToList();
            var oldList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year && !a.THDB);
            oldList.Delete();
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == day.Month && a.Year == day.Year, q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).AsNoTracking().ToList();
            var phieuThuAddList = new List<BC_PhieuThu_DB>();
            foreach (var item in phieuThuTakeList)
            {
                item.MaNVChotSale = item.MaNVChotSale.Replace("'","");
                var historyUser = historyUsers.FirstOrDefault(a => a.User.MaNhanVien == item.MaNVChotSale
                && a.DayStart.Date <= item.NgayThanhToan.Value.Date && (a.DayEnd == null || a.DayEnd.Value.Date >= item.NgayThanhToan.Value.Date));
                if (historyUser == null)
                {
                    logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai nhan su theo thang nao thoa man ngay lam viec: " + item.NgayThanhToan + " va MNV: " + item.MaNVChotSale);
                    continue;
                }
                var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == item.ChiNhanh.Normalize(NormalizationForm.FormC));
                if (office == null)
                {
                    logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai chi nhanh nao co ten ngan la " + item.ChiNhanh);
                    continue;
                }
                var phieuThu = new BC_PhieuThu_DB()
                {
                    PhieuThuKeToan = item.PhieuThuKeToan,
                    ChiNhanh = item.ChiNhanh,
                    MaNVChotSale = item.MaNVChotSale,
                    NgayThanhToan = item.NgayThanhToan,
                    SUD = item.SUD,
                    TUD = item.TUD,
                    Loai = item.Loai,
                    ReceiptCode = item.ReceiptCode,
                    MaHV = item.MaHV,
                    TenHV = item.TenHV,
                    PhanTramUD = item.PhanTramUD,
                    GioiTinh = item.GioiTinh,
                    HinhThucThanhToan = item.HinhThucThanhToan,
                    Notes = item.Notes,
                    DangKy = item.DangKy,
                    GioTao = item.GioTao,
                    ChotSale = item.ChotSale,
                    CongTacVien = item.CongTacVien,
                    ThangHocDuKienDecimal = (decimal?)item.ThangHocDuKien,
                    UD_FINAL = item.UD_FINAL,
                    LoaiCTH = item.LoaiCTH,
                    ChuongTrinhHoc = item.ChuongTrinhHoc,
                    CapDo = item.CapDo,
                    Modun = item.Modun,
                    UD_NhomUDFINAL = item.UD_NhomUDFINAL,
                    HDBH = item.HDBH,
                    DonHang = item.DonHang,
                    UDPhieuThu = item.UDPhieuThu,
                    ThangTinhDThu = item.NgayThanhToan.Value.Month,
                    NamTinhDThu = item.NgayThanhToan.Value.Year,
                    TrangThai = item.TrangThai,
                };
                phieuThuAddList.Add(phieuThu);
            }

            if (phieuThuAddList.Any())
                _unitOfWork.PhieuThuRepository.InsertRange(phieuThuAddList);

            _unitOfWork.Save();
        }


        public async Task SyncPhieuThuAsync()
        {
            await Task.Run(() =>
            {
                SyncPhieuThu();
            });
        }
    }
}
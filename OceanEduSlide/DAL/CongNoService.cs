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
        private DateTime _now;
        private int _thisYear;
        private int _thisMonth;

        public void SyncCongNo(/*DateTime day*/)
        {
            _now = DateTime.Now;
            _thisYear = _now.Year;
            _thisMonth = _now.Month;

            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (config == null || !config.AutoDebt)
            {
                return;
            }
            //Chuẩn bị các list dữ liệu
            var listMDHOld = _unitOfWork.DebtRepository.GetQuery(a => a.TypeData == TypeData.New).Select(a => a.MaDonHang).ToHashSet();
            var listCongNoTake = _dongBoTuyenSinh.BC_CongNo.Where(a => !listMDHOld.Contains(a.MaDonHang)).ToList();
            var listUser = _unitOfWork.UserRepository.GetQuery().ToList();
            var listOffice = _unitOfWork.OfficeRepository.GetQuery().ToList();

            // Đồng bộ công nợ từ DB trung gian => DB dự án
            TakeCongNoToDataBase(listCongNoTake, listUser, listOffice);

            // Lấy ra các list mới sau khi đồng bộ công nợ từ DB trung gian => DB dự án
            var listCongNoNew = _unitOfWork.DebtRepository.GetQuery(a => a.Active && a.TypeData == TypeData.New).ToList();
            var listMDHNew = listCongNoNew.Select(a => a.MaDonHang).ToHashSet();
            var listPhieuThu = _dongBoTuyenSinh.BC_PhieuThu.Where(a => listMDHNew.Contains(a.DonHang) && (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm")).AsNoTracking().ToList();
            // Tính toán số tiền đã cọc, bổ sung, còn lại
            CalculateMoney(listCongNoNew, listPhieuThu);

            //Danh sách công nợ cần thu
            var congNoDuThu = listCongNoNew.Where(a => a.TypeDebt != TypeDebt.Type6 && (a.Year > _thisYear || (a.Year == _thisYear && a.Month >= _thisMonth - 1))).ToList();

            // Tính toán báo cáo dự thu
            if (config.AutoRevenue)
            {
                //Nhân viên
                BaoCaoDuThuNV(congNoDuThu);
                //Chi nhánh
                BaoCaoDuThuCN(congNoDuThu);
            }

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
                    DepositDate = item.NgayLenDon?.ToString("dd/MM/yyyy"),
                    Month = item.NgayLenDon.Value.Month,
                    Year = item.NgayLenDon.Value.Year,
                    TotalMoney = item.TongTien.Value,
                    StudentCode = item.MaHV,
                    UserOriginId = user.Id,
                    UserId = user.Id,
                    OfficeId = office.Id,
                    TypeData = TypeData.New,
                    //TypeDebt = TypeDebt.Type1,
                    Pathway = (decimal)(item.ThangHocDuKien ?? 0),
                };
                listCongNo.Add(congno);
            }
            if (listCongNo.Any())
            {
                _unitOfWork.DebtRepository.InsertRange(listCongNo);
            }
            _unitOfWork.Save();
        }
        public void CalculateMoney(List<Debt> listCongNoNew, List<BC_PhieuThu> listPhieuThu)
        {
            foreach (var item in listCongNoNew)
            {
                var phieuThus = listPhieuThu.Where(a => a.DonHang == item.MaDonHang).ToList();
                var tongCoc = 0m;
                var boSungPhi = 0m;
                var daDong = 0m;
                foreach (var phieuThu in phieuThus)
                {
                    if (phieuThu.Loai == "Đặt cọc")
                        tongCoc += phieuThu.SUD ?? 0;

                    else if (phieuThu.Loai == "Bổ Sung Phí")
                        boSungPhi += phieuThu.SUD ?? 0;
                    else if (phieuThu.Loai == "Học phí" || phieuThu.Loai == "Phiếu gộp")
                    {
                        item.TypeDebt = TypeDebt.Type6;
                        item.GrossDate = phieuThu.NgayThanhToan?.ToString("dd/MM/yyyy");
                    }


                }
                daDong = tongCoc + boSungPhi;
                item.DebtMoney = tongCoc;
                item.DebtMoney2 = boSungPhi;
                item.RemainMoney = item.TotalMoney - daDong;

            }
            _unitOfWork.Save();
        }
        public void BaoCaoDuThuNV(List<Debt> congNoDuThu)
        {
            //Danh sách báo cáo dự thu nhân viên
            var listBCDuThuNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 92).ToList();

            //Danh sách báo cáo %HT dự thu nhân viên
            var listBCHTDuThuNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 93).ToList();

            //Danh sách báo cáo chỉ tiêu Doanh số NV
            var listBCCTDSNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 87).AsNoTracking().ToList();

            //Danh sách báo cáo thực đạt Doanh số NV
            var listBCTDDSNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 88).AsNoTracking().ToList();

            //Danh sách nhân sự tháng đang làm việc
            var historyUserActives = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.Status == StatusUser.Active).AsNoTracking().ToList();

            //Danh sách nhân sự tháng đã điều chuyển - nghỉ việc
            var historyUserNoActives = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.Status != StatusUser.Active).AsNoTracking().ToList();

            var listBCDuThuAdd = new List<ReportData>();

            foreach (var item in historyUserActives)
            {

                // Kết quả dự thu
                var bcDuThuNV = listBCDuThuNV.FirstOrDefault(a => a.HistoryUserId == item.UserId);
                if (bcDuThuNV == null)
                    bcDuThuNV = listBCDuThuAdd.FirstOrDefault(a => a.HistoryUserId == item.UserId && a.ReportCategoryId == 92);
                var duThuNV = congNoDuThu.Where(a => a.UserOriginId == item.UserId).Sum(a => a.TotalMoney);
                var data = duThuNV.ToString("N0");
                if (bcDuThuNV != null)
                {
                    bcDuThuNV.DataReal = duThuNV;
                    bcDuThuNV.Data = data;
                }
                else
                {
                    bcDuThuNV = new ReportData()
                    {
                        OfficeId = item.OfficeId,
                        HistoryUserId = item.Id,
                        ReportCategoryId = 92,
                        Month = _thisMonth,
                        Year = _thisYear,
                        Data = data,
                        DataReal = duThuNV,
                        Sort = 13
                    };
                    listBCDuThuAdd.Add(bcDuThuNV);
                }

                // % HT dự thu
                decimal? HTDT = null;

                var bcCTDSNV = listBCCTDSNV.FirstOrDefault(a => a.HistoryUserId == item.Id);
                if (bcCTDSNV?.DataReal > 0)
                {
                    var bcTDDSNV = listBCTDDSNV.FirstOrDefault(a => a.HistoryUserId == item.Id);
                    var thucDat = bcTDDSNV?.DataReal ?? 0;
                    var tongDuThu = duThuNV + thucDat;
                    HTDT = tongDuThu / bcCTDSNV.DataReal;
                }

                var bcHTDuThuNV = listBCHTDuThuNV.FirstOrDefault(a => a.HistoryUserId == item.UserId);
                if (bcHTDuThuNV == null)
                    bcHTDuThuNV = listBCDuThuAdd.FirstOrDefault(a => a.HistoryUserId == item.UserId && a.ReportCategoryId == 93);
                if (bcHTDuThuNV != null)
                {
                    bcHTDuThuNV.DataReal = HTDT;
                    bcHTDuThuNV.Data = (HTDT * 100)?.ToString("F2") + "%";
                }
                else
                {
                    bcDuThuNV = new ReportData()
                    {
                        OfficeId = item.OfficeId,
                        HistoryUserId = item.Id,
                        ReportCategoryId = 93,
                        Month = _thisMonth,
                        Year = _thisYear,
                        Data = (HTDT * 100)?.ToString("F2") + "%",
                        DataReal = HTDT,
                        Sort = 14
                    };
                    listBCDuThuAdd.Add(bcDuThuNV);
                }
            }

            if (listBCDuThuAdd.Any())
                _unitOfWork.ReportDataRepository.InsertRange(listBCDuThuAdd);

            foreach (var item in historyUserNoActives)
            {
                var bcDuThuNV = listBCDuThuNV.FirstOrDefault(a => a.UserId == item.UserId);
                if (bcDuThuNV != null)
                {
                    bcDuThuNV.DataReal = null;
                    bcDuThuNV.Data = "";
                }

                var bcHTDuThuNV = listBCHTDuThuNV.FirstOrDefault(a => a.UserId == item.UserId);
                if (bcHTDuThuNV != null)
                {
                    bcHTDuThuNV.DataReal = null;
                    bcHTDuThuNV.Data = "";
                }
            }

            _unitOfWork.Save();
        }
        public void BaoCaoDuThuCN(List<Debt> congNoDuThu)
        {
            //Danh sách báo cáo dự thu chi nhánh
            var listBCDuThuCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 50).ToList();

            //Danh sách báo cáo %HT dự thu chi nhánh
            var listBCHTDuThuCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 51).ToList();

            //Danh sách báo cáo chỉ tiêu Doanh số chi nhánh
            var listBCCTDSCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 34).AsNoTracking().ToList();

            //Danh sách báo cáo thực đạt Doanh số chi nhánh
            var listBCTDDSCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == _thisMonth && a.Year == _thisYear && a.ReportCategoryId == 35).AsNoTracking().ToList();

            var listOfficeId = _unitOfWork.OfficeRepository.GetQuery().Select(a => a.Id).ToList();

            var listBCDuThuAdd = new List<ReportData>();
            foreach (var item in listOfficeId)
            {
                var bcDuThuCN = listBCDuThuCN.FirstOrDefault(a => a.OfficeId == item);
                if (bcDuThuCN == null)
                    bcDuThuCN = listBCDuThuAdd.FirstOrDefault(a => a.OfficeId == item && a.ReportCategoryId == 50);
                var duThuCN = congNoDuThu.Where(a => a.OfficeId == item).Sum(a => a.TotalMoney);
                var data = duThuCN.ToString("N0");
                if (bcDuThuCN != null)
                {
                    bcDuThuCN.DataReal = duThuCN;
                    bcDuThuCN.Data = data;
                }
                else
                {
                    bcDuThuCN = new ReportData()
                    {
                        OfficeId = item,
                        ReportCategoryId = 50,
                        Month = _thisMonth,
                        Year = _thisYear,
                        Data = data,
                        DataReal = duThuCN,
                        Sort = 16
                    };
                    listBCDuThuAdd.Add(bcDuThuCN);
                }

                // % HT dự thu
                decimal? HTDT = null;

                var bcCTDSCN = listBCCTDSCN.FirstOrDefault(a => a.OfficeId == item);
                if (bcCTDSCN?.DataReal > 0)
                {
                    var bcTDDSCN = listBCTDDSCN.FirstOrDefault(a => a.OfficeId == item);
                    var thucDat = bcTDDSCN?.DataReal ?? 0;
                    var tongDuThu = duThuCN + thucDat;
                    HTDT = tongDuThu / bcCTDSCN.DataReal;
                }

                var bcHTDuThuCN = listBCHTDuThuCN.FirstOrDefault(a => a.OfficeId == item);
                if (bcHTDuThuCN == null)
                    bcHTDuThuCN = listBCDuThuAdd.FirstOrDefault(a => a.OfficeId == item && a.ReportCategoryId == 51);
                if (bcHTDuThuCN != null)
                {
                    bcHTDuThuCN.DataReal = HTDT;
                    bcHTDuThuCN.Data = (HTDT * 100)?.ToString("F2") + "%";
                }
                else
                {
                    bcDuThuCN = new ReportData()
                    {
                        OfficeId = item,
                        ReportCategoryId = 51,
                        Month = _thisMonth,
                        Year = _thisYear,
                        Data = (HTDT * 100)?.ToString("F2") + "%",
                        DataReal = HTDT,
                        Sort = 17
                    };
                    listBCDuThuAdd.Add(bcDuThuCN);
                }
            }
            if (listBCDuThuAdd.Any())
                _unitOfWork.ReportDataRepository.InsertRange(listBCDuThuAdd);

            _unitOfWork.Save();
        }


        public async Task SyncCongNoAsync(/*DateTime day*/)
        {
            await Task.Run(() =>
            {
                SyncCongNo(/*day*/);
            });
        }
    }
}
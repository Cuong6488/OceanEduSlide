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
    public class PhieuThuService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();

        public void SyncPhieuThu()
        {
            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (config == null || !config.AutoRevenue)
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
            SyncDthu(day.Month, day.Year);
        }

        //Test Sync
        public void TestSyncPhieuThu(int date, int month)
        {

            //var day = DateTime.Today.AddDays(-1);
            // custom day để test
            var day = new DateTime(2025, month, date).Date;
            var phieuThuTakeList = _dongBoTuyenSinh.BC_PhieuThu.Where(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year).AsNoTracking().ToList();
            //var phieuThuKeToanList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month).Select(a => a.PhieuThuKeToan).ToList();
            var oldList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.NgayThanhToan != null && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year && !a.THDB);
            oldList.Delete();
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == day.Month && a.Year == day.Year, q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).AsNoTracking().ToList();
            var phieuThuAddList = new List<BC_PhieuThu_DB>();
            foreach (var item in phieuThuTakeList)
            {
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
            SyncDthu(day.Month, day.Year);
        }
        ////Tự động tính BC CN - NV; thực tế tuấn NV
        //public void SyncDthu(int month, int year)
        //{
        //    var day = new DateTime(year, month, 1);
        //    var phieuThuAllList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.ThangTinhDThu == day.Month && a.NamTinhDThu == day.Year && (a.Loai == "Phiếu gộp" || a.Loai == "Học phí") &&
        //    (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm" || a.TrangThai == null || a.TrangThai == ""));
        //    var phieuThuList = phieuThuAllList.ToList();
        //    var bcList = new List<ReportData>();
        //    var rUserWeek_RealList = new List<RevenueUser_Week_Real>();
        //    var listMaNV = phieuThuAllList.Select(a => a.MaNVChotSale).Distinct().ToList();
        //    var listCN = phieuThuAllList.Select(a => a.ChiNhanh).Distinct().ToList();
        //    var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
        //    foreach (var mnv in listMaNV)
        //    {
        //        //Reset thực đạt NV về 0
        //        var bcnvs = _unitOfWork.ReportDataRepository.GetQuery(a => (a.ReportCategoryId == 88 || a.ReportCategoryId == 96 || a.ReportCategoryId == 103) && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null && a.HistoryUser.User.MaNhanVien == mnv);
        //        foreach (var bcnv in bcnvs)
        //        {
        //            bcnv.Data = "";
        //            bcnv.DataReal = 0;
        //        }

        //        var ttWeeks = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null && a.HistoryUser.User.MaNhanVien == mnv);
        //        foreach (var ttWeek in ttWeeks)
        //        {
        //            ttWeek.TargetBM = 0;
        //        }
        //    }

        //    foreach (var cn in listCN)
        //    {
        //        var office = offices.FirstOrDefault(a => a.Active && a.ShortName == cn);
        //        if (office == null)
        //            continue;
        //        var listIdReset = new List<int> { 35, 40, 43, 119, 78, 80, 82, 84 };
        //        var bccns = _unitOfWork.ReportDataRepository.GetQuery(a => listIdReset.Contains(a.ReportCategoryId) && a.Month == day.Month && a.Year == day.Year && a.Office.ShortName == cn);
        //        foreach (var b in bccns)
        //        {
        //            b.DataReal = 0;
        //            b.Data = "0";
        //        }
        //        //var countHV = phieuThuThongThuong.Where(a => a.ChiNhanh == cn).Select(a => a.MaHV).Distinct().Count();
        //        // Thực đạt HV CN
        //        var countHV = phieuThuList.Where(a => a.ChiNhanh == cn).GroupBy(a => a.MaHV).Where(g => g.Sum(x => x.SUD ?? 0) > 0).Count();

        //        var bcTDHVCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 31 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcTDHVCN == null)
        //            bcTDHVCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 31 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcTDHVCN == null)
        //        {
        //            bcTDHVCN = new ReportData()
        //            {
        //                Sort = 11,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 31,
        //                Data = countHV.ToString("N0"),
        //                OfficeId = office.Id,
        //                DataReal = countHV,
        //            };
        //            bcList.Add(bcTDHVCN);
        //        }
        //        else
        //        {
        //            bcTDHVCN.DataReal = countHV;
        //            bcTDHVCN.Data = countHV.ToString("N0");
        //        }
        //        // % ht báo cáo HV CN
        //        var reportCTHVCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 30).FirstOrDefault();
        //        if (reportCTHVCN?.DataReal > 0)
        //        {
        //            var htHVCN = bcTDHVCN.DataReal / reportCTHVCN.DataReal * 100;
        //            var datahtHVCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 32).FirstOrDefault();
        //            if (datahtHVCN == null)
        //                datahtHVCN = bcList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 32);
        //            if (datahtHVCN == null)
        //            {
        //                datahtHVCN = new ReportData()
        //                {
        //                    Data = (htHVCN ?? 0).ToString("F2") + "%",
        //                    DataReal = htHVCN / 100,
        //                    Month = day.Month,
        //                    Year = day.Month,
        //                    ReportCategoryId = 32,
        //                    OfficeId = office.Id,
        //                    Sort = 12,
        //                };
        //                bcList.Add(datahtHVCN);
        //            }
        //            else
        //            {
        //                datahtHVCN.Data = (htHVCN ?? 0).ToString("F2") + "%";
        //                datahtHVCN.DataReal = htHVCN / 100;
        //            }
        //        }

        //        // BC Học viên GD mới, GD lại

        //        // Lấy danh sách học viên đã ghi danh mới
        //        var maHVGhiDanhMoi = new HashSet<string>(
        //            phieuThuList
        //                .Where(a => a.ChiNhanh == cn && a.DangKy == "Ghi danh mới")
        //                .Select(a => a.MaHV)
        //                .Distinct()
        //        );

        //        // Đếm học viên ghi danh mới
        //        var countHVGDM = phieuThuList
        //            .Where(a => a.ChiNhanh == cn && a.DangKy == "Ghi danh mới")
        //            .GroupBy(a => a.MaHV)
        //            .Where(g => g.Sum(x => x.SUD ?? 0) > 0)
        //            .Count();

        //        // Đếm học viên ghi danh lại (không nằm trong danh sách ghi danh mới)
        //        var countHVGDL = phieuThuList
        //            .Where(a => a.ChiNhanh == cn
        //                     && a.DangKy == "Ghi danh tiếp"
        //                     && !maHVGhiDanhMoi.Contains(a.MaHV))
        //            .GroupBy(a => a.MaHV)
        //            .Where(g => g.Sum(x => x.SUD ?? 0) > 0)
        //            .Count();


        //        var bcHVGDM = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 60 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcHVGDM == null)
        //            bcHVGDM = bcList.FirstOrDefault(a => a.ReportCategoryId == 60 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcHVGDM == null)
        //        {
        //            bcHVGDM = new ReportData()
        //            {
        //                Sort = 28,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 60,
        //                Data = countHVGDM.ToString("N0"),
        //                OfficeId = office.Id,
        //                DataReal = countHVGDM,
        //            };
        //            bcList.Add(bcHVGDM);
        //        }
        //        else
        //        {
        //            bcHVGDM.DataReal = countHVGDM;
        //            bcHVGDM.Data = countHVGDM.ToString("N0");
        //        }

        //        var bcHVGDL = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 58 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcHVGDL == null)
        //            bcHVGDL = bcList.FirstOrDefault(a => a.ReportCategoryId == 58 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcHVGDL == null)
        //        {
        //            bcHVGDL = new ReportData()
        //            {
        //                Sort = 27,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 58,
        //                Data = countHVGDL.ToString("N0"),
        //                OfficeId = office.Id,
        //                DataReal = countHVGDL,
        //            };
        //            bcList.Add(bcHVGDL);
        //        }
        //        else
        //        {
        //            bcHVGDL.DataReal = countHVGDL;
        //            bcHVGDL.Data = countHVGDL.ToString("N0");
        //        }

        //        // Báo cáo Tổng số tháng ĐK, tháng học BQ/HV
        //        var sumSoThangDK = phieuThuList.Where(a => a.ChiNhanh == cn && a.SUD != 0).Sum(a => a.ThangHocDuKienDecimal ?? 0);
        //        decimal? STHBQ1HV = null;
        //        STHBQ1HV = countHV > 0 ? sumSoThangDK / countHV : (decimal?)null;

        //        var bcSoThangDK = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 62 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcSoThangDK == null)
        //            bcSoThangDK = bcList.FirstOrDefault(a => a.ReportCategoryId == 62 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcSoThangDK == null)
        //        {
        //            bcSoThangDK = new ReportData()
        //            {
        //                Sort = 29,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 62,
        //                Data = sumSoThangDK.ToString("N2"),
        //                OfficeId = office.Id,
        //                DataReal = sumSoThangDK,
        //            };
        //            bcList.Add(bcSoThangDK);
        //        }
        //        else
        //        {
        //            bcSoThangDK.DataReal = sumSoThangDK;
        //            bcSoThangDK.Data = sumSoThangDK.ToString("N2");
        //        }

        //        var bcSTHBQ1HV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 64 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcSTHBQ1HV == null)
        //            bcSTHBQ1HV = bcList.FirstOrDefault(a => a.ReportCategoryId == 64 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcSTHBQ1HV == null)
        //        {
        //            bcSTHBQ1HV = new ReportData()
        //            {
        //                Sort = 30,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 64,
        //                Data = countHV > 0 ? (STHBQ1HV ?? 0).ToString("N2") : "",
        //                OfficeId = office.Id,
        //                DataReal = STHBQ1HV,
        //            };
        //            bcList.Add(bcSTHBQ1HV);
        //        }
        //        else
        //        {
        //            bcSTHBQ1HV.DataReal = STHBQ1HV;
        //            bcSTHBQ1HV.Data = countHV > 0 ? (STHBQ1HV ?? 0).ToString("N2") : "";
        //        }

        //        //BQ Ưu đãi sử dụng
        //        var listPhieuThuKhac0d = phieuThuList.Where(a => a.ChiNhanh == cn && a.SUD != 0);
        //        var SUDTotal = listPhieuThuKhac0d.Sum(a => a.SUD);
        //        var TUDTotal = listPhieuThuKhac0d.Sum(a => a.TUD);
        //        if (TUDTotal > 0)
        //        {
        //            var bqUDSD = 1 - (SUDTotal / TUDTotal);
        //            var bcBQUDSDCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 66 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //            if (bcBQUDSDCN == null)
        //                bcBQUDSDCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 66 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //            if (bcBQUDSDCN == null)
        //            {
        //                bcBQUDSDCN = new ReportData()
        //                {
        //                    Sort = 31,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 66,
        //                    Data = ((bqUDSD ?? 0) * 100).ToString("F2") + "%",
        //                    OfficeId = office.Id,
        //                    DataReal = bqUDSD,
        //                };
        //                bcList.Add(bcBQUDSDCN);
        //            }
        //            else
        //            {
        //                bcBQUDSDCN.DataReal = bqUDSD;
        //                bcBQUDSDCN.Data = ((bqUDSD ?? 0) * 100).ToString("F2") + "%";
        //            }
        //        }
        //    }
        //    //List chứa các mã học viên đã được tính
        //    var listMaHV = new List<string>();
        //    foreach (var item in phieuThuList)
        //    {
        //        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == item.ChiNhanh).FirstOrDefault();
        //        if (office == null)
        //        {
        //            logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai chi nhanh nao co ten ngan la " + item.ChiNhanh);
        //            continue;
        //        }

        //        var bcCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 35 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcCN == null)
        //            bcCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 35 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcCN == null)
        //        {
        //            bcCN = new ReportData()
        //            {
        //                Sort = 14,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 35,
        //                Data = (item.SUD ?? 0).ToString("N0"),
        //                DataReal = item.SUD,
        //                OfficeId = office.Id,
        //            };
        //            bcList.Add(bcCN);
        //        }
        //        else
        //        {
        //            decimal DataCN = 0;

        //            if (string.IsNullOrEmpty(bcCN.Data))
        //                bcCN.Data = "0";
        //            if (bcCN.DataReal == null)
        //                bcCN.DataReal = 0;
        //            var cleanedData = bcCN.Data.Replace(",", "").Replace(".", "");

        //            if (decimal.TryParse(cleanedData, out DataCN))
        //            {
        //                DataCN += item.SUD ?? 0;
        //                bcCN.DataReal += item.SUD ?? 0;
        //                bcCN.Data = DataCN.ToString("N0");
        //            }
        //            else
        //            {
        //                // Chuyển đổi thất bại
        //                logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong the chuyen doi thanh so tu nhien ket qua bao cao CN: " + item.ChiNhanh);
        //                continue;
        //            }
        //        }
        //        if (!string.IsNullOrEmpty(item.MaNVChotSale))
        //        {
        //            var historyUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == day.Month && a.Year == day.Year && a.User.MaNhanVien == item.MaNVChotSale
        //                                && DbFunctions.TruncateTime(a.DayStart) <= DbFunctions.TruncateTime(item.NgayThanhToan) && (a.DayEnd == null || DbFunctions.TruncateTime(a.DayEnd) >= DbFunctions.TruncateTime(item.NgayThanhToan)),
        //                                q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).FirstOrDefault();
        //            if (historyUser == null)
        //            {
        //                logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai nhan su theo thang nao thoa man ngay lam viec: " + item.NgayThanhToan + " va MNV: " + item.MaNVChotSale);
        //                continue;
        //            }
        //            // Doanh số sale - đào tạo- kế toán; Thực đạt HV NV
        //            int idTyTrong = 0;
        //            int sortTyTrong = 0;
        //            if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC)
        //            {
        //                if (historyUser.TypeUser != TypeUser.AEC)
        //                {
        //                    idTyTrong = 40;
        //                    sortTyTrong = 18;
        //                }
        //                else
        //                {
        //                    // Xử lý chỉ tiêu HV rỗng cho AEC
        //                    var bcCTHVAEC = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 95 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                    if (bcCTHVAEC == null)
        //                        bcCTHVAEC = bcList.FirstOrDefault(a => a.ReportCategoryId == 95 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                    if (bcCTHVAEC == null)
        //                    {
        //                        bcCTHVAEC = new ReportData()
        //                        {
        //                            Sort = 15,
        //                            Month = day.Month,
        //                            Year = day.Year,
        //                            ReportCategoryId = 95,
        //                            Data = "",
        //                            HistoryUserId = historyUser.Id,
        //                        };
        //                        bcList.Add(bcCTHVAEC);
        //                    }
        //                }
        //                if (!listMaHV.Contains(item.MaHV))
        //                {
        //                    var sumSUD = phieuThuList.Where(a => a.MaHV == item.MaHV).Sum(a => a.SUD);
        //                    if (sumSUD > 0)
        //                    {
        //                        listMaHV.Add(item.MaHV);
        //                        // Thực đạt HV NV
        //                        var bcTDHVNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 96 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                        if (bcTDHVNV == null)
        //                            bcTDHVNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 96 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                        if (bcTDHVNV == null)
        //                        {
        //                            bcTDHVNV = new ReportData()
        //                            {
        //                                Sort = 16,
        //                                Month = day.Month,
        //                                Year = day.Year,
        //                                ReportCategoryId = 96,
        //                                Data = "1",
        //                                HistoryUserId = historyUser.Id,
        //                                DataReal = 1,
        //                            };
        //                            bcList.Add(bcTDHVNV);
        //                        }
        //                        else
        //                        {
        //                            if (bcTDHVNV.DataReal == null)
        //                                bcTDHVNV.DataReal = 0;
        //                            bcTDHVNV.DataReal += 1;
        //                            bcTDHVNV.Data = (bcTDHVNV.DataReal ?? 0).ToString("N0");
        //                        }

        //                        // Tổng số tháng chốt - nhân viên
        //                        var bcTSTCNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 103 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                        if (bcTSTCNV == null)
        //                            bcTSTCNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 103 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                        if (bcTSTCNV == null)
        //                        {
        //                            bcTSTCNV = new ReportData()
        //                            {
        //                                Sort = 21,
        //                                Month = day.Month,
        //                                Year = day.Year,
        //                                ReportCategoryId = 103,
        //                                Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
        //                                HistoryUserId = historyUser.Id,
        //                                DataReal = item.ThangHocDuKienDecimal,
        //                            };
        //                            bcList.Add(bcTSTCNV);
        //                        }
        //                        else
        //                        {
        //                            if (bcTSTCNV.DataReal == null)
        //                                bcTSTCNV.DataReal = 0;
        //                            bcTSTCNV.DataReal += (item.ThangHocDuKienDecimal ?? 0);
        //                            bcTSTCNV.Data = (bcTSTCNV.DataReal ?? 0).ToString("N2");
        //                        }
        //                    }
        //                }
        //            }
        //            else if (historyUser.TypeUser == TypeUser.CM || historyUser.TypeUser == TypeUser.TTL)
        //            {
        //                idTyTrong = 43;
        //                sortTyTrong = 20;
        //            }
        //            else if (historyUser.TypeUser == TypeUser.SAB)
        //            {
        //                idTyTrong = 119;
        //                sortTyTrong = 40;
        //            }
        //            if (idTyTrong != 0)
        //            {
        //                var bcTiTrongCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == idTyTrong && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //                if (bcTiTrongCN == null)
        //                    bcTiTrongCN = bcList.FirstOrDefault(a => a.ReportCategoryId == idTyTrong && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //                if (bcTiTrongCN == null)
        //                {
        //                    bcTiTrongCN = new ReportData()
        //                    {
        //                        Sort = sortTyTrong,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = idTyTrong,
        //                        Data = (item.SUD ?? 0).ToString("N0"),
        //                        DataReal = item.SUD,
        //                        OfficeId = office.Id,
        //                    };
        //                    bcList.Add(bcTiTrongCN);
        //                }
        //                else
        //                {
        //                    decimal DataCN = 0;
        //                    if (string.IsNullOrEmpty(bcTiTrongCN.Data))
        //                        bcTiTrongCN.Data = "0";
        //                    var cleanedData = bcTiTrongCN.Data.Replace(",", "").Replace(".", "").Replace("ok", "");

        //                    if (decimal.TryParse(cleanedData, out DataCN))
        //                    {
        //                        DataCN += item.SUD ?? 0;
        //                        bcTiTrongCN.Data = DataCN.ToString("N0");

        //                        if (bcTiTrongCN.DataReal == null)
        //                            bcTiTrongCN.DataReal = 0;
        //                        bcTiTrongCN.DataReal += item.SUD ?? 0;
        //                    }
        //                    else
        //                    {
        //                        // Chuyển đổi thất bại
        //                        logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong the chuyen doi thanh so tu nhien ket qua bao cao CN: " + item.ChiNhanh);
        //                        continue;
        //                    }
        //                }
        //            }
        //            // Thực đạt doanh số NV
        //            var bcNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 88 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //            if (bcNV == null)
        //                bcNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 88 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //            if (bcNV == null)
        //            {
        //                bcNV = new ReportData()
        //                {
        //                    Sort = 11,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 88,
        //                    Data = (item.SUD ?? 0).ToString("N0"),
        //                    DataReal = item.SUD ?? 0,
        //                    UserId = historyUser.UserId,
        //                    HistoryUserId = historyUser.Id,
        //                    OfficeId = office.Id,
        //                };
        //                bcList.Add(bcNV);
        //            }
        //            else
        //            {
        //                if (bcNV.DataReal == null)
        //                    bcNV.DataReal = 0;
        //                bcNV.DataReal += item.SUD ?? 0;
        //                bcNV.Data = (bcNV.DataReal ?? 0).ToString("N0");
        //            }
        //            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(day.Year, day.Month, item.NgayThanhToan.Value);
        //            var weekNumber = new WeekNumber();
        //            switch (currentWeek)
        //            {
        //                case 1:
        //                    weekNumber = WeekNumber.Week1;
        //                    break;
        //                case 2:
        //                    weekNumber = WeekNumber.Week2;
        //                    break;
        //                case 3:
        //                    weekNumber = WeekNumber.Week3;
        //                    break;
        //                case 4:
        //                    weekNumber = WeekNumber.Week4;
        //                    break;
        //                case 5:
        //                    weekNumber = WeekNumber.Week5;
        //                    break;
        //                case 6:
        //                    weekNumber = WeekNumber.Week6;
        //                    break;
        //                default:
        //                    break;
        //            }
        //            var rUserWeek_Real = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Year == day.Year && a.Month == day.Month && (int)a.WeekNumber == currentWeek).FirstOrDefault();
        //            if (rUserWeek_Real == null)
        //                rUserWeek_Real = rUserWeek_RealList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Year == day.Year && a.Month == day.Month && (int)a.WeekNumber == currentWeek);
        //            if (rUserWeek_Real == null)
        //            {
        //                rUserWeek_Real = new RevenueUser_Week_Real()
        //                {
        //                    HistoryUserId = historyUser.Id,
        //                    UserId = historyUser.UserId,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    WeekNumber = weekNumber,
        //                    TargetBM = item.SUD ?? 0,
        //                };
        //                rUserWeek_RealList.Add(rUserWeek_Real);
        //            }
        //            else
        //            {
        //                rUserWeek_Real.TargetBM += item.SUD ?? 0;
        //            }
        //        }
        //    }

        //    if (rUserWeek_RealList.Any())
        //        _unitOfWork.RevenueUser_Week_RealRepository.InsertRange(rUserWeek_RealList);
        //    // ht phiếu cọc

        //    var listPhieuCoc = _unitOfWork.PhieuThuRepository.Get(a => a.NgayThanhToan.HasValue && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year && a.Loai == "Đặt cọc" &&
        //    (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm") && !a.THDB);
        //    var listChiNhanh = listPhieuCoc.Select(a => a.ChiNhanh).Distinct().ToList();
        //    var listMaNhanVien = listPhieuCoc.Select(a => a.MaNVChotSale).Distinct().ToList();

        //    // Phiếu cọc chi nhánh
        //    foreach (var cn in listChiNhanh)
        //    {
        //        var office = offices.FirstOrDefault(a => a.Active && a.ShortName == cn);
        //        if (office == null)
        //            continue;
        //        var phieuCocCN = listPhieuCoc.Where(a => a.ChiNhanh == cn).GroupBy(a => a.HDBH).Select(g => g.First()).ToList();

        //        var tongThangCoc = phieuCocCN.Sum(s => s.ThangHocDuKienDecimal);
        //        var thangCocDaGop = phieuCocCN.Where(a => a.HDBH != null).Sum(s => s.ThangHocDuKienDecimal);
        //        var thangCocTon = tongThangCoc - thangCocDaGop;
        //        // Báo cáo Tổng tháng cọc
        //        var bctongThangCoc = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 68 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bctongThangCoc == null)
        //            bctongThangCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 68 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bctongThangCoc == null)
        //        {
        //            bctongThangCoc = new ReportData()
        //            {
        //                Sort = 1,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 68,
        //                Data = (tongThangCoc ?? 0).ToString("N2"),
        //                OfficeId = office.Id,
        //                DataReal = tongThangCoc,
        //            };
        //            bcList.Add(bctongThangCoc);
        //        }
        //        else
        //        {
        //            bctongThangCoc.DataReal = tongThangCoc;
        //            bctongThangCoc.Data = (tongThangCoc ?? 0).ToString("N2");
        //        }
        //        // Báo cáo tháng cọc đã gộp
        //        var bcThangCocDaGop = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 71 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcThangCocDaGop == null)
        //            bcThangCocDaGop = bcList.FirstOrDefault(a => a.ReportCategoryId == 71 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcThangCocDaGop == null)
        //        {
        //            bcThangCocDaGop = new ReportData()
        //            {
        //                Sort = 1,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 71,
        //                Data = (thangCocDaGop ?? 0).ToString("N2"),
        //                OfficeId = office.Id,
        //                DataReal = thangCocDaGop,
        //            };
        //            bcList.Add(bcThangCocDaGop);
        //        }
        //        else
        //        {
        //            bcThangCocDaGop.DataReal = thangCocDaGop;
        //            bcThangCocDaGop.Data = (thangCocDaGop ?? 0).ToString("N2");
        //        }

        //        // Báo cáo tháng cọc tồn
        //        var bcThangCocTon = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 76 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //        if (bcThangCocTon == null)
        //            bcThangCocTon = bcList.FirstOrDefault(a => a.ReportCategoryId == 76 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //        if (bcThangCocTon == null)
        //        {
        //            bcThangCocTon = new ReportData()
        //            {
        //                Sort = 1,
        //                Month = day.Month,
        //                Year = day.Year,
        //                ReportCategoryId = 76,
        //                Data = (thangCocTon ?? 0).ToString("N2"),
        //                OfficeId = office.Id,
        //                DataReal = thangCocTon,
        //            };
        //            bcList.Add(bcThangCocTon);
        //        }
        //        else
        //        {
        //            bcThangCocTon.DataReal = thangCocTon;
        //            bcThangCocTon.Data = (thangCocTon ?? 0).ToString("N2");
        //        }

        //        if (tongThangCoc > 0 && thangCocDaGop != null)
        //        {
        //            var tileChuyenDoiCoc = thangCocDaGop / tongThangCoc;
        //            // Báo cáo tỉ lệ chuyển đổi cọc
        //            var bcTiLeCDCoc = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 73 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id).FirstOrDefault();
        //            if (bcTiLeCDCoc == null)
        //                bcTiLeCDCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 73 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
        //            if (bcTiLeCDCoc == null)
        //            {
        //                bcTiLeCDCoc = new ReportData()
        //                {
        //                    Sort = 1,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 73,
        //                    Data = ((tileChuyenDoiCoc ?? 0) * 100).ToString("F2") + "%",
        //                    OfficeId = office.Id,
        //                    DataReal = tileChuyenDoiCoc,
        //                };
        //                bcList.Add(bcTiLeCDCoc);
        //            }
        //            else
        //            {
        //                bcTiLeCDCoc.DataReal = tileChuyenDoiCoc;
        //                bcTiLeCDCoc.Data = ((tileChuyenDoiCoc ?? 0) * 100).ToString("F2") + "%";
        //            }
        //        }
        //    }

        //    //Phiếu cọc nhân sự
        //    foreach (var mnv in listMaNhanVien)
        //    {
        //        //Reset thực đạt NV về 0
        //        var bcnvs = _unitOfWork.ReportDataRepository.GetQuery(a => (a.ReportCategoryId == 111 || a.ReportCategoryId == 113 || a.ReportCategoryId == 114 || a.ReportCategoryId == 115) && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null && a.HistoryUser.User.MaNhanVien == mnv);
        //        foreach (var bcnv in bcnvs)
        //        {
        //            bcnv.Data = "";
        //            bcnv.DataReal = 0;
        //        }
        //    }

        //    foreach (var item in listPhieuCoc)
        //    {
        //        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == item.ChiNhanh).FirstOrDefault();
        //        if (office == null)
        //        {
        //            logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai chi nhanh nao co ten ngan la " + item.ChiNhanh);
        //            continue;
        //        }
        //        if (!string.IsNullOrEmpty(item.MaNVChotSale))
        //        {
        //            var historyUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == day.Month && a.Year == day.Year && a.User.MaNhanVien == item.MaNVChotSale
        //                                && DbFunctions.TruncateTime(a.DayStart) <= DbFunctions.TruncateTime(item.NgayThanhToan) && (a.DayEnd == null || DbFunctions.TruncateTime(a.DayEnd) >= DbFunctions.TruncateTime(item.NgayThanhToan)),
        //                                q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).FirstOrDefault();
        //            if (historyUser == null)
        //            {
        //                logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai nhan su theo thang nao thoa man ngay lam viec: " + item.NgayThanhToan + " va MNV: " + item.MaNVChotSale);
        //                continue;
        //            }
        //            // Tổng tháng cọc

        //            var bcTongThangCocNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 111 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //            if (bcTongThangCocNV == null)
        //                bcTongThangCocNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 111 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //            if (bcTongThangCocNV == null)
        //            {
        //                bcTongThangCocNV = new ReportData()
        //                {
        //                    Sort = 1,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 111,
        //                    Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
        //                    DataReal = item.ThangHocDuKienDecimal ?? 0,
        //                    UserId = historyUser.UserId,
        //                    HistoryUserId = historyUser.Id,
        //                    OfficeId = office.Id,
        //                };
        //                bcList.Add(bcTongThangCocNV);
        //            }
        //            else
        //            {
        //                if (bcTongThangCocNV.DataReal == null)
        //                    bcTongThangCocNV.DataReal = 0;
        //                bcTongThangCocNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
        //                bcTongThangCocNV.Data = (bcTongThangCocNV.DataReal ?? 0).ToString("N2");
        //            }

        //            //Tổng tháng đã gộp

        //            if (item.HDBH == null)
        //            {
        //                var bcThangCocDaGopNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 113 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                if (bcThangCocDaGopNV == null)
        //                    bcThangCocDaGopNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 113 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                if (bcThangCocDaGopNV == null)
        //                {
        //                    bcThangCocDaGopNV = new ReportData()
        //                    {
        //                        Sort = 1,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 113,
        //                        Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
        //                        DataReal = item.ThangHocDuKienDecimal ?? 0,
        //                        UserId = historyUser.UserId,
        //                        HistoryUserId = historyUser.Id,
        //                        OfficeId = office.Id,
        //                    };
        //                    bcList.Add(bcThangCocDaGopNV);
        //                }
        //                else
        //                {
        //                    if (bcThangCocDaGopNV.DataReal == null)
        //                        bcThangCocDaGopNV.DataReal = 0;
        //                    bcThangCocDaGopNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
        //                    bcThangCocDaGopNV.Data = (bcTongThangCocNV.DataReal ?? 0).ToString("N2");
        //                }
        //                if (bcTongThangCocNV.DataReal > 0)
        //                {
        //                    var tilethuhoicoc = bcThangCocDaGopNV.DataReal / bcTongThangCocNV.DataReal;
        //                    var bcTiLeThuHoiCoc = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 115 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                    if (bcTiLeThuHoiCoc == null)
        //                        bcTiLeThuHoiCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 115 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                    if (bcTiLeThuHoiCoc == null)
        //                    {
        //                        bcTiLeThuHoiCoc = new ReportData()
        //                        {
        //                            Sort = 1,
        //                            Month = day.Month,
        //                            Year = day.Year,
        //                            ReportCategoryId = 115,
        //                            Data = (tilethuhoicoc ?? 0).ToString("F2"),
        //                            DataReal = tilethuhoicoc,
        //                            UserId = historyUser.UserId,
        //                            HistoryUserId = historyUser.Id,
        //                            OfficeId = office.Id,
        //                        };
        //                        bcList.Add(bcTiLeThuHoiCoc);
        //                    }
        //                    else
        //                    {
        //                        bcTiLeThuHoiCoc.DataReal = tilethuhoicoc;
        //                        bcTiLeThuHoiCoc.Data = (tilethuhoicoc ?? 0).ToString("F2");
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var bcThangCocTonNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 114 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id).FirstOrDefault();
        //                if (bcThangCocTonNV == null)
        //                    bcThangCocTonNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 114 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
        //                if (bcThangCocTonNV == null)
        //                {
        //                    bcThangCocTonNV = new ReportData()
        //                    {
        //                        Sort = 1,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 114,
        //                        Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
        //                        DataReal = item.ThangHocDuKienDecimal ?? 0,
        //                        UserId = historyUser.UserId,
        //                        HistoryUserId = historyUser.Id,
        //                        OfficeId = office.Id,
        //                    };
        //                    bcList.Add(bcThangCocTonNV);
        //                }
        //                else
        //                {
        //                    if (bcThangCocTonNV.DataReal == null)
        //                        bcThangCocTonNV.DataReal = 0;
        //                    bcThangCocTonNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
        //                    bcThangCocTonNV.Data = (bcTongThangCocNV.DataReal ?? 0).ToString("N2");
        //                }
        //            }
        //        }
        //    }
        //    if (bcList.Any())
        //        _unitOfWork.ReportDataRepository.InsertRange(bcList);
        //    //_unitOfWork.Save();

        //    var bcListNew = new List<ReportData>();
        //    var newDataList = _unitOfWork.ReportDataRepository.GetQuery(a => (a.ReportCategoryId == 35 || a.ReportCategoryId == 88 || a.ReportCategoryId == 96) && a.Month == day.Month && a.Year == day.Year);
        //    // % ht doanh số cn - nv; %ht HVNV; Cơ cấu DS CN
        //    foreach (var item in newDataList)
        //    {
        //        //var day = item.CreateDate;
        //        // Data Thực đạt DS CN
        //        if (item.ReportCategoryId == 35)
        //        {
        //            // Chỉ tiêu DS CN
        //            var datact = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 34 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId && a.Data != null && a.Data != "0" && a.Data != "").FirstOrDefault();
        //            if (datact == null)
        //            {
        //                logger.Error("Chua co chi tieu DS chi nhanh: " + item.Office?.ShortName + " - thang " + item.Month);
        //                continue;
        //            }

        //            decimal ct = 0;
        //            var cleanedDatact = datact.Data.Replace(",", "").Replace(".", "");
        //            if (decimal.TryParse(cleanedDatact, out ct))
        //            {
        //            }
        //            else
        //            {
        //                // Chuyển đổi thất bại
        //                logger.Error("Khong the chuyen doi thanh so tu nhien ket qua bao cao chi tieu CN: " + item.Office?.ShortName);
        //                continue;
        //            }
        //            //Thực đạt DS CN
        //            decimal td = 0;

        //            if (string.IsNullOrEmpty(item.Data))
        //                item.Data = "0";
        //            var cleanedData = item.Data.Replace(",", "").Replace(".", "");
        //            if (decimal.TryParse(cleanedData, out td))
        //            {
        //            }
        //            else
        //            {
        //                // Chuyển đổi thất bại
        //                logger.Error("Khong the chuyen doi thanh so tu nhien ket qua bao cao thuc dat CN: " + item.Office?.ShortName);
        //                continue;
        //            }
        //            // % HT DS CN
        //            var ht = td / ct * 100;
        //            var dataht = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 36 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //            if (dataht != null)
        //            {
        //                dataht.Data = ht.ToString("F2") + "%";
        //                dataht.DataReal = ht / 100;
        //            }
        //            else
        //            {
        //                dataht = new ReportData()
        //                {
        //                    Sort = 15,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 36,
        //                    Data = ht.ToString("F2") + "%",
        //                    DataReal = ht / 100,
        //                    OfficeId = item.OfficeId,
        //                };
        //                bcListNew.Add(dataht);
        //            }

        //            // Cơ cấu DS các bộ phận (Sale, kế toán, đào tạo)
        //            if (td > 0)
        //            {
        //                // Doanh số sale
        //                var dataDSSale = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 40 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataDSSale == null)
        //                {
        //                    dataDSSale = new ReportData()
        //                    {
        //                        Sort = 18,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 40,
        //                        Data = "0",
        //                        DataReal = 0,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataDSSale);
        //                }

        //                // Tỉ trọng sale
        //                decimal tiTrongSale = (dataDSSale.DataReal ?? 0) / td;
        //                var dataTiTrongSale = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 41 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataTiTrongSale != null)
        //                {
        //                    dataTiTrongSale.Data = tiTrongSale.ToString("N2");
        //                    dataTiTrongSale.DataReal = tiTrongSale;
        //                }
        //                else
        //                {
        //                    dataTiTrongSale = new ReportData()
        //                    {
        //                        Sort = 19,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 41,
        //                        Data = tiTrongSale.ToString("N2"),
        //                        DataReal = tiTrongSale,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataTiTrongSale);
        //                }

        //                // Doanh số đào tạo
        //                var dataDSHV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 43 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataDSHV == null)
        //                {
        //                    dataDSHV = new ReportData()
        //                    {
        //                        Sort = 20,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 43,
        //                        Data = "0",
        //                        DataReal = 0,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataDSHV);
        //                }

        //                // Tỉ trọng đào tạo
        //                decimal tiTrongHV = (dataDSHV.DataReal ?? 0) / td;
        //                var dataTiTrongHV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 45 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataTiTrongHV != null)
        //                {
        //                    dataTiTrongHV.Data = tiTrongHV.ToString("N2");
        //                    dataTiTrongHV.DataReal = tiTrongHV;
        //                }
        //                else
        //                {
        //                    dataTiTrongHV = new ReportData()
        //                    {
        //                        Sort = 21,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 45,
        //                        Data = tiTrongHV.ToString("N2"),
        //                        DataReal = tiTrongHV,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataTiTrongHV);
        //                }

        //                // Doanh số kế toán
        //                var dataDSKT = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 119 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataDSKT == null)
        //                {
        //                    dataDSKT = new ReportData()
        //                    {
        //                        Sort = 40,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 119,
        //                        Data = "0",
        //                        DataReal = 0,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataDSKT);
        //                }

        //                // Tỉ trọng kế toán
        //                decimal tiTrongKT = (dataDSKT.DataReal ?? 0) / td;
        //                var dataTiTrongKT = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 120 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                if (dataTiTrongKT != null)
        //                {
        //                    dataTiTrongKT.Data = tiTrongKT.ToString("N2");
        //                    dataTiTrongKT.DataReal = tiTrongKT;
        //                }
        //                else
        //                {
        //                    dataTiTrongKT = new ReportData()
        //                    {
        //                        Sort = 41,
        //                        Month = day.Month,
        //                        Year = day.Year,
        //                        ReportCategoryId = 120,
        //                        Data = tiTrongKT.ToString("N2"),
        //                        DataReal = tiTrongKT,
        //                        OfficeId = item.OfficeId,
        //                    };
        //                    bcListNew.Add(dataTiTrongKT);
        //                }
        //            }

        //        }
        //        // Data Thực đạt DS NV
        //        else if (item.ReportCategoryId == 88)
        //        {
        //            //var datact = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 87 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId && a.Data != null && a.Data != "0" && a.Data != "").FirstOrDefault();
        //            // Tìm báo cáo chỉ tiêu ds của nv
        //            var datact = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 87 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId).FirstOrDefault();
        //            if (datact == null)
        //                datact = bcListNew.FirstOrDefault(a => a.ReportCategoryId == 87 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId);
        //            // Nếu không có chỉ tiêu trong db, gán bằng chuỗi rỗng
        //            if (datact == null)
        //            {
        //                logger.Error("Chua co chi tieu DS NV: " + item.HistoryUser?.User.MaNhanVien + " - thang " + item.Month);
        //                datact = new ReportData()
        //                {
        //                    Sort = 10,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 87,
        //                    Data = "",
        //                    HistoryUserId = item.HistoryUserId,
        //                    UserId = item.UserId,
        //                    OfficeId = item.OfficeId,
        //                };
        //                bcListNew.Add(datact);
        //                continue;
        //            }
        //            if (datact.DataReal == null || datact.DataReal == 0)
        //            {
        //                logger.Error("Chi tieu DS NV trong: " + item.HistoryUser?.User.MaNhanVien + " - thang " + item.Month);
        //                continue;
        //            }
        //            if (item.DataReal == null)
        //                item.DataReal = 0;
        //            var ht = (item.DataReal / datact.DataReal) ?? 0;
        //            // %ht báo cáo DS NV
        //            var dataht = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 89 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId).FirstOrDefault();
        //            if (dataht != null)
        //            {
        //                dataht.Data = (ht * 100).ToString("F2") + "%";
        //                dataht.DataReal = ht;
        //            }
        //            else
        //            {
        //                dataht = new ReportData()
        //                {
        //                    Sort = 12,
        //                    Month = day.Month,
        //                    Year = day.Year,
        //                    ReportCategoryId = 89,
        //                    Data = ht.ToString("F2") + "%",
        //                    DataReal = ht,
        //                    HistoryUserId = item.HistoryUserId,
        //                    UserId = item.UserId,
        //                    OfficeId = item.OfficeId,
        //                };
        //                bcListNew.Add(dataht);
        //            }

        //            // Cơ cấu %ht Sale
        //            if (item.HistoryUser?.TypeUser == TypeUser.EC || item.HistoryUser?.TypeUser == TypeUser.ALT)
        //            {
        //                int idkqhtSale = 0;
        //                if (ht >= 1)
        //                    idkqhtSale = 78;
        //                else if (ht >= (decimal)0.3 && ht < (decimal)0.5)
        //                    idkqhtSale = 80;
        //                else if (ht >= (decimal)0.2 && ht < (decimal)0.3)
        //                    idkqhtSale = 82;
        //                else if (ht < (decimal)0.2)
        //                    idkqhtSale = 84;
        //                if (idkqhtSale != 0)
        //                {
        //                    var datakqhtSale = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == idkqhtSale && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId).FirstOrDefault();
        //                    if (datakqhtSale == null)
        //                        datakqhtSale = bcListNew.FirstOrDefault(a => a.ReportCategoryId == idkqhtSale && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId);
        //                    if (datakqhtSale == null)
        //                    {
        //                        datakqhtSale = new ReportData()
        //                        {
        //                            Sort = 1,
        //                            Month = day.Month,
        //                            Year = day.Year,
        //                            ReportCategoryId = idkqhtSale,
        //                            Data = "1",
        //                            DataReal = 1,
        //                            OfficeId = item.OfficeId,
        //                        };
        //                        bcListNew.Add(datakqhtSale);
        //                    }
        //                    else
        //                    {
        //                        if (datakqhtSale.DataReal == null)
        //                            datakqhtSale.DataReal = 0;
        //                        datakqhtSale.DataReal += 1;
        //                        datakqhtSale.Data = datakqhtSale.DataReal.ToString();
        //                    }
        //                }
        //            }


        //        }
        //        // Data Thực đạt HV NV; BQ tháng chốt/ HV - nhân viên
        //        else if (item.ReportCategoryId == 96)
        //        {
        //            // % ht báo cáo HV NV
        //            var reportCTHVNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 95).FirstOrDefault();
        //            if (reportCTHVNV?.DataReal > 0)
        //            {
        //                var htHVNV = (item.DataReal ?? 0) / reportCTHVNV.DataReal * 100;
        //                var datahtHVNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 97).FirstOrDefault();
        //                if (datahtHVNV == null)
        //                    datahtHVNV = bcListNew.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 97);
        //                if (datahtHVNV == null)
        //                {
        //                    datahtHVNV = new ReportData()
        //                    {
        //                        Data = (htHVNV ?? 0).ToString("F2") + "%",
        //                        DataReal = htHVNV / 100,
        //                        Month = day.Month,
        //                        Year = day.Month,
        //                        ReportCategoryId = 97,
        //                        HistoryUserId = item.HistoryUserId,
        //                        Sort = 17,
        //                    };
        //                    bcListNew.Add(datahtHVNV);
        //                }
        //                else
        //                {
        //                    datahtHVNV.Data = (htHVNV ?? 0).ToString("F2") + "%";
        //                    datahtHVNV.DataReal = htHVNV / 100;
        //                }
        //            }

        //            //BQ tháng chốt/ HV - NV

        //            // Tổng số tháng chốt
        //            var reportTSTCNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 103).FirstOrDefault();
        //            if (item.DataReal > 0 && reportTSTCNV?.DataReal != null)
        //            {
        //                var bqTC1HV = reportTSTCNV.DataReal / item.DataReal;
        //                var dataBQTC1HV = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 105).FirstOrDefault();
        //                if (dataBQTC1HV == null)
        //                    dataBQTC1HV = bcListNew.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 105);
        //                if (dataBQTC1HV == null)
        //                {
        //                    dataBQTC1HV = new ReportData()
        //                    {
        //                        Data = (bqTC1HV ?? 0).ToString("N2"),
        //                        DataReal = bqTC1HV,
        //                        Month = day.Month,
        //                        Year = day.Month,
        //                        ReportCategoryId = 105,
        //                        HistoryUserId = item.HistoryUserId,
        //                        Sort = 22,
        //                    };
        //                    bcListNew.Add(dataBQTC1HV);
        //                }
        //                else
        //                {
        //                    dataBQTC1HV.Data = (bqTC1HV ?? 0).ToString("N2");
        //                    dataBQTC1HV.DataReal = bqTC1HV;
        //                }
        //            }
        //        }
        //    }

        //    if (bcListNew.Any())
        //        _unitOfWork.ReportDataRepository.InsertRange(bcListNew);

        //    _unitOfWork.Save();
        //}
        public void SyncDthu(int month, int year)
        {
            var day = new DateTime(year, month, 1);
            var phieuThuAllList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.ThangTinhDThu == day.Month && a.NamTinhDThu == day.Year && (a.Loai == "Phiếu gộp" || a.Loai == "Học phí") &&
            (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm" || a.TrangThai == null || a.TrangThai == "")).AsNoTracking().ToList();
            var bcList = new List<ReportData>();
            var rUserWeek_RealList = new List<RevenueUser_Week_Real>();
            var listMaNV = phieuThuAllList.Select(a => a.MaNVChotSale).Distinct().ToList();
            var listCN = phieuThuAllList.Select(a => a.ChiNhanh).Distinct().ToList();
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var listbcnvReset = _unitOfWork.ReportDataRepository.Get(a => (a.ReportCategoryId == 88 || a.ReportCategoryId == 96 || a.ReportCategoryId == 103) && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null);
            var listttWeeks = _unitOfWork.RevenueUser_Week_RealRepository.Get(a => a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null);
            var listReportCategoryIdCN = new List<int> { 30, 31, 32, 35, 36, 45, 58, 60, 62, 64, 66, 68, 71, 73, 76, 40, 43, 119, 117 };

            //var listCategoryCTUD = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.Group == 8).AsNoTracking().ToList();
            //var listIdChild = listCategoryCTUD.Where(a => a.ReportCategoryId != null).Select(a => a.Id);
            //listReportCategoryIdCN.AddRange(listIdChild);

            var listBCCN = _unitOfWork.ReportDataRepository.Get(a => a.Month == day.Month && a.Year == day.Year && listReportCategoryIdCN.Contains(a.ReportCategoryId));
            var listReportCategoryIdNV = new List<int> { 95, 96, 88, 111, 113, 114, 115, 103 };
            var listBCNV = _unitOfWork.ReportDataRepository.Get(a => a.Month == day.Month && a.Year == day.Year && listReportCategoryIdNV.Contains(a.ReportCategoryId));
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == day.Month && a.Year == day.Year, q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).AsNoTracking().ToList();
            var rUserWeek_Reals = _unitOfWork.RevenueUser_Week_RealRepository.Get(a => a.Year == day.Year && a.Month == day.Month);
            var firstDayOfMonth = new DateTime(day.Year, day.Month, 1).Date;
            var endDayOfMonth = new DateTime(day.Year, day.Month, DateTime.DaysInMonth(day.Year, day.Month));

            //var listDiscount = _unitOfWork.DiscountRepository.GetQuery(a => a.Active && DbFunctions.TruncateTime(a.StartDate) <= firstDayOfMonth && DbFunctions.TruncateTime(a.EndDate) >= endDayOfMonth).AsNoTracking().ToList();
            //var listgroupDiscount = _unitOfWork.DiscountRepository.GetQuery(a => a.Active).AsNoTracking().ToList();

            //var listCategoryCTUD = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.Group == 8).AsNoTracking().ToList();
            //var listIdChild = listCategoryCTUD.Where(a => a.ReportCategoryId != null).Select(a => a.Id);
            foreach (var mnv in listMaNV)
            {
                //Reset thực đạt NV về 0
                var bcnvs = listbcnvReset.Where(a => a.HistoryUser.User.MaNhanVien == mnv);
                foreach (var bcnv in bcnvs)
                {
                    bcnv.Data = "";
                    bcnv.DataReal = 0;
                }

                var ttWeeks = listttWeeks.Where(a => a.HistoryUser.User.MaNhanVien == mnv);
                foreach (var ttWeek in ttWeeks)
                {
                    ttWeek.TargetBM = 0;
                }
            }

            var listIdReset = new List<int> { 35, 40, 43, 119, 78, 80, 82, 84 };
            //listIdReset.AddRange(listIdChild);
            var listbccnReset = _unitOfWork.ReportDataRepository.Get(a => listIdReset.Contains(a.ReportCategoryId) && a.Month == day.Month && a.Year == day.Year);

            foreach (var cn in listCN)
            {
                var office = offices.FirstOrDefault(a => a.Active && a.ShortName == cn);
                if (office == null)
                    continue;
                var bccns = listbccnReset.Where(a => a.Office.ShortName == cn);
                foreach (var b in bccns)
                {
                    b.DataReal = 0;
                    b.Data = "0";
                }
                //var countHV = phieuThuThongThuong.Where(a => a.ChiNhanh == cn).Select(a => a.MaHV).Distinct().Count();
                // Thực đạt HV CN
                var countHV = phieuThuAllList.Where(a => a.ChiNhanh == cn).GroupBy(a => a.MaHV).Where(g => g.Sum(x => x.SUD ?? 0) > 0).Count();

                var bcTDHVCN = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 31 && a.OfficeId == office.Id);
                if (bcTDHVCN == null)
                    bcTDHVCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 31 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcTDHVCN == null)
                {
                    bcTDHVCN = new ReportData()
                    {
                        Sort = 11,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 31,
                        Data = countHV.ToString("N0"),
                        OfficeId = office.Id,
                        DataReal = countHV,
                    };
                    bcList.Add(bcTDHVCN);
                }
                else
                {
                    bcTDHVCN.DataReal = countHV;
                    bcTDHVCN.Data = countHV.ToString("N0");
                }
                // % ht báo cáo HV CN
                var reportCTHVCN = listBCCN.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 30);
                if (reportCTHVCN?.DataReal > 0)
                {
                    var htHVCN = bcTDHVCN.DataReal / reportCTHVCN.DataReal * 100;
                    var datahtHVCN = listBCCN.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 32);
                    if (datahtHVCN == null)
                        datahtHVCN = bcList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 32);
                    if (datahtHVCN == null)
                    {
                        datahtHVCN = new ReportData()
                        {
                            Data = (htHVCN ?? 0).ToString("F2") + "%",
                            DataReal = htHVCN / 100,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 32,
                            OfficeId = office.Id,
                            Sort = 12,
                        };
                        bcList.Add(datahtHVCN);
                    }
                    else
                    {
                        datahtHVCN.Data = (htHVCN ?? 0).ToString("F2") + "%";
                        datahtHVCN.DataReal = htHVCN / 100;
                    }
                }

                // BC Học viên GD mới, GD lại

                // Lấy danh sách học viên đã ghi danh mới
                var maHVGhiDanhMoi = new HashSet<string>(
                    phieuThuAllList
                        .Where(a => a.ChiNhanh == cn && a.DangKy == "Ghi danh mới")
                        .Select(a => a.MaHV)
                        .Distinct()
                );

                // Đếm học viên ghi danh mới
                var countHVGDM = phieuThuAllList
                    .Where(a => a.ChiNhanh == cn && a.DangKy == "Ghi danh mới")
                    .GroupBy(a => a.MaHV)
                    .Where(g => g.Sum(x => x.SUD ?? 0) > 0)
                    .Count();

                // Đếm học viên ghi danh lại (không nằm trong danh sách ghi danh mới)
                var countHVGDL = phieuThuAllList
                    .Where(a => a.ChiNhanh == cn
                             && a.DangKy == "Ghi danh tiếp"
                             && !maHVGhiDanhMoi.Contains(a.MaHV))
                    .GroupBy(a => a.MaHV)
                    .Where(g => g.Sum(x => x.SUD ?? 0) > 0)
                    .Count();


                var bcHVGDM = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 60 && a.OfficeId == office.Id);
                if (bcHVGDM == null)
                    bcHVGDM = bcList.FirstOrDefault(a => a.ReportCategoryId == 60 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcHVGDM == null)
                {
                    bcHVGDM = new ReportData()
                    {
                        Sort = 28,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 60,
                        Data = countHVGDM.ToString("N0"),
                        OfficeId = office.Id,
                        DataReal = countHVGDM,
                    };
                    bcList.Add(bcHVGDM);
                }
                else
                {
                    bcHVGDM.DataReal = countHVGDM;
                    bcHVGDM.Data = countHVGDM.ToString("N0");
                }

                var bcHVGDL = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 58 && a.OfficeId == office.Id);
                if (bcHVGDL == null)
                    bcHVGDL = bcList.FirstOrDefault(a => a.ReportCategoryId == 58 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcHVGDL == null)
                {
                    bcHVGDL = new ReportData()
                    {
                        Sort = 27,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 58,
                        Data = countHVGDL.ToString("N0"),
                        OfficeId = office.Id,
                        DataReal = countHVGDL,
                    };
                    bcList.Add(bcHVGDL);
                }
                else
                {
                    bcHVGDL.DataReal = countHVGDL;
                    bcHVGDL.Data = countHVGDL.ToString("N0");
                }

                // Báo cáo Tổng số tháng ĐK, tháng học BQ/HV
                var sumSoThangDK = phieuThuAllList.Where(a => a.ChiNhanh == cn && a.SUD != 0).Sum(a => a.ThangHocDuKienDecimal ?? 0);
                decimal? STHBQ1HV = null;
                STHBQ1HV = countHV > 0 ? sumSoThangDK / countHV : (decimal?)null;

                var bcSoThangDK = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 62 && a.OfficeId == office.Id);
                if (bcSoThangDK == null)
                    bcSoThangDK = bcList.FirstOrDefault(a => a.ReportCategoryId == 62 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcSoThangDK == null)
                {
                    bcSoThangDK = new ReportData()
                    {
                        Sort = 29,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 62,
                        Data = sumSoThangDK.ToString("N2"),
                        OfficeId = office.Id,
                        DataReal = sumSoThangDK,
                    };
                    bcList.Add(bcSoThangDK);
                }
                else
                {
                    bcSoThangDK.DataReal = sumSoThangDK;
                    bcSoThangDK.Data = sumSoThangDK.ToString("N2");
                }

                var bcSTHBQ1HV = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 64 && a.OfficeId == office.Id);
                if (bcSTHBQ1HV == null)
                    bcSTHBQ1HV = bcList.FirstOrDefault(a => a.ReportCategoryId == 64 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcSTHBQ1HV == null)
                {
                    bcSTHBQ1HV = new ReportData()
                    {
                        Sort = 30,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 64,
                        Data = countHV > 0 ? (STHBQ1HV ?? 0).ToString("N2") : "",
                        OfficeId = office.Id,
                        DataReal = STHBQ1HV,
                    };
                    bcList.Add(bcSTHBQ1HV);
                }
                else
                {
                    bcSTHBQ1HV.DataReal = STHBQ1HV;
                    bcSTHBQ1HV.Data = countHV > 0 ? (STHBQ1HV ?? 0).ToString("N2") : "";
                }

                //BQ Ưu đãi sử dụng
                var listPhieuThuKhac0d = phieuThuAllList.Where(a => a.ChiNhanh == cn && a.SUD != 0);
                var SUDTotal = listPhieuThuKhac0d.Sum(a => a.SUD);
                var TUDTotal = listPhieuThuKhac0d.Sum(a => a.TUD);
                if (TUDTotal > 0)
                {
                    var bqUDSD = 1 - (SUDTotal / TUDTotal);
                    var bcBQUDSDCN = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 66 && a.OfficeId == office.Id);
                    if (bcBQUDSDCN == null)
                        bcBQUDSDCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 66 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                    if (bcBQUDSDCN == null)
                    {
                        bcBQUDSDCN = new ReportData()
                        {
                            Sort = 31,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 66,
                            Data = ((bqUDSD ?? 0) * 100).ToString("F2") + "%",
                            OfficeId = office.Id,
                            DataReal = bqUDSD,
                        };
                        bcList.Add(bcBQUDSDCN);
                    }
                    else
                    {
                        bcBQUDSDCN.DataReal = bqUDSD;
                        bcBQUDSDCN.Data = ((bqUDSD ?? 0) * 100).ToString("F2") + "%";
                    }
                }

            }
            //List chứa các mã học viên đã được tính
            var listMaHV = new List<string>();
            foreach (var item in phieuThuAllList)
            {
                var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == item.ChiNhanh.Normalize(NormalizationForm.FormC));
                if (office == null)
                {
                    logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai chi nhanh nao co ten ngan la " + item.ChiNhanh);
                    continue;
                }
                // thực đạt doanh thu CN
                var bcCN = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 35 && a.OfficeId == office.Id);
                if (bcCN == null)
                    bcCN = bcList.FirstOrDefault(a => a.ReportCategoryId == 35 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcCN == null)
                {
                    bcCN = new ReportData()
                    {
                        Sort = 14,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 35,
                        Data = (item.SUD ?? 0).ToString("N0"),
                        DataReal = item.SUD,
                        OfficeId = office.Id,
                    };
                    bcList.Add(bcCN);
                }
                else
                {
                    if (bcCN.DataReal == null)
                        bcCN.DataReal = 0;

                    bcCN.DataReal += item.SUD ?? 0;
                    bcCN.Data = (bcCN.DataReal ?? 0).ToString("N0");
                }

                //Phân loại doanh thu theo CT ưu đãi

                //var idCategoryCTUD = 0;

                //var phanloai = listDiscount.FirstOrDefault(a => a.Username == item.UD_FINAL)?.PhanLoai;
                //var categoryCTUDChild = listCategoryCTUD.FirstOrDefault(a => a.CategoryParent != null && a.CategoryParent.Name == phanloai);

                //if (categoryCTUDChild != null)
                //    idCategoryCTUD = categoryCTUDChild.Id;
                //if (idCategoryCTUD != 0)
                //{
                //    var bcPhanLoaiDthu = listBCCN.FirstOrDefault(a => a.ReportCategoryId == idCategoryCTUD && a.OfficeId == office.Id);
                //    if (bcPhanLoaiDthu == null)
                //        bcPhanLoaiDthu = bcList.FirstOrDefault(a => a.ReportCategoryId == idCategoryCTUD && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                //    if (bcPhanLoaiDthu == null)
                //    {
                //        bcPhanLoaiDthu = new ReportData()
                //        {
                //            Sort = 1,
                //            Month = day.Month,
                //            Year = day.Year,
                //            ReportCategoryId = idCategoryCTUD,
                //            Data = (item.SUD ?? 0).ToString("N0"),
                //            DataReal = item.SUD,
                //            OfficeId = office.Id,
                //        };
                //        bcList.Add(bcPhanLoaiDthu);
                //    }
                //    else
                //    {
                //        if (bcPhanLoaiDthu.DataReal == null)
                //            bcPhanLoaiDthu.DataReal = 0;
                //        bcPhanLoaiDthu.DataReal += item.SUD ?? 0;
                //        bcPhanLoaiDthu.Data = (bcPhanLoaiDthu.DataReal ?? 0).ToString("N0");
                //    }
                //}

                if (!string.IsNullOrEmpty(item.MaNVChotSale))
                {
                    var historyUser = historyUsers.FirstOrDefault(a => a.User.MaNhanVien == item.MaNVChotSale && a.DayStart.Date <= item.NgayThanhToan.Value.Date
                    && (a.DayEnd == null || a.DayEnd.Value.Date >= item.NgayThanhToan.Value.Date));
                    if (historyUser == null)
                    {
                        logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai nhan su theo thang nao thoa man ngay lam viec: " + item.NgayThanhToan + " va MNV: " + item.MaNVChotSale);
                        continue;
                    }
                    // Doanh số sale - đào tạo- kế toán; Thực đạt HV NV
                    int idTyTrong = 0;
                    int sortTyTrong = 0;
                    if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC)
                    {
                        if (historyUser.TypeUser != TypeUser.AEC)
                        {
                            idTyTrong = 40;
                            sortTyTrong = 18;
                        }
                        else
                        {
                            // Xử lý chỉ tiêu HV rỗng cho AEC
                            var bcCTHVAEC = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 95 && a.HistoryUserId == historyUser.Id);
                            if (bcCTHVAEC == null)
                                bcCTHVAEC = bcList.FirstOrDefault(a => a.ReportCategoryId == 95 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                            if (bcCTHVAEC == null)
                            {
                                bcCTHVAEC = new ReportData()
                                {
                                    Sort = 15,
                                    Month = day.Month,
                                    Year = day.Year,
                                    ReportCategoryId = 95,
                                    Data = "",
                                    HistoryUserId = historyUser.Id,
                                };
                                bcList.Add(bcCTHVAEC);
                            }
                        }
                        if (!listMaHV.Contains(item.MaHV))
                        {
                            var sumSUD = phieuThuAllList.Where(a => a.MaHV == item.MaHV).Sum(a => a.SUD);
                            if (sumSUD > 0)
                            {
                                listMaHV.Add(item.MaHV);
                                // Thực đạt HV NV
                                var bcTDHVNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 96 && a.HistoryUserId == historyUser.Id);
                                if (bcTDHVNV == null)
                                    bcTDHVNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 96 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                                if (bcTDHVNV == null)
                                {
                                    bcTDHVNV = new ReportData()
                                    {
                                        Sort = 16,
                                        Month = day.Month,
                                        Year = day.Year,
                                        ReportCategoryId = 96,
                                        Data = "1",
                                        HistoryUserId = historyUser.Id,
                                        DataReal = 1,
                                    };
                                    bcList.Add(bcTDHVNV);
                                }
                                else
                                {
                                    if (bcTDHVNV.DataReal == null)
                                        bcTDHVNV.DataReal = 0;
                                    bcTDHVNV.DataReal += 1;
                                    bcTDHVNV.Data = (bcTDHVNV.DataReal ?? 0).ToString("N0");
                                }

                                // Tổng số tháng chốt - nhân viên
                                var bcTSTCNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 103 && a.HistoryUserId == historyUser.Id);
                                if (bcTSTCNV == null)
                                    bcTSTCNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 103 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                                if (bcTSTCNV == null)
                                {
                                    bcTSTCNV = new ReportData()
                                    {
                                        Sort = 21,
                                        Month = day.Month,
                                        Year = day.Year,
                                        ReportCategoryId = 103,
                                        Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
                                        HistoryUserId = historyUser.Id,
                                        DataReal = item.ThangHocDuKienDecimal,
                                    };
                                    bcList.Add(bcTSTCNV);
                                }
                                else
                                {
                                    if (bcTSTCNV.DataReal == null)
                                        bcTSTCNV.DataReal = 0;
                                    bcTSTCNV.DataReal += (item.ThangHocDuKienDecimal ?? 0);
                                    bcTSTCNV.Data = (bcTSTCNV.DataReal ?? 0).ToString("N2");
                                }
                            }
                        }
                    }
                    else if (historyUser.TypeUser == TypeUser.CM || historyUser.TypeUser == TypeUser.TTL)
                    {
                        idTyTrong = 43;
                        sortTyTrong = 20;
                    }
                    else if (historyUser.TypeUser == TypeUser.SAB)
                    {
                        idTyTrong = 119;
                        sortTyTrong = 40;
                    }
                    if (idTyTrong != 0)
                    {
                        var bcTiTrongCN = listBCCN.FirstOrDefault(a => a.ReportCategoryId == idTyTrong && a.OfficeId == office.Id);
                        if (bcTiTrongCN == null)
                            bcTiTrongCN = bcList.FirstOrDefault(a => a.ReportCategoryId == idTyTrong && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                        if (bcTiTrongCN == null)
                        {
                            bcTiTrongCN = new ReportData()
                            {
                                Sort = sortTyTrong,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = idTyTrong,
                                Data = (item.SUD ?? 0).ToString("N0"),
                                DataReal = item.SUD,
                                OfficeId = office.Id,
                            };
                            bcList.Add(bcTiTrongCN);
                        }
                        else
                        {
                            if (bcTiTrongCN.DataReal == null)
                                bcTiTrongCN.DataReal = 0;
                            bcTiTrongCN.DataReal += item.SUD ?? 0;
                            bcTiTrongCN.Data = (bcTiTrongCN.DataReal ?? 0).ToString("N0");

                        }
                    }
                    // Thực đạt doanh số NV
                    var bcNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 88 && a.HistoryUserId == historyUser.Id);
                    if (bcNV == null)
                        bcNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 88 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                    if (bcNV == null)
                    {
                        bcNV = new ReportData()
                        {
                            Sort = 11,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 88,
                            Data = (item.SUD ?? 0).ToString("N0"),
                            DataReal = item.SUD ?? 0,
                            UserId = historyUser.UserId,
                            HistoryUserId = historyUser.Id,
                            OfficeId = office.Id,
                        };
                        bcList.Add(bcNV);
                    }
                    else
                    {
                        if (bcNV.DataReal == null)
                            bcNV.DataReal = 0;
                        bcNV.DataReal += item.SUD ?? 0;
                        bcNV.Data = (bcNV.DataReal ?? 0).ToString("N0");
                    }
                    (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(day.Year, day.Month, item.NgayThanhToan.Value);
                    var weekNumber = new WeekNumber();
                    switch (currentWeek)
                    {
                        case 1:
                            weekNumber = WeekNumber.Week1;
                            break;
                        case 2:
                            weekNumber = WeekNumber.Week2;
                            break;
                        case 3:
                            weekNumber = WeekNumber.Week3;
                            break;
                        case 4:
                            weekNumber = WeekNumber.Week4;
                            break;
                        case 5:
                            weekNumber = WeekNumber.Week5;
                            break;
                        case 6:
                            weekNumber = WeekNumber.Week6;
                            break;
                        default:
                            break;
                    }
                    var rUserWeek_Real = rUserWeek_Reals.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && (int)a.WeekNumber == currentWeek);
                    if (rUserWeek_Real == null)
                        rUserWeek_Real = rUserWeek_RealList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Year == day.Year && a.Month == day.Month && (int)a.WeekNumber == currentWeek);
                    if (rUserWeek_Real == null)
                    {
                        rUserWeek_Real = new RevenueUser_Week_Real()
                        {
                            HistoryUserId = historyUser.Id,
                            UserId = historyUser.UserId,
                            Month = day.Month,
                            Year = day.Year,
                            WeekNumber = weekNumber,
                            TargetBM = item.SUD ?? 0,
                        };
                        rUserWeek_RealList.Add(rUserWeek_Real);
                    }
                    else
                    {
                        rUserWeek_Real.TargetBM += item.SUD ?? 0;
                    }
                }
            }

            if (rUserWeek_RealList.Any())
                _unitOfWork.RevenueUser_Week_RealRepository.InsertRange(rUserWeek_RealList);
            // ht phiếu cọc

            var listPhieuCoc = _unitOfWork.PhieuThuRepository.Get(a => a.NgayThanhToan.HasValue && a.NgayThanhToan.Value.Month == day.Month && a.NgayThanhToan.Value.Year == day.Year && a.Loai == "Đặt cọc" &&
            (a.TrangThai == "StatusPayment_Complete" || a.TrangThai == "StatusPayment_Confirm") && !a.THDB);
            var listChiNhanh = listPhieuCoc.Select(a => a.ChiNhanh).Distinct().ToList();
            var listMaNhanVien = listPhieuCoc.Select(a => a.MaNVChotSale).Distinct().ToList();

            // Phiếu cọc chi nhánh
            foreach (var cn in listChiNhanh)
            {
                var office = offices.FirstOrDefault(a => a.Active && a.ShortName == cn);
                if (office == null)
                    continue;
                var phieuCocCN = listPhieuCoc.Where(a => a.ChiNhanh == cn).ToList();
                var tongThangCoc = phieuCocCN.Sum(s => s.ThangHocDuKienDecimal);
                var thangCocDaGop = phieuCocCN.Where(a => a.HDBH != null).Sum(s => s.ThangHocDuKienDecimal);
                var thangCocTon = tongThangCoc - thangCocDaGop;
                // Báo cáo Tổng tháng cọc
                var bctongThangCoc = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 68 && a.OfficeId == office.Id);
                if (bctongThangCoc == null)
                    bctongThangCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 68 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bctongThangCoc == null)
                {
                    bctongThangCoc = new ReportData()
                    {
                        Sort = 1,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 68,
                        Data = (tongThangCoc ?? 0).ToString("N2"),
                        OfficeId = office.Id,
                        DataReal = tongThangCoc,
                    };
                    bcList.Add(bctongThangCoc);
                }
                else
                {
                    bctongThangCoc.DataReal = tongThangCoc;
                    bctongThangCoc.Data = (tongThangCoc ?? 0).ToString("N2");
                }
                // Báo cáo tháng cọc đã gộp
                var bcThangCocDaGop = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 71 && a.OfficeId == office.Id);
                if (bcThangCocDaGop == null)
                    bcThangCocDaGop = bcList.FirstOrDefault(a => a.ReportCategoryId == 71 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcThangCocDaGop == null)
                {
                    bcThangCocDaGop = new ReportData()
                    {
                        Sort = 1,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 71,
                        Data = (thangCocDaGop ?? 0).ToString("N2"),
                        OfficeId = office.Id,
                        DataReal = thangCocDaGop,
                    };
                    bcList.Add(bcThangCocDaGop);
                }
                else
                {
                    bcThangCocDaGop.DataReal = thangCocDaGop;
                    bcThangCocDaGop.Data = (thangCocDaGop ?? 0).ToString("N2");
                }

                // Báo cáo tháng cọc tồn
                var bcThangCocTon = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 76 && a.OfficeId == office.Id);
                if (bcThangCocTon == null)
                    bcThangCocTon = bcList.FirstOrDefault(a => a.ReportCategoryId == 76 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcThangCocTon == null)
                {
                    bcThangCocTon = new ReportData()
                    {
                        Sort = 1,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 76,
                        Data = (thangCocTon ?? 0).ToString("N2"),
                        OfficeId = office.Id,
                        DataReal = thangCocTon,
                    };
                    bcList.Add(bcThangCocTon);
                }
                else
                {
                    bcThangCocTon.DataReal = thangCocTon;
                    bcThangCocTon.Data = (thangCocTon ?? 0).ToString("N2");
                }

                if (tongThangCoc > 0 && thangCocDaGop != null)
                {
                    var tileChuyenDoiCoc = thangCocDaGop / tongThangCoc;
                    // Báo cáo tỉ lệ chuyển đổi cọc
                    var bcTiLeCDCoc = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 73 && a.OfficeId == office.Id);
                    if (bcTiLeCDCoc == null)
                        bcTiLeCDCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 73 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                    if (bcTiLeCDCoc == null)
                    {
                        bcTiLeCDCoc = new ReportData()
                        {
                            Sort = 1,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 73,
                            Data = ((tileChuyenDoiCoc ?? 0) * 100).ToString("F2") + "%",
                            OfficeId = office.Id,
                            DataReal = tileChuyenDoiCoc,
                        };
                        bcList.Add(bcTiLeCDCoc);
                    }
                    else
                    {
                        bcTiLeCDCoc.DataReal = tileChuyenDoiCoc;
                        bcTiLeCDCoc.Data = ((tileChuyenDoiCoc ?? 0) * 100).ToString("F2") + "%";
                    }
                }

                // Doanh thu từ cọc tồn
                var dThuCocTon = phieuCocCN.Where(a => a.HDBH == null).Sum(a => a.SUD);
                var bcdThuCocTon = listBCCN.FirstOrDefault(a => a.ReportCategoryId == 117 && a.OfficeId == office.Id);
                if (bcdThuCocTon == null)
                    bcdThuCocTon = bcList.FirstOrDefault(a => a.ReportCategoryId == 117 && a.Month == day.Month && a.Year == day.Year && a.OfficeId == office.Id);
                if (bcdThuCocTon == null)
                {
                    bcdThuCocTon = new ReportData()
                    {
                        Sort = 1,
                        Month = day.Month,
                        Year = day.Year,
                        ReportCategoryId = 117,
                        Data = (dThuCocTon ?? 0).ToString("N0"),
                        OfficeId = office.Id,
                        DataReal = dThuCocTon,
                    };
                    bcList.Add(bcdThuCocTon);
                }
                else
                {
                    bcdThuCocTon.DataReal = dThuCocTon;
                    bcdThuCocTon.Data = (dThuCocTon ?? 0).ToString("N0");
                }
            }

            //Phiếu cọc nhân sự
            var listbcResetNV = _unitOfWork.ReportDataRepository.Get(a => (a.ReportCategoryId == 111 || a.ReportCategoryId == 113 || a.ReportCategoryId == 114 || a.ReportCategoryId == 115) && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId != null);

            foreach (var mnv in listMaNhanVien)
            {
                //Reset thực đạt NV về 0
                var bcnvs = listbcResetNV.Where(a => a.HistoryUser.User.MaNhanVien == mnv);
                foreach (var bcnv in bcnvs)
                {
                    bcnv.Data = "";
                    bcnv.DataReal = 0;
                }
            }

            foreach (var item in listPhieuCoc)
            {
                var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == item.ChiNhanh.Normalize(NormalizationForm.FormC));
                if (office == null)
                {
                    logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai chi nhanh nao co ten ngan la " + item.ChiNhanh);
                    continue;
                }
                if (!string.IsNullOrEmpty(item.MaNVChotSale))
                {
                    var historyUser = historyUsers.FirstOrDefault(a => a.User.MaNhanVien == item.MaNVChotSale && a.DayStart.Date <= item.NgayThanhToan.Value.Date
                    && (a.DayEnd == null || a.DayEnd.Value.Date >= item.NgayThanhToan.Value.Date));

                    if (historyUser == null)
                    {
                        logger.Error("PhieuThuKeToan " + item.PhieuThuKeToan + ": Khong ton tai nhan su theo thang nao thoa man ngay lam viec: " + item.NgayThanhToan + " va MNV: " + item.MaNVChotSale);
                        continue;
                    }
                    // Tổng tháng cọc

                    var bcTongThangCocNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 111 && a.HistoryUserId == historyUser.Id);
                    if (bcTongThangCocNV == null)
                        bcTongThangCocNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 111 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                    if (bcTongThangCocNV == null)
                    {
                        bcTongThangCocNV = new ReportData()
                        {
                            Sort = 1,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 111,
                            Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
                            DataReal = item.ThangHocDuKienDecimal ?? 0,
                            UserId = historyUser.UserId,
                            HistoryUserId = historyUser.Id,
                            OfficeId = office.Id,
                        };
                        bcList.Add(bcTongThangCocNV);
                    }
                    else
                    {
                        if (bcTongThangCocNV.DataReal == null)
                            bcTongThangCocNV.DataReal = 0;
                        bcTongThangCocNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
                        bcTongThangCocNV.Data = (bcTongThangCocNV.DataReal ?? 0).ToString("N2");
                    }

                    //Tổng tháng đã gộp

                    if (item.HDBH != null)
                    {
                        var bcThangCocDaGopNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 113 && a.HistoryUserId == historyUser.Id);
                        if (bcThangCocDaGopNV == null)
                            bcThangCocDaGopNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 113 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                        if (bcThangCocDaGopNV == null)
                        {
                            bcThangCocDaGopNV = new ReportData()
                            {
                                Sort = 1,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 113,
                                Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
                                DataReal = item.ThangHocDuKienDecimal ?? 0,
                                UserId = historyUser.UserId,
                                HistoryUserId = historyUser.Id,
                                OfficeId = office.Id,
                            };
                            bcList.Add(bcThangCocDaGopNV);
                        }
                        else
                        {
                            if (bcThangCocDaGopNV.DataReal == null)
                                bcThangCocDaGopNV.DataReal = 0;
                            bcThangCocDaGopNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
                            bcThangCocDaGopNV.Data = (bcThangCocDaGopNV.DataReal ?? 0).ToString("N2");
                        }
                        if (bcTongThangCocNV.DataReal > 0)
                        {
                            var tilethuhoicoc = bcThangCocDaGopNV.DataReal / bcTongThangCocNV.DataReal;
                            var bcTiLeThuHoiCoc = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 115 && a.HistoryUserId == historyUser.Id);
                            if (bcTiLeThuHoiCoc == null)
                                bcTiLeThuHoiCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 115 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                            if (bcTiLeThuHoiCoc == null)
                            {
                                bcTiLeThuHoiCoc = new ReportData()
                                {
                                    Sort = 1,
                                    Month = day.Month,
                                    Year = day.Year,
                                    ReportCategoryId = 115,
                                    Data = ((tilethuhoicoc ?? 0) * 100).ToString("F2") + "%",
                                    DataReal = tilethuhoicoc,
                                    UserId = historyUser.UserId,
                                    HistoryUserId = historyUser.Id,
                                    OfficeId = office.Id,
                                };
                                bcList.Add(bcTiLeThuHoiCoc);
                            }
                            else
                            {
                                bcTiLeThuHoiCoc.DataReal = tilethuhoicoc;
                                bcTiLeThuHoiCoc.Data = ((tilethuhoicoc ?? 0) * 100).ToString("F2") + "%";
                            }
                        }
                    }
                    else
                    {
                        var bcThangCocTonNV = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 114 && a.HistoryUserId == historyUser.Id);
                        if (bcThangCocTonNV == null)
                            bcThangCocTonNV = bcList.FirstOrDefault(a => a.ReportCategoryId == 114 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                        if (bcThangCocTonNV == null)
                        {
                            bcThangCocTonNV = new ReportData()
                            {
                                Sort = 1,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 114,
                                Data = (item.ThangHocDuKienDecimal ?? 0).ToString("N2"),
                                DataReal = item.ThangHocDuKienDecimal ?? 0,
                                UserId = historyUser.UserId,
                                HistoryUserId = historyUser.Id,
                                OfficeId = office.Id,
                            };
                            bcList.Add(bcThangCocTonNV);
                        }
                        else
                        {
                            if (bcThangCocTonNV.DataReal == null)
                                bcThangCocTonNV.DataReal = 0;
                            bcThangCocTonNV.DataReal += item.ThangHocDuKienDecimal ?? 0;
                            bcThangCocTonNV.Data = (bcThangCocTonNV.DataReal ?? 0).ToString("N2");
                        }

                        if (bcTongThangCocNV.DataReal > 0)
                        {
                            var tilethuhoicoc = 1 - (bcThangCocTonNV.DataReal / bcTongThangCocNV.DataReal);
                            var bcTiLeThuHoiCoc = listBCNV.FirstOrDefault(a => a.ReportCategoryId == 115 && a.HistoryUserId == historyUser.Id);
                            if (bcTiLeThuHoiCoc == null)
                                bcTiLeThuHoiCoc = bcList.FirstOrDefault(a => a.ReportCategoryId == 115 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == historyUser.Id);
                            if (bcTiLeThuHoiCoc == null)
                            {
                                bcTiLeThuHoiCoc = new ReportData()
                                {
                                    Sort = 1,
                                    Month = day.Month,
                                    Year = day.Year,
                                    ReportCategoryId = 115,
                                    Data = ((tilethuhoicoc ?? 0) * 100).ToString("F2") + "%",
                                    DataReal = tilethuhoicoc,
                                    UserId = historyUser.UserId,
                                    HistoryUserId = historyUser.Id,
                                    OfficeId = office.Id,
                                };
                                bcList.Add(bcTiLeThuHoiCoc);
                            }
                            else
                            {
                                bcTiLeThuHoiCoc.DataReal = tilethuhoicoc;
                                bcTiLeThuHoiCoc.Data = ((tilethuhoicoc ?? 0) * 100).ToString("F2") + "%";
                            }
                        }
                    }
                }
            }
            if (bcList.Any())
                _unitOfWork.ReportDataRepository.InsertRange(bcList);
            _unitOfWork.Save();

            var bcListNew = new List<ReportData>();
            var newDataList = _unitOfWork.ReportDataRepository.Get(a => (a.ReportCategoryId == 35 || a.ReportCategoryId == 88 || a.ReportCategoryId == 96) && a.Month == day.Month && a.Year == day.Year);
            var listReportCategoryIdData = new List<int> { 32, 34, 36, 40, 41, 43, 45, 119, 120, 87, 89, 78, 80, 82, 84, 95, 97, 103, 105 };
            var datasList = _unitOfWork.ReportDataRepository.Get(a => listReportCategoryIdData.Contains(a.ReportCategoryId) && a.Month == day.Month && a.Year == day.Year/* && a.DataReal > 0*/);

            // % ht doanh số cn - nv; %ht HVNV; Cơ cấu DS CN
            foreach (var item in newDataList)
            {
                //var day = item.CreateDate;
                // Data Thực đạt DS CN
                if (item.ReportCategoryId == 35)
                {
                    // Chỉ tiêu DS CN
                    var datact = datasList.FirstOrDefault(a => a.ReportCategoryId == 34 && a.OfficeId == item.OfficeId);
                    if (!(datact?.DataReal > 0))
                    {
                        logger.Error("Chua co chi tieu DS chi nhanh: " + item.Office?.ShortName + " - thang " + item.Month);
                        continue;
                    }
                    // % HT DS CN
                    var ht = item.DataReal / datact.DataReal;
                    var dataht = datasList.FirstOrDefault(a => a.ReportCategoryId == 36 && a.OfficeId == item.OfficeId);
                    if (dataht != null)
                    {
                        dataht.Data = ((ht ?? 0) * 100).ToString("F2") + "%";
                        dataht.DataReal = ht;
                    }
                    else
                    {
                        dataht = new ReportData()
                        {
                            Sort = 15,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 36,
                            Data = ((ht ?? 0) * 100).ToString("F2") + "%",
                            DataReal = ht,
                            OfficeId = item.OfficeId,
                        };
                        bcListNew.Add(dataht);
                    }

                    // Cơ cấu DS các bộ phận (Sale, kế toán, đào tạo)
                    if (item.DataReal > 0)
                    {
                        // Doanh số sale
                        var dataDSSale = datasList.FirstOrDefault(a => a.ReportCategoryId == 40 && a.OfficeId == item.OfficeId);
                        if (dataDSSale == null)
                        {
                            dataDSSale = new ReportData()
                            {
                                Sort = 18,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 40,
                                Data = "0",
                                DataReal = 0,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataDSSale);
                        }
                        // Tỉ trọng sale
                        decimal? tiTrongSale = (dataDSSale.DataReal ?? 0) / item.DataReal;
                        var dataTiTrongSale = datasList.FirstOrDefault(a => a.ReportCategoryId == 41 && a.OfficeId == item.OfficeId);
                        if (dataTiTrongSale != null)
                        {
                            dataTiTrongSale.Data = (tiTrongSale ?? 0).ToString("N2");
                            dataTiTrongSale.DataReal = tiTrongSale;
                        }
                        else
                        {
                            dataTiTrongSale = new ReportData()
                            {
                                Sort = 19,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 41,
                                Data = (tiTrongSale ?? 0).ToString("N2"),
                                DataReal = tiTrongSale,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataTiTrongSale);
                        }

                        // Doanh số đào tạo
                        var dataDSHV = datasList.FirstOrDefault(a => a.ReportCategoryId == 43 && a.OfficeId == item.OfficeId);
                        if (dataDSHV == null)
                        {
                            dataDSHV = new ReportData()
                            {
                                Sort = 20,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 43,
                                Data = "0",
                                DataReal = 0,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataDSHV);
                        }

                        // Tỉ trọng đào tạo
                        decimal? tiTrongHV = (dataDSHV.DataReal ?? 0) / item.DataReal;
                        var dataTiTrongHV = datasList.FirstOrDefault(a => a.ReportCategoryId == 45 && a.OfficeId == item.OfficeId);
                        if (dataTiTrongHV != null)
                        {
                            dataTiTrongHV.Data = (tiTrongHV ?? 0).ToString("N2");
                            dataTiTrongHV.DataReal = tiTrongHV;
                        }
                        else
                        {
                            dataTiTrongHV = new ReportData()
                            {
                                Sort = 21,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 45,
                                Data = (tiTrongHV ?? 0).ToString("N2"),
                                DataReal = tiTrongHV,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataTiTrongHV);
                        }

                        // Doanh số kế toán
                        var dataDSKT = datasList.FirstOrDefault(a => a.ReportCategoryId == 119 && a.OfficeId == item.OfficeId);
                        if (dataDSKT == null)
                        {
                            dataDSKT = new ReportData()
                            {
                                Sort = 40,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 119,
                                Data = "0",
                                DataReal = 0,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataDSKT);
                        }

                        // Tỉ trọng kế toán
                        decimal? tiTrongKT = (dataDSKT.DataReal ?? 0) / item.DataReal;
                        var dataTiTrongKT = datasList.FirstOrDefault(a => a.ReportCategoryId == 120 && a.OfficeId == item.OfficeId);
                        if (dataTiTrongKT != null)
                        {
                            dataTiTrongKT.Data = (tiTrongKT ?? 0).ToString("N2");
                            dataTiTrongKT.DataReal = tiTrongKT;
                        }
                        else
                        {
                            dataTiTrongKT = new ReportData()
                            {
                                Sort = 41,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 120,
                                Data = (tiTrongKT ?? 0).ToString("N2"),
                                DataReal = tiTrongKT,
                                OfficeId = item.OfficeId,
                            };
                            bcListNew.Add(dataTiTrongKT);
                        }
                    }

                }
                // Data Thực đạt DS NV
                else if (item.ReportCategoryId == 88)
                {
                    //var datact = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 87 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId && a.Data != null && a.Data != "0" && a.Data != "").FirstOrDefault();
                    // Tìm báo cáo chỉ tiêu ds của nv
                    var datact = datasList.FirstOrDefault(a => a.ReportCategoryId == 87 && a.HistoryUserId == item.HistoryUserId);
                    if (datact == null)
                        datact = bcListNew.FirstOrDefault(a => a.ReportCategoryId == 87 && a.Month == day.Month && a.Year == day.Year && a.HistoryUserId == item.HistoryUserId);
                    // Nếu không có chỉ tiêu trong db, gán bằng chuỗi rỗng
                    if (datact == null)
                    {
                        logger.Error("Chua co chi tieu DS NV: " + item.HistoryUser?.User.MaNhanVien + " - thang " + item.Month);
                        datact = new ReportData()
                        {
                            Sort = 10,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 87,
                            Data = "",
                            HistoryUserId = item.HistoryUserId,
                            UserId = item.UserId,
                            OfficeId = item.OfficeId,
                        };
                        bcListNew.Add(datact);
                        continue;
                    }
                    if (datact.DataReal == null || datact.DataReal == 0)
                    {
                        logger.Error("Chi tieu DS NV trong: " + item.HistoryUser?.User.MaNhanVien + " - thang " + item.Month);
                        continue;
                    }
                    if (item.DataReal == null)
                        item.DataReal = 0;
                    var ht = (item.DataReal / datact.DataReal) ?? 0;
                    // %ht báo cáo DS NV
                    var dataht = datasList.FirstOrDefault(a => a.ReportCategoryId == 89 && a.HistoryUserId == item.HistoryUserId);
                    if (dataht != null)
                    {
                        dataht.Data = (ht * 100).ToString("F2") + "%";
                        dataht.DataReal = ht;
                    }
                    else
                    {
                        dataht = new ReportData()
                        {
                            Sort = 12,
                            Month = day.Month,
                            Year = day.Year,
                            ReportCategoryId = 89,
                            Data = ht.ToString("F2") + "%",
                            DataReal = ht,
                            HistoryUserId = item.HistoryUserId,
                            UserId = item.UserId,
                            OfficeId = item.OfficeId,
                        };
                        bcListNew.Add(dataht);
                    }

                    // Cơ cấu %ht Sale
                    if (item.HistoryUser?.TypeUser == TypeUser.EC || item.HistoryUser?.TypeUser == TypeUser.ALT)
                    {
                        int idkqhtSale = 0;
                        if (ht >= 1)
                            idkqhtSale = 78;
                        else if (ht >= (decimal)0.3 && ht < (decimal)0.5)
                            idkqhtSale = 80;
                        else if (ht >= (decimal)0.2 && ht < (decimal)0.3)
                            idkqhtSale = 82;
                        else if (ht < (decimal)0.2)
                            idkqhtSale = 84;
                        if (idkqhtSale != 0)
                        {
                            var datakqhtSale = datasList.FirstOrDefault(a => a.ReportCategoryId == idkqhtSale && a.OfficeId == item.OfficeId);
                            if (datakqhtSale == null)
                                datakqhtSale = bcListNew.FirstOrDefault(a => a.ReportCategoryId == idkqhtSale && a.Month == day.Month && a.Year == day.Year && a.OfficeId == item.OfficeId);
                            if (datakqhtSale == null)
                            {
                                datakqhtSale = new ReportData()
                                {
                                    Sort = 1,
                                    Month = day.Month,
                                    Year = day.Year,
                                    ReportCategoryId = idkqhtSale,
                                    Data = "1",
                                    DataReal = 1,
                                    OfficeId = item.OfficeId,
                                };
                                bcListNew.Add(datakqhtSale);
                            }
                            else
                            {
                                if (datakqhtSale.DataReal == null)
                                    datakqhtSale.DataReal = 0;
                                datakqhtSale.DataReal += 1;
                                datakqhtSale.Data = datakqhtSale.DataReal.ToString();
                            }
                        }
                    }


                }
                // Data Thực đạt HV NV; BQ tháng chốt/ HV - nhân viên
                else if (item.ReportCategoryId == 96)
                {
                    // % ht báo cáo HV NV

                    //Chỉ tiêu HV NV
                    var reportCTHVNV = datasList.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.ReportCategoryId == 95);
                    if (reportCTHVNV?.DataReal > 0)
                    {
                        var htHVNV = (item.DataReal ?? 0) / reportCTHVNV.DataReal * 100;
                        var datahtHVNV = datasList.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.ReportCategoryId == 97);
                        if (datahtHVNV == null)
                            datahtHVNV = bcListNew.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 97);
                        if (datahtHVNV == null)
                        {
                            datahtHVNV = new ReportData()
                            {
                                Data = (htHVNV ?? 0).ToString("F2") + "%",
                                DataReal = htHVNV / 100,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 97,
                                HistoryUserId = item.HistoryUserId,
                                Sort = 17,
                            };
                            bcListNew.Add(datahtHVNV);
                        }
                        else
                        {
                            datahtHVNV.Data = (htHVNV ?? 0).ToString("F2") + "%";
                            datahtHVNV.DataReal = htHVNV / 100;
                        }
                    }

                    //BQ tháng chốt/ HV - NV

                    // Tổng số tháng chốt
                    var reportTSTCNV = datasList.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.ReportCategoryId == 103);
                    if (item.DataReal > 0 && reportTSTCNV?.DataReal != null)
                    {
                        var bqTC1HV = reportTSTCNV.DataReal / item.DataReal;
                        var dataBQTC1HV = datasList.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.ReportCategoryId == 105);
                        if (dataBQTC1HV == null)
                            dataBQTC1HV = bcListNew.FirstOrDefault(a => a.HistoryUserId == item.HistoryUserId && a.Month == day.Month && a.Year == day.Year && a.ReportCategoryId == 105);
                        if (dataBQTC1HV == null)
                        {
                            dataBQTC1HV = new ReportData()
                            {
                                Data = (bqTC1HV ?? 0).ToString("N2"),
                                DataReal = bqTC1HV,
                                Month = day.Month,
                                Year = day.Year,
                                ReportCategoryId = 105,
                                HistoryUserId = item.HistoryUserId,
                                Sort = 22,
                            };
                            bcListNew.Add(dataBQTC1HV);
                        }
                        else
                        {
                            dataBQTC1HV.Data = (bqTC1HV ?? 0).ToString("N2");
                            dataBQTC1HV.DataReal = bqTC1HV;
                        }
                    }
                }
            }

            if (bcListNew.Any())
                _unitOfWork.ReportDataRepository.InsertRange(bcListNew);

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
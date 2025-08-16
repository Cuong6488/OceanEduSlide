using Fido2NetLib;
using Newtonsoft.Json;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using Org.BouncyCastle.Asn1.X509;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Z.EntityFramework.Plus;
using NLog;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    [ForcePasswordChangeFilter]
    public class ReportHomeController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            int currentMonth = Month ?? DateTime.Now.Month;
            int currentYear = Year ?? DateTime.Now.Year;
            int pageNumber = page ?? 1;

            // 1. Truy vấn danh sách Office theo quyền truy cập
            var officeQuery = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsQueryable();

            if (User.TypeUser == TypeUser.CV)
            {
                officeQuery = officeQuery.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                officeQuery = officeQuery.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                officeQuery = officeQuery.Where(a => a.Id == User.OfficeId);
            }

            if (ZoneId.HasValue)
            {
                officeQuery = officeQuery.Where(a => a.ZoneId == ZoneId.Value);
            }

            var allOffices = officeQuery.AsNoTracking().ToList();
            var officeIds = allOffices.Select(o => o.Id).ToList();

            // 2. Truy vấn ReportData cho chỉ ReportCategoryId == 35
            var reportDataRaw = _unitOfWork.ReportDataRepository
                .GetQuery(a =>
                    a.Active &&
                    a.Month == currentMonth &&
                    a.Year == currentYear &&
                    a.ReportCategory.TypeCat == TypeCat.Type1 &&
                    a.ReportCategoryId == 35 &&
                    officeIds.Contains(a.OfficeId)) // giới hạn trong office được truy cập
                .Select(a => new { a.OfficeId, a.Data })
                .AsNoTracking()
                .ToList();

            // 3. Tính tổng theo OfficeId
            var officeDataDict = reportDataRaw
                .GroupBy(r => r.OfficeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r =>
                    {
                        int val;
                        var cleaned = r.Data?.Replace(".", "").Replace(",", "") ?? "0";
                        return int.TryParse(cleaned, out val) ? val : 0;
                    })
                );

            // 4. Sắp xếp Offices theo tổng ReportData giảm dần
            var sortedOffices = allOffices
                .OrderByDescending(o => officeDataDict.ContainsKey(o.Id) ? officeDataDict[o.Id] : 0)
                .ToPagedList(pageNumber, 15);

            var pagedOfficeIds = sortedOffices.Select(o => o.Id).ToList();

            // 5. Truy vấn ReportData (đầy đủ) cho các Office trong trang hiện tại
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
                a.Active &&
                a.Month == currentMonth &&
                a.Year == currentYear &&
                a.ReportCategory.TypeCat == TypeCat.Type1 &&
                pagedOfficeIds.Contains(a.OfficeId))
                .AsNoTracking()
                .ToList();

            // 6. Chuẩn bị ViewModel
            var model = new ListReportHomeViewModel
            {
                Month = currentMonth,
                Year = currentYear,
                User = User,
                ZoneId = ZoneId,
                Offices = sortedOffices,
                ReportCategories = _unitOfWork.ReportCategoryRepository
                    .GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1,
                              q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort))
                    .AsNoTracking(),
                ReportDatas = reportDatas
            };

            // 7. Gán zone cho model theo quyền
            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository
                    .Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
            }
            else
            {
                model.ZoneId = User.ZoneId;
            }

            // 8. OfficeIds cho ViewBag
            ViewBag.OfficeIds = "," + string.Join(",", reportDatas.Select(d => d.OfficeId).Distinct()) + ",";

            return View(model);
        }

        //public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year)
        //{
        //    if (User.TypeUser == null)
        //        return HttpNotFound();

        //    int currentMonth = Month ?? DateTime.Now.Month;
        //    int currentYear = Year ?? DateTime.Now.Year;
        //    int pageNumber = page ?? 1;

        //    // Truy vấn ReportData: chỉ lấy các field cần thiết để tính toán
        //    var reportDataRaw = _unitOfWork.ReportDataRepository
        //        .GetQuery(a =>
        //            a.Active &&
        //            a.Month == currentMonth &&
        //            a.Year == currentYear &&
        //            a.ReportCategory.TypeCat == TypeCat.Type1 &&
        //            a.ReportCategoryId == 35)
        //        .Select(a => new { a.OfficeId, a.Data }) // giảm payload rất nhiều
        //        .AsNoTracking()
        //        .ToList();

        //    // Tính tổng theo OfficeId
        //    var officeDataDict = reportDataRaw
        //        .GroupBy(r => r.OfficeId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.Sum(r =>
        //            {
        //                int val;
        //                var cleaned = r.Data?.Replace(".", "").Replace(",", "") ?? "0";
        //                return int.TryParse(cleaned, out val) ? val : 0;
        //            })
        //        );

        //    // Truy vấn Office kèm theo điều kiện lọc
        //    IQueryable<Office> officeQuery = _unitOfWork.OfficeRepository
        //        .GetQuery(a => a.Active)
        //        .AsNoTracking(); // nhẹ hơn

        //    // Lọc theo quyền user
        //    if (User.TypeUser == TypeUser.CV)
        //    {
        //        officeQuery = officeQuery.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
        //    }
        //    else if (User.TypeUser == TypeUser.ASM)
        //    {
        //        officeQuery = officeQuery.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
        //    }
        //    else if (User.TypeUser != TypeUser.HO)
        //    {
        //        officeQuery = officeQuery.Where(a => a.Id == User.OfficeId);
        //    }

        //    if (ZoneId.HasValue)
        //    {
        //        officeQuery = officeQuery.Where(a => a.ZoneId == ZoneId.Value);
        //    }

        //    // Lấy danh sách offices đã lọc và sắp xếp
        //    var filteredOffices = officeQuery
        //        .ToList() // chỉ ToList khi đã có filter
        //        .OrderByDescending(o => officeDataDict.ContainsKey(o.Id) ? officeDataDict[o.Id] : 0)
        //        .ToPagedList(pageNumber, 15);

        //    // Lấy danh sách ReportData (full) cho hiển thị
        //    var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
        //        a.Active &&
        //        a.Month == currentMonth &&
        //        a.Year == currentYear &&
        //        a.ReportCategory.TypeCat == TypeCat.Type1);

        //    if (ZoneId.HasValue)
        //    {
        //        reportDatas = reportDatas.Where(a => a.Office.ZoneId == ZoneId.Value);
        //    }

        //    var model = new ListReportHomeViewModel
        //    {
        //        Month = currentMonth,
        //        Year = currentYear,
        //        User = User,
        //        ZoneId = ZoneId,
        //        Offices = filteredOffices,
        //        ReportCategories = _unitOfWork.ReportCategoryRepository
        //            .GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1,
        //                      q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort))
        //            .AsNoTracking(),
        //        ReportDatas = reportDatas.AsNoTracking()
        //    };

        //    // Zones theo quyền
        //    if (User.TypeUser == TypeUser.HO)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
        //    }
        //    else if (User.TypeUser == TypeUser.CV)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository
        //            .Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
        //    }
        //    else
        //    {
        //        model.ZoneId = User.ZoneId;
        //    }

        //    // Gán danh sách OfficeId cho ViewBag
        //    ViewBag.OfficeIds = "," + string.Join(",", model.ReportDatas.Select(d => d.OfficeId).Distinct()) + ",";

        //    return View(model);
        //}
        //public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        //{
        //    if (User.TypeUser == null)
        //        return HttpNotFound();

        //    var pageNumber = page ?? 1;
        //    var selectedMonth = Month ?? DateTime.Now.Month;
        //    var selectedYear = Year ?? DateTime.Now.Year;

        //    // Lấy dữ liệu báo cáo theo điều kiện
        //    var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active
        //            && a.Month == selectedMonth
        //            && a.Year == selectedYear
        //            && a.ReportCategory.TypeCat == TypeCat.Type2
        //            && a.ReportCategoryId == 88,
        //        q => q.OrderBy(a => a.Sort))
        //        .ToList();

        //    // Lấy danh sách user có OfficeId và TypeUser
        //    var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.TypeUser != null && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.OfficeId != null,
        //            q => q.OrderBy(a => a.OfficeId))
        //        .ToList();

        //    // Tính tổng giá trị reportData cho từng user (đã parse số liệu)
        //    var userDataDict = reportDatas
        //        .GroupBy(r => r.UserId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.Sum(r =>
        //            {
        //                int val;
        //                var cleanedData = r.Data?.Replace(",", "");
        //                return int.TryParse(cleanedData, out val) ? val : 0;
        //            })
        //        );

        //    // Sắp xếp user theo tổng giá trị data giảm dần rồi theo OfficeId
        //    var sortedUsers = users
        //        .OrderByDescending(o => userDataDict.ContainsKey(o.Id) ? userDataDict[o.Id] : 0)
        //        .ThenBy(a => a.OfficeId);

        //    IEnumerable<User> filteredUsers = sortedUsers;

        //    // Tạo model view
        //    var model = new ListReportNVHomeViewModel
        //    {
        //        Month = selectedMonth,
        //        Year = selectedYear,
        //        Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
        //        User = User,
        //        ZoneId = ZoneId,
        //        ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
        //        ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == selectedMonth && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Sort)),
        //        OfficeId = OfficeId,
        //    };

        //    // Phân quyền theo TypeUser để lọc vùng, chi nhánh, nhân viên
        //    if (User.TypeUser == TypeUser.HO)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
        //    }
        //    else if (User.TypeUser == TypeUser.CV)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
        //        model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
        //        filteredUsers = filteredUsers.Where(a => User.ZoneIds.Contains("," + a.Office.Zone?.ShortCode + ","));
        //    }
        //    else
        //    {
        //        model.ZoneId = User.ZoneId;

        //        if (User.TypeUser == TypeUser.ASM)
        //        {
        //            model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
        //            filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
        //        }
        //        else
        //        {
        //            model.OfficeId = User.OfficeId;
        //            filteredUsers = filteredUsers.Where(a => a.OfficeId == User.OfficeId);
        //        }
        //    }

        //    if (model.ZoneId != null)
        //    {
        //        model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
        //        filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);
        //    }

        //    if (model.OfficeId != null)
        //    {
        //        model.ReportDatas = model.ReportDatas.Where(a => a.User?.OfficeId == model.OfficeId);
        //        filteredUsers = filteredUsers.Where(a => a.OfficeId == model.OfficeId);
        //        ViewBag.OfficeIds = "," + string.Join(",", model.ReportDatas.Select(d => d.OfficeId)) + ",";
        //    }

        //    // Phân trang cho danh sách user
        //    model.Users = filteredUsers.ToPagedList(pageNumber, 15);

        //    // Tạo chuỗi MaNhanViens dùng để phân biệt user có dữ liệu báo cáo
        //    string manhanviens = ",";
        //    foreach (var item in model.ReportDatas)
        //    {
        //        if (!manhanviens.Contains("," + item.User?.MaNhanVien + ","))
        //            manhanviens += item.User?.MaNhanVien + ",";
        //    }

        //    ViewBag.MaNhanViens = manhanviens;

        //    return View(model);
        //}
        //public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        //{
        //    if (User.TypeUser == null)
        //        return HttpNotFound();

        //    var pageNumber = page ?? 1;
        //    var selectedMonth = Month ?? DateTime.Now.Month;
        //    var selectedYear = Year ?? DateTime.Now.Year;

        //    // Lấy tất cả user theo phân quyền
        //    var users = _unitOfWork.UserRepository.GetQuery(a =>
        //            a.Active &&
        //            a.TypeUser != null &&
        //            a.TypeUser != TypeUser.HO &&
        //            a.TypeUser != TypeUser.CV &&
        //            a.TypeUser != TypeUser.PKT &&
        //            a.TypeUser != TypeUser.ASM &&
        //            a.OfficeId != null,
        //        q => q.OrderBy(a => a.OfficeId)).ToList();

        //    IEnumerable<User> filteredUsers = users;

        //    // Lấy danh sách office và zone cho model
        //    var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort));
        //    var zones = _unitOfWork.ZoneRepository.Get(a => a.Active);

        //    var model = new ListReportNVHomeViewModel
        //    {
        //        Month = selectedMonth,
        //        Year = selectedYear,
        //        Offices = offices,
        //        User = User,
        //        ZoneId = ZoneId,
        //        ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
        //        OfficeId = OfficeId
        //    };

        //    // Phân quyền
        //    if (User.TypeUser == TypeUser.HO)
        //    {
        //        model.Zones = zones;
        //    }
        //    else if (User.TypeUser == TypeUser.CV)
        //    {
        //        model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ","));
        //        model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));

        //        filteredUsers = filteredUsers.Where(a => User.ZoneIds.Contains("," + a.Office.Zone?.ShortCode + ","));
        //    }
        //    else
        //    {
        //        model.ZoneId = User.ZoneId;

        //        if (User.TypeUser == TypeUser.ASM)
        //        {
        //            model.Offices = offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
        //            filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
        //        }
        //        else
        //        {
        //            model.OfficeId = User.OfficeId;
        //            filteredUsers = filteredUsers.Where(a => a.OfficeId == User.OfficeId);
        //        }
        //    }

        //    if (model.ZoneId != null)
        //    {
        //        model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
        //        filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);
        //    }

        //    if (model.OfficeId != null)
        //    {
        //        filteredUsers = filteredUsers.Where(a => a.OfficeId == model.OfficeId);
        //    }

        //    // Phân trang
        //    var pagedUsers = filteredUsers.ToPagedList(pageNumber, 15);
        //    model.Users = pagedUsers;

        //    // Lấy ID của user trong trang hiện tại
        //    var userIdsInPage = pagedUsers.Select(u => u.Id).ToList();

        //    // Lấy dữ liệu ReportData chỉ cho user trong trang hiện tại
        //    var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
        //            a.Active &&
        //            a.Month == selectedMonth &&
        //            a.Year == selectedYear &&
        //            a.ReportCategory.TypeCat == TypeCat.Type2 &&
        //            userIdsInPage.Contains(a.UserId ?? 0),
        //        q => q.OrderBy(a => a.Sort)).ToList();

        //    model.ReportDatas = reportDatas;

        //    // Tạo danh sách MaNhanVien để hiển thị user có dữ liệu
        //    var maNhanViens = "," + string.Join(",", reportDatas.Select(d => d.User?.MaNhanVien).Where(x => !string.IsNullOrEmpty(x)).Distinct()) + ",";
        //    ViewBag.MaNhanViens = maNhanViens;

        //    if (model.OfficeId != null)
        //    {
        //        ViewBag.OfficeIds = "," + string.Join(",", reportDatas.Select(d => d.OfficeId).Distinct()) + ",";
        //    }

        //    return View(model);
        //}
        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
            var selectedMonth = Month ?? DateTime.Now.Month;
            var selectedYear = Year ?? DateTime.Now.Year;

            //var users = _unitOfWork.UserRepository.GetQuery(a =>
            //        a.Active &&
            //        a.TypeUser != null &&
            //        a.TypeUser != TypeUser.HO &&
            //        a.TypeUser != TypeUser.CV &&
            //        a.TypeUser != TypeUser.PKT &&
            //        a.TypeUser != TypeUser.ASM &&
            //        a.OfficeId != null,
            //    q => q.OrderBy(a => a.OfficeId)).ToList();
            //var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a =>
            //                   a.Active && a.Month == selectedMonth && a.Year == selectedYear && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value.Day != 1)) &&
            //                   a.TypeUser != TypeUser.HO &&
            //                   a.TypeUser != TypeUser.CV &&
            //                   a.TypeUser != TypeUser.PKT &&
            //                   a.TypeUser != TypeUser.ASM &&
            //                   a.OfficeId != null,
            //               q => q.OrderBy(a => a.OfficeId));
            var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a =>
    a.Active &&
    a.Month == selectedMonth &&
    a.Year == selectedYear &&
    (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value.Day != 1)) &&
    a.TypeUser != TypeUser.HO &&
    a.TypeUser != TypeUser.CV &&
    a.TypeUser != TypeUser.PKT &&
    a.TypeUser != TypeUser.ASM &&
    a.OfficeId != null);
            //IEnumerable<User> filteredUsers = users;
            //IEnumerable<HistoryUser> filteredHistoryUsers = historyUsers;

            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort));
            var zones = _unitOfWork.ZoneRepository.Get(a => a.Active);

            var model = new ListReportNVHomeViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                Offices = offices,
                User = User,
                ZoneId = ZoneId,
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
                OfficeId = OfficeId
            };

            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = zones;
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ","));
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                //filteredUsers = filteredUsers.Where(a => User.ZoneIds.Contains("," + a.Office.Zone?.ShortCode + ","));
                historyQuery = historyQuery.Where(a => a.OfficeId != null && a.Office.ZoneId != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ","));
            }
            else
            {
                model.ZoneId = User.ZoneId;

                if (User.TypeUser == TypeUser.ASM)
                {
                    model.Offices = offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                    //filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
                    historyQuery = historyQuery.Where(a => a.OfficeId != null && User.Zone.OfficeIds.Contains("," + a.OfficeId.ToString() + ","));
                }
                else
                {
                    model.OfficeId = User.OfficeId;
                    //filteredUsers = filteredUsers.Where(a => a.OfficeId == User.OfficeId);
                    historyQuery = historyQuery.Where(a => a.OfficeId == User.OfficeId);
                }
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                //filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);
                historyQuery = historyQuery.Where(a => a.Office.ZoneId == model.ZoneId);
            }

            if (model.OfficeId != null)
            {
                //filteredUsers = filteredUsers.Where(a => a.OfficeId == model.OfficeId);
                historyQuery = historyQuery.Where(a => a.OfficeId == model.OfficeId);
            }
            IEnumerable<HistoryUser> filteredHistoryUsers = historyQuery.OrderBy(a => a.OfficeId).ToList();

            // LẤY ReportData CHỈ CHO CategoryId == 88 (dùng để sort user)
            //var userIds = filteredUsers.Select(u => u.Id).ToList();
            //var userIds = filteredUsers.Select(u => u.Id).ToList();
            var historyUserIds = filteredHistoryUsers.Select(h => h.Id).ToList();
            var reportData88 = _unitOfWork.ReportDataRepository.GetQuery(a =>
                a.Active &&
                a.Month == selectedMonth &&
                a.Year == selectedYear &&
                a.ReportCategory.TypeCat == TypeCat.Type2 &&
                a.ReportCategoryId == 88 &&
                historyUserIds.Contains(a.HistoryUserId ?? 0)).ToList();

            // Tính tổng
            var userDataDict = reportData88
                .GroupBy(r => r.HistoryUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r =>
                    {
                        int val;
                        var cleanedData = r.Data?.Replace(",", "");
                        return int.TryParse(cleanedData, out val) ? val : 0;
                    })
                );

            // SẮP XẾP LẠI USER TRƯỚC KHI PHÂN TRANG
            filteredHistoryUsers = filteredHistoryUsers
                .OrderByDescending(u => userDataDict.ContainsKey(u.Id) ? userDataDict[u.Id] : 0)
                .ThenBy(u => u.OfficeId);

            // PHÂN TRANG
            var pagedUsers = filteredHistoryUsers.ToPagedList(pageNumber, 15);
            //model.Users = pagedUsers;
            model.HistoryUsers = pagedUsers;

            var userIdsInPage = pagedUsers.Select(u => u.Id).ToList();

            // Lấy reportData của user trong trang hiện tại (tất cả category)
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
                    a.Active &&
                    a.Month == selectedMonth &&
                    a.Year == selectedYear &&
                    a.ReportCategory.TypeCat == TypeCat.Type2 &&
                    userIdsInPage.Contains(a.HistoryUserId ?? 0),
                q => q.OrderBy(a => a.Sort)).ToList();

            model.ReportDatas = reportDatas;

            // Tạo MaNhanViens
            var maNhanViens = "," + string.Join(",", reportDatas.Select(d => d.User?.MaNhanVien).Where(x => !string.IsNullOrEmpty(x)).Distinct()) + ",";
            ViewBag.MaNhanViens = maNhanViens;

            if (model.OfficeId != null)
            {
                ViewBag.OfficeIds = "," + string.Join(",", reportDatas.Select(d => d.OfficeId).Distinct()) + ",";
            }

            return View(model);
        }

        #region CallLogs
        public async Task<ActionResult> Sync()
        {
            await SyncCallLogsAsync();
            return Content("Đã đồng bộ xong các cuộc gọi");
        }

        private async Task SyncCallLogsAsync()
        {
            int daysToCheck = 3;
            DateTime today = DateTime.Today;

            for (int i = 1; i <= daysToCheck; i++)
            {
                DateTime day = today.AddDays(-i);

                bool hasData = _unitOfWork.CallLogRepository
                    .GetQuery(x => DbFunctions.TruncateTime(x.CallDate) == day)
                    .Any();

                if (!hasData)
                {
                    //logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " khong co du lieu");
                    await FetchAndSaveLogsAsync(day);
                }
                else
                {
                    logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " da co du lieu");
                }
            }
        }

        private async Task FetchAndSaveLogsAsync(DateTime day)
        {
            string user = "lvd";
            string pass = "qazplm123`$%^";
            string baseUrl = "https://voip.ocean.edu.vn/api/report.php";

            string tbegin = day.ToString("yyyy/MM/dd");
            string tend = day.AddDays(1).ToString("yyyy/MM/dd");

            string url = $"{baseUrl}?user={user}&pass={Uri.EscapeDataString(pass)}&tbegin={tbegin}&tend={tend}&type=1";
            logger.Info(url);
            using (var http = new HttpClient())
            {
                try
                {
                    var json = await http.GetStringAsync(url);
                    if (string.IsNullOrEmpty(json) || json?.Length < 10)
                    {
                        logger.Info(json);
                    }
                    var allLogs = JsonConvert.DeserializeObject<List<CallLog>>(json);
                    if (allLogs == null || allLogs.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No logs found for {day:yyyy-MM-dd}");
                        logger.Info("Khong co ban ghi nao ngay " + day.ToString("dd/MM/yyyy"));
                        return;
                    }

                    var logs = allLogs.ToList();
                    var uniqueIds = logs.Select(l => l.UniqueId).Distinct().ToList();

                    // ✅ Chia nhỏ truy vấn để tránh lỗi biểu thức dài
                    var existingUniqueIds = new HashSet<string>();
                    int batchSize = 500;
                    for (int i = 0; i < uniqueIds.Count; i += batchSize)
                    {
                        var batch = uniqueIds.Skip(i).Take(batchSize).ToList();
                        var batchIds = _unitOfWork.CallLogRepository
                            .GetQuery(c => batch.Contains(c.UniqueId))
                            .Select(c => c.UniqueId)
                            .ToList();
                        foreach (var id in batchIds)
                            existingUniqueIds.Add(id);
                    }

                    var newLogs = logs.Where(log => !existingUniqueIds.Contains(log.UniqueId)).ToList();

                    if (!newLogs.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ No new logs to sync for {day:yyyy-MM-dd}");
                        logger.Info("Khong co ban ghi moi nao ngay " + day.ToString("dd/MM/yyyy"));

                        return;
                    }

                    // ✅ Tải toàn bộ người dùng 1 lần duy nhất
                    var userDict = _unitOfWork.UserRepository.GetQuery(u => !string.IsNullOrEmpty(u.MaNhanVien) && u.Active).ToList()
                                    .GroupBy(u => u.MaNhanVien).Select(g => g.First()).ToDictionary(u => u.MaNhanVien, u => u.Id);

                    // ✅ Lọc các logs không tìm thấy user
                    newLogs = newLogs
                        .Where(log =>
                        {
                            if (userDict.TryGetValue(log.Exten, out var userId))
                            {
                                log.UserId = userId;
                                //log.CallDateString = log.CallDate.ToString("dd/MM/yyyy"); // nếu cần
                                return true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No user found for Exten {log.Exten}. Skipping log.");
                                return false;
                            }
                        })
                        .ToList();

                    if (newLogs.Any())
                    {
                        _unitOfWork.CallLogRepository.InsertRange(newLogs);
                        _unitOfWork.Save();
                        System.Diagnostics.Debug.WriteLine($"✓ Synced {newLogs.Count} new call logs for {day:yyyy-MM-dd}");
                        logger.Info("Cap nhat du lieu thanh cong ngay " + day.ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ No matching users for new logs on {day:yyyy-MM-dd}");
                        logger.Info("Khong co user nao trung khop - ngay " + day.ToString("dd/MM/yyyy"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error syncing {day:yyyy-MM-dd}: {ex.Message}");
                    logger.Error("Loi xay ra: " + ex.Message + day.ToString("dd/MM/yyyy"));
                }
            }
        }
        public ActionResult ReportCall(int? page, int? ZoneId, int? OfficeId, string startDay, string endDay)
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            var pageNumber = page ?? 1;

            if (string.IsNullOrEmpty(startDay))
                startDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            if (string.IsNullOrEmpty(endDay))
                endDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            var model = new ListCallViewModel
            {
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
                StartDay = startDay,
                EndDay = endDay
            };
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
            }
            else
            {
                model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                    model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                else
                    model.OfficeId = User.OfficeId;
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                //model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
            }
            if (model.OfficeId != null && !string.IsNullOrEmpty(startDay) && !string.IsNullOrEmpty(endDay))
            {
                DateTime StartDate = new DateTime();
                DateTime EndDate = new DateTime();
                if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                {
                    StartDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                }
                if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                {
                    EndDate = new DateTime(crd.Year, crd.Month, crd.Day, 0, 0, 0);
                }
                var startDate = StartDate.Date;
                var endDate = EndDate.Date.AddDays(1);
                var callData = _unitOfWork.CallLogRepository.GetQuery(p => p.CallDate >= startDate && p.CallDate < endDate && p.User.OfficeId == model.OfficeId);
                var aggregated = callData.GroupBy(p => p.UserId).Select(g => new
                {
                    UserId = g.Key,
                    Over120s = g.Count(x => x.BillSec > 120),
                    Over90s = g.Count(x => x.BillSec > 90 && x.BillSec <= 120),
                    Over60s = g.Count(x => x.BillSec >= 60 && x.BillSec <= 90),
                    Under60s = g.Count(x => x.BillSec >= 30 && x.BillSec < 60),
                    Under30s = g.Count(x => x.BillSec < 30 && x.Disposition == "ANSWERED"),
                    NoAns = g.Count(x => x.Disposition == "NO ANSWER"),
                    Busy = g.Count(x => x.Disposition == "BUSY"),
                    Failed = g.Count(x => x.Disposition == "FAILED"),
                    TotalOver60s = g.Count(x => x.BillSec >= 60),
                    TotalOver30s = g.Count(x => x.BillSec >= 30),
                    Total = g.Count(),
                }).ToList();
                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery();
                //var users = _unitOfWork.UserRepository.Get(a => a.TypeUser != null && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.OfficeId == model.OfficeId);
                var users = _unitOfWork.UserRepository.GetQuery().Where(a => historyUsers.Any(h => h.UserId == a.Id &&
                                      h.OfficeId == model.OfficeId && (h.DayEnd == null || h.DayEnd >= startDate))).ToList();
                var userItems = users.Select(u =>
                {
                    var match = aggregated.FirstOrDefault(x => x.UserId == u.Id);

                    return new ListCallViewModel.UserItem
                    {
                        User = u,
                        Over120s = match?.Over120s ?? 0,
                        Over90s = match?.Over90s ?? 0,
                        Over60s = match?.Over60s ?? 0,
                        Under60s = match?.Under60s ?? 0,
                        Under30s = match?.Under30s ?? 0,
                        NoAns = match?.NoAns ?? 0,
                        Busy = match?.Busy ?? 0,
                        Failed = match?.Failed ?? 0,
                        Total = match?.Total ?? 0,
                        TotalOver30s = match?.TotalOver30s ?? 0,
                        TotalOver60s = match?.TotalOver60s ?? 0,
                    };
                }).ToList();
                model.TotalOver120s = userItems.Sum(a => a.Over120s);
                model.TotalOver90s = userItems.Sum(a => a.Over90s);
                model.TotalOver60s = userItems.Sum(a => a.Over60s);
                model.TotalUnder60s = userItems.Sum(a => a.Under60s);
                model.TotalUnder30s = userItems.Sum(a => a.Under30s);
                model.UserItems = userItems;
            }
            return View(model);

        }
        public PartialViewResult LoadListCall(int userId, int type, string startDay, string endDay)
        {
            var model = new LoadListCallViewModel
            {
                StartDay = startDay,
                EndDay = endDay,
                User = _unitOfWork.UserRepository.GetById(userId),
            };

            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();

            if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                startDate = cd.Date;

            if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                endDate = crd.Date.AddDays(1);

            switch (type)
            {
                case 120:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 120);
                    ViewBag.Type = "trên 2 phút";
                    break;
                case 90:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 90 && p.BillSec <= 120);
                    ViewBag.Type = "trên 1,5 phút";
                    break;
                case 60:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 60 && p.BillSec <= 90);
                    ViewBag.Type = "trên 1 phút";
                    break;
                case 59:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 30 && p.BillSec < 60);
                    ViewBag.Type = "dưới 1 phút";
                    break;
                case 30:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec < 30 && p.Disposition == "ANSWERED");
                    ViewBag.Type = "Dưới 30s";
                    break;
                default:
                    ViewBag.Type = "";
                    break;
            }

            return PartialView(model);
        }
        public ActionResult ClearCallLogs()
        {
            var calllogs = _unitOfWork.CallLogRepository.GetQuery();
            calllogs.Delete();
            return RedirectToAction("ReportCall");
        }

        #endregion
    }
}
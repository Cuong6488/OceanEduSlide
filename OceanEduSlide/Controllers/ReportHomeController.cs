using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using NLog;
using OfficeOpenXml;
using System.Data;
using OceanEduSlide.Migrations;
using System.Web.Services.Protocols;
using System.Drawing.Printing;
using OfficeOpenXml.Style;
using System.Security.Policy;

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
        public ActionResult ReportKDCN(int? page, int? zoneId, int? officeId, int? month, int? year, int? categoryid, int sort = 1)
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            int pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;
            categoryid = categoryid ?? 35;

            month = month ?? DateTime.Now.Month;
            year = year ?? DateTime.Now.Year;

            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Month == month && a.Year == year).AsNoTracking();

            var zones = PermisstionHelper.GetZoneManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value);
            if (zones.Count() == 1)
            {
                zoneId = zones.First().Id;
            }

            var offices = PermisstionHelper.GetOfficeManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value, zoneId);
            var listOfficeId = offices.Select(a => a.Id).ToHashSet();
            if (officeId.HasValue && !listOfficeId.Contains(officeId.Value))
            {
                officeId = null;
            }
            if (offices.Count() == 1)
            {
                officeId = offices.First().Id;
            }

            var allOffices = offices.ToList();
            var officeIds = allOffices.Select(o => o.Id).ToList();
            if (officeId.HasValue)
            {
                allOffices = allOffices.Where(a => a.Id == officeId).ToList();
            }
            var officeDataDict = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == month && a.Year == year && a.ReportCategoryId == categoryid && a.OfficeId.HasValue && officeIds.Contains(a.OfficeId.Value))
                .GroupBy(a => a.OfficeId).Select(g => new
                {
                    OfficeId = g.Key,
                    Total = g.Sum(x => x.DataReal ?? 0)
                })
                .ToDictionary(x => x.OfficeId, x => x.Total);
            // 4. Tách offices có và không có data
            var officeHasData = allOffices.Where(o => officeDataDict.ContainsKey(o.Id)).ToList();
            var officeNoData = allOffices.Where(o => !officeDataDict.ContainsKey(o.Id)).ToList();
            List<Office> sortedAllOffices;
            // 5. Sắp xếp officeHasData
            if (sort == 1)
            {
                officeHasData = officeHasData
                                .OrderByDescending(o => officeDataDict[o.Id])
                                .ToList();
                sortedAllOffices = officeHasData.Concat(officeNoData).ToList();
            }
            else
            {
                officeHasData = officeHasData
                                .OrderBy(o => officeDataDict[o.Id])
                                .ToList();
                sortedAllOffices = officeNoData.Concat(officeHasData).ToList();
            }

            // Gộp lại: officeHasData lên trước, officeNoData ở sau
            // Phân trang
            var sortedOffices = sortedAllOffices.ToPagedList(pageNumber, 15);

            var pagedOfficeIds = sortedOffices.Select(o => o.Id).ToList();

            // 5. Truy vấn ReportData (đầy đủ) cho các Office trong trang hiện tại
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
                a.Active &&
                a.Month == month &&
                a.Year == year &&
                a.ReportCategory.TypeCat == TypeCat.Type1 &&
                pagedOfficeIds.Contains(a.OfficeId ?? 0))
                .AsNoTracking()
                .ToList();

            // 6. Chuẩn bị ViewModel
            var model = new ListReportHomeViewModel
            {
                Year = year,
                Month = month,
                Zones = zones,
                ListOffice = offices,
                ZoneId = zoneId,
                OfficeId = officeId,
                categoryId = categoryid,
                sort = sort,
                Offices = sortedOffices,
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)).AsNoTracking(),
                ReportDatas = reportDatas,
                User = User
            };

            return View(model);
        }
        public void ExportKDCN(int year, int month, int? zoneId, int? officeId)
        {
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Month == month && a.Year == year).AsNoTracking();

            var offices = PermisstionHelper.GetOfficeManagerMonth(_unitOfWork, User, historyUsers, year, month, zoneId);
            if (officeId.HasValue)
                offices = offices.Where(a => a.Id == officeId);
            var officeIds = offices.Select(o => o.Id).ToList();

            // Lấy toàn bộ category và group theo parent
            var categoryParents = _unitOfWork.ReportCategoryRepository
                .GetQuery(a => a.Active && (int)a.TypeCat == 1 && a.ReportCategoryId == null, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort))
                .ToList();

            var allChildCategories = _unitOfWork.ReportCategoryRepository
                .GetQuery(a => a.Active && a.ReportCategoryId != null)
                .ToList();

            var childCategoryDict = allChildCategories
                .GroupBy(c => c.ReportCategoryId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Lấy toàn bộ ReportData theo tháng, năm, office
            var reportDataList = _unitOfWork.ReportDataRepository
                .GetQuery(r => r.Active && r.Month == month && r.Year == year && r.OfficeId != null && officeIds.Contains(r.OfficeId.Value))
                .ToList();

            // Gom dữ liệu report theo OfficeId + CategoryId để truy xuất nhanh
            var reportDataDict = reportDataList
                .GroupBy(r => (r.OfficeId.Value, r.ReportCategoryId))
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.DataReal ?? null);

            // Tạo bảng dữ liệu
            var dt = new DataTable();
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Chi nhánh");

            // Thêm cột động theo category
            foreach (var catParent in categoryParents)
            {
                if (childCategoryDict.TryGetValue(catParent.Id, out var childCats))
                {
                    foreach (var category in childCats)
                    {
                        dt.Columns.Add($"{catParent.Name} - {category.Name}");
                    }
                }
            }

            // Ghi dữ liệu vào bảng
            foreach (var office in offices)
            {
                var listData = new List<string>
        {
            $"{month}/{year}",                 // Tháng
            office.Zone?.Name ?? "",          // Vùng
            office.ShortName                  // Chi nhánh
        };

                foreach (var catParent in categoryParents)
                {
                    if (childCategoryDict.TryGetValue(catParent.Id, out var categories))
                    {
                        foreach (var category in categories)
                        {
                            var key = (office.Id, category.Id);
                            var report = reportDataDict.ContainsKey(key) ? reportDataDict[key] : null;
                            listData.Add(report.ToString());
                        }
                    }
                }

                dt.Rows.Add(listData.ToArray());
            }

            // Xuất Excel
            var filename = $"bao-cao-kdcn.xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Báo cáo KDCN");
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public ActionResult ReportKDNV(int? page, int? zoneId, int? officeId, int? userId, int? userType, int? month, int? year, int? categoryid/*, List<int> ListMonth*/, int sort = 1)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;

            categoryid = categoryid ?? 88;
            month = month ?? DateTime.Now.Month;
            year = year ?? DateTime.Now.Year;

            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Month == month && a.Year == year).AsNoTracking();

            var zones = PermisstionHelper.GetZoneManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value);
            if (zones.Count() == 1)
            {
                zoneId = zones.First().Id;
            }

            var offices = PermisstionHelper.GetOfficeManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value, zoneId);
            var listOfficeId = offices.Select(a => a.Id).ToHashSet();
            if (officeId.HasValue && !listOfficeId.Contains(officeId.Value))
            {
                officeId = null;
            }
            if (offices.Count() == 1)
            {
                officeId = offices.First().Id;
            }

            var users = PermisstionHelper.GetUserManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value, zoneId, officeId);
            var listUserId = users.Select(a => a.Id).ToHashSet();
            if (userId.HasValue && !listUserId.Contains(userId.Value))
            {
                userId = null;
            }
            if (users.Count() == 1)
            {
                userId = users.First().Id;
            }

            var listHistoryUser = PermisstionHelper.GetHistoryUserManagerMonth(_unitOfWork, User, historyUsers, year.Value, month.Value, zoneId, officeId, userId, userType);

            IEnumerable<HistoryUser> filteredHistoryUsers = listHistoryUser.OrderBy(a => a.OfficeId).ToList();

            // LẤY ReportData CHỈ CHO categoryid (dùng để sort user)
            var historyUserIds = filteredHistoryUsers.Select(h => h.Id).ToList();
            var userDataDict = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == month && a.Year == year && a.ReportCategoryId == categoryid && a.HistoryUserId.HasValue
            && listHistoryUser.Select(h => h.Id).Contains(a.HistoryUserId.Value))
                .GroupBy(a => a.HistoryUserId)
                .Select(g => new { HistoryUserId = g.Key, Total = g.Sum(x => x.DataReal ?? 0) })
                .ToDictionary(x => x.HistoryUserId, x => x.Total);
            List<HistoryUser> sortedUsers;
            if (categoryid == null)
            {
                // categoryid == null => sắp xếp theo HistoryUser.Sort
                sortedUsers = filteredHistoryUsers
                    .OrderBy(u => u.Sort)
                    .ThenBy(u => u.OfficeId)
                    .ToList();
            }
            else
            {
                // Có categoryid => sắp xếp theo tổng report data
                var usersHasData = filteredHistoryUsers
                    .Where(u => userDataDict.ContainsKey(u.Id))
                    .ToList();

                var usersNoData = filteredHistoryUsers
                    .Where(u => !userDataDict.ContainsKey(u.Id))
                    .ToList();

                if (sort == 1)
                {
                    // Giảm dần theo tổng
                    usersHasData = usersHasData
                        .OrderByDescending(u => userDataDict[u.Id])
                        .ThenBy(u => u.OfficeId)
                        .ToList();

                    sortedUsers = usersHasData.Concat(usersNoData.OrderBy(u => u.OfficeId)).ToList();
                }
                else
                {
                    // Tăng dần theo tổng
                    usersHasData = usersHasData
                        .OrderBy(u => userDataDict[u.Id])
                        .ThenBy(u => u.OfficeId)
                        .ToList();

                    sortedUsers = usersNoData.OrderBy(u => u.OfficeId).Concat(usersHasData).ToList();
                }
            }

            filteredHistoryUsers = sortedUsers;

            // PHÂN TRANG
            var pagedUsers = filteredHistoryUsers.ToPagedList(pageNumber, 15);

            var userIdsInPage = pagedUsers.Select(u => u.Id).ToList();

            // Lấy reportData của user trong trang hiện tại (tất cả category)
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
                    a.Active &&
                    a.Month == month &&
                    a.Year == year &&
                    a.ReportCategory.TypeCat == TypeCat.Type2 &&
                    userIdsInPage.Contains(a.HistoryUserId ?? 0),
                q => q.OrderBy(a => a.Sort)).ToList();

            var model = new ListReportNVHomeViewModel
            {
                Month = month,
                Year = year,
                Zones = zones,
                Offices = offices,
                Users = users,
                ZoneId = zoneId,
                OfficeId = officeId,
                UserId = userId,
                UserType = userType,
                categoryId = categoryid,
                sort = sort,
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
                ReportDatas = reportDatas,
                HistoryUsers = pagedUsers,
                User = User,
            };
            //// Tạo MaNhanViens
            //var maNhanViens = "," + string.Join(",", reportDatas.Select(d => d.User?.MaNhanVien).Where(x => !string.IsNullOrEmpty(x)).Distinct()) + ",";
            //ViewBag.MaNhanViens = maNhanViens;

            //if (model.OfficeId != null)
            //{
            //    ViewBag.OfficeIds = "," + string.Join(",", reportDatas.Select(d => d.OfficeId).Distinct()) + ",";
            //}
            return View(model);
        }

        public void ExportKDNV(int? ZoneId, int? OfficeId, int? userId, int? UserType, int Month, int Year)
        {
            var lisstHistoryUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Month == Month && a.Year == Year).AsNoTracking();

            var historyQuery = PermisstionHelper.GetHistoryUserManagerMonth(_unitOfWork, User, lisstHistoryUser, Year, Month, ZoneId, OfficeId, userId, UserType);

            if (userId.HasValue)
                historyQuery = historyQuery.Where(a => a.UserId == userId);


            // Include các bảng liên quan để tránh Lazy Loading
            var historyUsers = historyQuery
                .Include(a => a.User)
                .Include(a => a.Zone)
                .Include(a => a.Office)
                .ToList();

            var userIds = historyUsers.Select(x => x.Id).ToList();

            // Lấy toàn bộ ReportData 1 lần
            var reportDataList = _unitOfWork.ReportDataRepository
                .GetQuery(r => r.Active && r.Month == Month && r.Year == Year && userIds.Contains(r.HistoryUserId ?? 0))
                .ToList();

            var reportDataDict = reportDataList
                .GroupBy(r => r.HistoryUserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Lấy toàn bộ ReportCategory và group theo Parent
            var categoryParents = _unitOfWork.ReportCategoryRepository
                .GetQuery(a => a.Active && (int)a.TypeCat == 2 && a.ReportCategoryId == null, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort))
                .ToList();

            var allChildCategories = _unitOfWork.ReportCategoryRepository
                .GetQuery(a => a.Active && a.ReportCategoryId != null)
                .ToList();

            var childCategoryDict = allChildCategories
                .GroupBy(c => c.ReportCategoryId.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Chuẩn bị DataTable
            var dt = new DataTable();
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Mã NV");
            dt.Columns.Add("Họ tên nhân sự");
            dt.Columns.Add("CDCM");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Ngày vào làm");
            dt.Columns.Add("Ngày nghỉ/ điều chuyển");

            // Tạo các cột động từ ReportCategory
            foreach (var catParent in categoryParents)
            {
                if (childCategoryDict.TryGetValue(catParent.Id, out var categories))
                {
                    foreach (var category in categories)
                    {
                        dt.Columns.Add($"{catParent.Name} - {category.Name}");
                    }
                }
            }

            // Đổ dữ liệu từng dòng
            foreach (var historyUser in historyUsers)
            {
                var listData = new List<string>
        {
            $"{Month}/{Year}",
            historyUser.Zone?.Name ?? "",
            historyUser.Office?.ShortName ?? "",
            historyUser.User.MaNhanVien ?? "",
            historyUser.User.Fullname ?? "",
            historyUser.CDCM ?? "",
            EnumHelpers.EnumExtensions.GetDisplayName(historyUser.Status),
            historyUser.DayStart.ToString("dd/MM/yyyy"),
            historyUser.DayEnd?.ToString("dd/MM/yyyy") ?? ""
        };

                var userReports = reportDataDict.ContainsKey(historyUser.Id)
                    ? reportDataDict[historyUser.Id]
                    : new List<ReportData>();

                foreach (var catParent in categoryParents)
                {
                    if (childCategoryDict.TryGetValue(catParent.Id, out var categories))
                    {
                        foreach (var category in categories)
                        {
                            var report = userReports.FirstOrDefault(r => r.ReportCategoryId == category.Id)?.DataReal ?? null;
                            listData.Add(report.ToString());
                        }
                    }
                }

                dt.Rows.Add(listData.ToArray());
            }

            // Xuất Excel
            var filename = $"bao-cao-kdnv.xlsx";

            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Báo cáo KDNV");
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=" + filename);
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public int? CheckUserId(IQueryable<User> listUserSelect, int? UserId)
        {
            var listUserId = listUserSelect.Select(a => a.Id).ToHashSet();
            if (UserId != null)
                if (listUserId.Contains(UserId.Value))
                {
                    return UserId.Value;
                }
            return null;
        }

        public ActionResult ReportTHNV(int? page, int? ZoneId, int? OfficeId, int? UserId, int? UserType, int? Year)
        {
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.BM)
                return HttpNotFound();
            var pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;
            var selectedYear = Year ?? DateTime.Now.Year;
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)).AsNoTracking();
            var zones = _unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking();
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Year == selectedYear && (a.TypeUser == TypeUser.HO || a.TypeUser == TypeUser.CV || a.TypeUser == TypeUser.ASM || a.TypeUser == TypeUser.BM)).AsNoTracking();

            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Year == selectedYear).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            }).AsNoTracking();
            var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == selectedYear
            && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == a.Month) || a.DayEnd.Value.Month != a.Month)))
            && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.BM).AsNoTracking();

            var listUser = _unitOfWork.UserRepository.GetQuery(a => historyQuery.Any(h => h.Active && h.UserId == a.Id)).AsNoTracking();
            var listUserSelect = listUser;

            var listMonth = new List<int>();
            var listMonthFull = new List<int>(Enumerable.Range(1, 12));

            if (UserType != null)
            {
                listUserSelect = listUserSelect.Where(a => (int)a.TypeUser == UserType);
                listUser = listUser.Where(a => (int)a.TypeUser == UserType);
            }

            if (User.TypeUser == TypeUser.HO)
            {
                listMonth = listMonthFull;
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",")));
                if (ZoneId == null)
                {
                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
                    if (OfficeId == null)
                    {
                        listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
                        && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                        || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                        || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
                        UserId = CheckUserId(listUserSelect, UserId);
                        if (UserId == null)
                        {
                            listMonth = listMonthFull;
                            listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
                            && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
                        }
                    }
                }
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                {
                    zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                    if (ZoneId == null)
                    {
                        offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
                        var lOf = offices.ToList();
                        if (OfficeId == null)
                        {
                            listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
                            && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
                            || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
                            UserId = CheckUserId(listUserSelect, UserId);
                            if (UserId == null)
                            {
                                listMonth = listMonthFull;
                                listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
                            && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
                            || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
                            }
                        }
                    }
                }
                else if (User.ZoneId != null)
                {
                    zones = zones.Where(a => User.ZoneId == a.Id || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",")));
                    if (ZoneId == null)
                    {
                        offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneId == h.ZoneId || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
                        if (OfficeId == null)
                        {
                            listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
                            && (User.ZoneId == h.ZoneId
                            || (h.OfficeId != null && User.ZoneId == h.Office.ZoneId)
                            || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
                            if (UserId == null)
                            {
                                listMonth = listMonthFull;
                                listUser = listUserSelect;
                            }
                        }
                    }
                }
            }
            else if (User.TypeUser == TypeUser.BM)
            {
                zones = null;
                if (!string.IsNullOrEmpty(User.OfficeIds))
                {
                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                    if (OfficeId == null)
                    {
                        listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
                        && ((h.OfficeId != null && User.OfficeIds.Contains("," + h.OfficeId + ","))
                        || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                        UserId = CheckUserId(listUserSelect, UserId);
                        if (UserId == null)
                        {
                            listMonth = listMonthFull;
                            listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
                        && ((h.OfficeId != null && User.OfficeIds.Contains("," + h.OfficeId + ","))
                        || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                        }
                    }
                }
                else if (User.OfficeId != null)
                {
                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                    if (OfficeId == null)
                    {
                        listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
                        && (User.OfficeId == h.OfficeId
                        || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                        if (UserId == null)
                        {
                            listMonth = listMonthFull;
                            listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
                        && (User.OfficeId == h.OfficeId
                        || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                        }
                    }
                }
            }
            if (ZoneId != null)
            {
                var zone = _unitOfWork.ZoneRepository.GetById(ZoneId);
                offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == ZoneId) || a.ZoneId == ZoneId);
                if (OfficeId == null)
                {
                    listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id && (ZoneId == h.ZoneId || (h.OfficeId != null && h.Office.ZoneId == ZoneId))));
                    UserId = CheckUserId(listUserSelect, UserId);
                    if (UserId == null)
                    {
                        listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id && (ZoneId == h.ZoneId || (h.OfficeId != null && h.Office.ZoneId == ZoneId))));

                        if (User.TypeUser != TypeUser.HO)
                            if (User.ZoneId == ZoneId || (User.ZoneIds != null && User.ZoneIds.Contains("," + zone.ShortCode + ",")))
                            {
                                listMonth.AddRange(Enumerable.Range(1, 12));
                            }
                            else
                            {
                                listMonth = historyUsers.Where(a => a.ZoneIds != null && a.ZoneIds.Contains("," + zone.ShortCode + ",")).Select(a => a.Month).Distinct().OrderBy(a => a).ToList();
                            }
                    }
                }
            }
            if (OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(OfficeId);
                listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id && OfficeId == h.OfficeId));
                UserId = CheckUserId(listUserSelect, UserId);
                if (UserId == null)
                {
                    listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id && OfficeId == h.OfficeId));

                    if (User.TypeUser != TypeUser.HO)
                        if (User.OfficeId == OfficeId || (User.OfficeIds != null && User.OfficeIds.Contains("," + office.Id + ",")) || (User.ZoneIds != null && office.ZoneId != null && User.ZoneIds.Contains("," + office.Zone.ShortCode + ",")))
                        {
                            listMonth = listMonthFull;
                        }
                        else
                        {
                            listMonth = historyUsers.Where(a =>
                            (a.ZoneIds != null && office.ZoneId != null && a.ZoneIds.Contains("," + office.Zone.ShortCode + ",")) ||
                            (a.OfficeIds != null && a.OfficeIds.Contains("," + OfficeId + ","))).Select(a => a.Month).Distinct().OrderBy(a => a).ToList();
                        }
                }
            }
            UserId = CheckUserId(listUserSelect, UserId);
            if (UserId != null)
            {
                var user = _unitOfWork.UserRepository.GetById(UserId);
                listUser = listUser.Where(a => a.Id == UserId);
                if (User.TypeUser != TypeUser.HO)

                    if ((User.ZoneIds != null && ((user.ZoneId != null && User.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
                    (User.OfficeIds != null && user.OfficeId != null && User.OfficeIds.Contains("," + user.OfficeId + ",")))
                    {
                        listMonth = listMonthFull;
                    }
                    else
                    {
                        var listHU = historyUsers.ToList();
                        listMonth = listHU.Where(hu => (hu.ZoneIds != null && ((user.ZoneId != null && hu.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
                        (hu.OfficeIds != null && user.OfficeId != null && hu.OfficeIds.Contains("," + user.OfficeId + ","))).Select(hu => hu.Month).Distinct().OrderBy(a => a).ToList();
                    }
            }
            var userItems = new List<BCTHNVViewModel.UserItem>();
            var listReportCategoryId = new List<int> { 87, 88, 95, 96, 99, 100, 103 };

            var allUserIds = listUser.Select(u => u.Id).ToList();

            // ================== PAGING USER TRƯỚC ==================
            var pagedUsers = listUser
                .OrderBy(u => u.Id)
                .ToPagedList(pageNumber, 10);

            var pagedUserIds = pagedUsers.Select(u => u.Id).ToList();

            // ================== LOAD HISTORY USER (CHỈ USER TRONG PAGE) ==================
            var historyUsersAll = _unitOfWork.HistoryUserRepository
                .GetQuery(a =>
                    a.Year == selectedYear &&
                    a.Active &&
                    pagedUserIds.Contains(a.UserId))
                .AsNoTracking()
                .ToList();

            var historyUserDict = historyUsersAll
                .GroupBy(x => (x.UserId, x.Month))
                .ToDictionary(g => g.Key, g => g.ToList());

            // ================== LOAD REPORT DATA (CHỈ USER TRONG PAGE) ==================
            var listReportData = _unitOfWork.ReportDataRepository
                .GetQuery(a =>
                    a.Active &&
                    a.Year == selectedYear &&
                    a.ReportCategory.TypeCat == TypeCat.Type2 &&
                    a.HistoryUserId.HasValue &&
                    pagedUserIds.Contains(a.HistoryUser.UserId) &&
                    listReportCategoryId.Contains(a.ReportCategoryId))
                .AsNoTracking()
                .ToList();

            // ================== GỘP REPORT DATA ==================
            var reportAggDict = listReportData
                .GroupBy(x => new { x.Month, x.HistoryUser.UserId, x.ReportCategoryId })
                .Select(g => new
                {
                    g.Key.Month,
                    g.Key.UserId,
                    g.Key.ReportCategoryId,
                    Value = g.Sum(x => x.DataReal ?? 0m)
                })
                .GroupBy(x => (x.UserId, x.Month))
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var a = new AggData();
                        foreach (var i in g)
                        {
                            switch (i.ReportCategoryId)
                            {
                                case 87: a.CT_DS += i.Value; break;
                                case 88: a.TD_DS += i.Value; break;
                                case 99: a.CT_CG += i.Value; break;
                                case 100: a.TD_CG += i.Value; break;
                                case 95: a.CT_HV += i.Value; break;
                                case 96: a.TD_HV += i.Value; break;
                                case 103: a.TongThangChot += i.Value; break;
                            }
                        }
                        return a;
                    });

            // ================== HÀM TÍNH CHUNG ==================
            //void CalcResult(decimal ct, decimal td, decimal tong,
            //    out string ht, out string tb)
            //{
            //    ht = ct > 0 ? (td / ct * 100).ToString("N2") + "%" : "";
            //    tb = td > 0 ? (tong / td).ToString("N0") : "";
            //}

            // ================== LOOP USER ==================
            foreach (var user in pagedUsers)
            {
                var listMonthUser = new List<int>();

                // ==== PHÂN QUYỀN USER ====
                if (User.TypeUser != TypeUser.HO)

                    if ((User.ZoneIds != null && ((user.ZoneId != null && User.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
                    (User.OfficeIds != null && user.OfficeId != null && User.OfficeIds.Contains("," + user.OfficeId + ",")))
                    {
                        listMonthUser = listMonthFull;
                    }
                    else
                    {
                        var listHU = historyUsers.ToList();
                        listMonthUser = listHU.Where(hu => (hu.ZoneIds != null && ((user.ZoneId != null && hu.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
                        (hu.OfficeIds != null && user.OfficeId != null && hu.OfficeIds.Contains("," + user.OfficeId + ","))).Select(hu => hu.Month).Distinct().OrderBy(a => a).ToList();
                    }
                else
                {
                    listMonthUser = listMonthFull;
                }

                var userItem = new BCTHNVViewModel.UserItem
                {
                    User = user,
                    ListHTDSs = new List<string>(),
                    ListHTCGs = new List<string>(),
                    ListHTHVs = new List<string>(),
                    ListTCBQs = new List<string>(),
                };

                // Biến tổng cả năm
                decimal ctDSNam = 0, tdDSNam = 0,
                        ctCGNam = 0, tdCGNam = 0,
                        ctHVNam = 0, tdHVNam = 0,
                        tongThangChotNam = 0;

                // Biến tổng từng quý
                decimal[] ctDSQuy = new decimal[4], tdDSQuy = new decimal[4],
                          ctCGQuy = new decimal[4], tdCGQuy = new decimal[4],
                          ctHVQuy = new decimal[4], tdHVQuy = new decimal[4],
                          tongThangChotQuy = new decimal[4];

                // ================== XỬ LÝ THEO THÁNG ==================
                foreach (var month in listMonth)
                {
                    if (!listMonthUser.Contains(month))
                    {
                        userItem.ListHTDSs.Add("");
                        userItem.ListHTCGs.Add("");
                        userItem.ListHTHVs.Add("");
                        userItem.ListTCBQs.Add("");
                        continue;
                    }

                    if (!reportAggDict.TryGetValue((user.Id, month), out var a))
                        a = new AggData();

                    // Tính phần trăm và tháng chốt bình quân
                    string htDS = a.CT_DS > 0 ? (a.TD_DS / a.CT_DS * 100).ToString("N2") + "%" : "";
                    string htCG = a.CT_CG > 0 ? (a.TD_CG / a.CT_CG * 100).ToString("N2") + "%" : "";
                    string htHV = a.CT_HV > 0 ? (a.TD_HV / a.CT_HV * 100).ToString("N2") + "%" : "";
                    string tbHV = a.TD_HV > 0 ? (a.TongThangChot / a.TD_HV).ToString("N0") : "";

                    userItem.ListHTDSs.Add(htDS);
                    userItem.ListHTCGs.Add(htCG);
                    userItem.ListHTHVs.Add(htHV);
                    userItem.ListTCBQs.Add(tbHV);

                    // Tổng cả năm
                    ctDSNam += a.CT_DS; tdDSNam += a.TD_DS;
                    ctCGNam += a.CT_CG; tdCGNam += a.TD_CG;
                    ctHVNam += a.CT_HV; tdHVNam += a.TD_HV;
                    tongThangChotNam += a.TongThangChot;

                    // Cộng vào quý
                    int quyIndex = (month - 1) / 3;
                    ctDSQuy[quyIndex] += a.CT_DS;
                    tdDSQuy[quyIndex] += a.TD_DS;
                    ctCGQuy[quyIndex] += a.CT_CG;
                    tdCGQuy[quyIndex] += a.TD_CG;
                    ctHVQuy[quyIndex] += a.CT_HV;
                    tdHVQuy[quyIndex] += a.TD_HV;
                    tongThangChotQuy[quyIndex] += a.TongThangChot;
                }

                // ================== XỬ LÝ TỪNG QUÝ ==================
                for (int i = 0; i < 4; i++)
                {
                    var monthsInQuarter = listMonth.Where(m => (m - 1) / 3 == i).ToList();
                    if (!monthsInQuarter.Any()) continue;

                    string htDSQuy = ctDSQuy[i] > 0 ? (tdDSQuy[i] / ctDSQuy[i] * 100).ToString("N2") + "%" : "";
                    string htCGQuy = ctCGQuy[i] > 0 ? (tdCGQuy[i] / ctCGQuy[i] * 100).ToString("N2") + "%" : "";
                    string htHVQuy = ctHVQuy[i] > 0 ? (tdHVQuy[i] / ctHVQuy[i] * 100).ToString("N2") + "%" : "";
                    string tbQuy = tdHVQuy[i] > 0 ? (tongThangChotQuy[i] / tdHVQuy[i]).ToString("N0") : "";

                    userItem.ListHTDSs.Add(htDSQuy);
                    userItem.ListHTCGs.Add(htCGQuy);
                    userItem.ListHTHVs.Add(htHVQuy);
                    userItem.ListTCBQs.Add(tbQuy);
                }

                // ================== KẾT QUẢ CẢ NĂM ==================
                string htDSNamStr = ctDSNam > 0 ? (tdDSNam / ctDSNam * 100).ToString("N2") + "%" : "";
                string htCGNamStr = ctCGNam > 0 ? (tdCGNam / ctCGNam * 100).ToString("N2") + "%" : "";
                string htHVNamStr = ctHVNam > 0 ? (tdHVNam / ctHVNam * 100).ToString("N2") + "%" : "";
                string tbNamStr = tdHVNam > 0 ? (tongThangChotNam / tdHVNam).ToString("N0") : "";

                userItem.ListHTDSs.Add(htDSNamStr);
                userItem.ListHTCGs.Add(htCGNamStr);
                userItem.ListHTHVs.Add(htHVNamStr);
                userItem.ListTCBQs.Add(tbNamStr);

                userItems.Add(userItem);

            }

            // ================== TẠO PAGED LIST ==================
            var pagedUserItems = new StaticPagedList<BCTHNVViewModel.UserItem>(
                userItems,
                pagedUsers.PageNumber,
                pagedUsers.PageSize,
                pagedUsers.TotalItemCount);


            var model = new BCTHNVViewModel
            {
                Zones = zones?.ToList(),
                Offices = offices.ToList(),
                Users = listUserSelect.ToList(),
                UserItems = pagedUserItems,
                Year = selectedYear,
                UserId = UserId,
                OfficeId = OfficeId,
                ZoneId = ZoneId,
                User = User,
                UserType = UserType,
                ListMonth = listMonth
            };



            return View(model);
        }
        //    public ActionResult ReportTHNV(int? page, int? ZoneId, int? OfficeId, int? UserId, int? UserType, int? Year)
        //    {
        //        if (User.TypeUser == null)
        //            return HttpNotFound();
        //        var pageNumber = page ?? 1;
        //        ViewBag.Page = pageNumber;
        //        var selectedYear = Year ?? DateTime.Now.Year;
        //        var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)).AsNoTracking();
        //        var zones = _unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking();
        //        var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Year == selectedYear && (a.TypeUser == TypeUser.HO || a.TypeUser == TypeUser.CV || a.TypeUser == TypeUser.ASM || a.TypeUser == TypeUser.BM)).AsNoTracking();

        //        var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Year == selectedYear).Select(h => new
        //        {
        //            h.OfficeId,
        //            ZoneShortCode = h.Zone.ShortCode,
        //            h.ZoneId
        //        }).AsNoTracking();
        //        var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == selectedYear
        //        && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == a.Month) || a.DayEnd.Value.Month != a.Month)))
        //        && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.BM).AsNoTracking();

        //        var listUser = _unitOfWork.UserRepository.GetQuery(a => historyQuery.Any(h => h.Active && h.UserId == a.Id)).AsNoTracking();
        //        var listUserSelect = listUser;

        //        var listMonth = new List<int>();
        //        var listMonthFull = new List<int>(Enumerable.Range(1, 12));

        //        if (UserType != null)
        //        {
        //            listUserSelect = listUserSelect.Where(a => (int)a.TypeUser == UserType);
        //            listUser = listUser.Where(a => (int)a.TypeUser == UserType);
        //        }

        //        if (User.TypeUser == TypeUser.HO)
        //        {
        //            listMonth = listMonthFull;
        //        }
        //        else if (User.TypeUser == TypeUser.CV)
        //        {
        //            zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",")));
        //            if (ZoneId == null)
        //            {
        //                offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
        //                if (OfficeId == null)
        //                {
        //                    listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                    && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                    || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                    || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
        //                    UserId = CheckUserId(listUserSelect, UserId);
        //                    if (UserId == null)
        //                    {
        //                        listMonth = listMonthFull;
        //                        listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                        && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                        || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                        || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
        //                    }
        //                }
        //            }
        //        }
        //        else if (User.TypeUser == TypeUser.ASM)
        //        {
        //            if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
        //            {
        //                zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
        //                if (ZoneId == null)
        //                {
        //                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));
        //                    if (OfficeId == null)
        //                    {
        //                        listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                        && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                        || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
        //                        || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
        //                        UserId = CheckUserId(listUserSelect, UserId);
        //                        if (UserId == null)
        //                        {
        //                            listMonth = listMonthFull;
        //                            listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                        && ((h.ZoneId != null && User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
        //                        || (h.OfficeId != null && h.Office.ZoneId != null && User.ZoneIds.Contains("," + h.Office.Zone.ShortCode + ","))
        //                        || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
        //                        }
        //                    }
        //                }
        //            }
        //            else if (User.ZoneId != null)
        //            {
        //                zones = zones.Where(a => User.ZoneId == a.Id || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ",")));
        //                if (ZoneId == null)
        //                {
        //                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneId == h.ZoneId || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
        //                    if (OfficeId == null)
        //                    {
        //                        listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                        && (User.ZoneId == h.ZoneId
        //                        || (h.OfficeId != null && User.ZoneId == h.Office.ZoneId)
        //                        || historyUsers.Any(hu => hu.ZoneIds != null && ((h.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")) || (h.OfficeId != null && h.Office.ZoneId != null && hu.ZoneIds.Contains("," + h.Zone.ShortCode + ",")))))));
        //                        if (UserId == null)
        //                        {
        //                            listMonth = listMonthFull;
        //                            listUser = listUserSelect;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else if (User.TypeUser == TypeUser.BM)
        //        {
        //            if (!string.IsNullOrEmpty(User.OfficeIds))
        //            {
        //                offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                if (OfficeId == null)
        //                {
        //                    listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                    && ((h.OfficeId != null && User.OfficeIds.Contains("," + h.OfficeId + ","))
        //                    || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                    UserId = CheckUserId(listUserSelect, UserId);
        //                    if (UserId == null)
        //                    {
        //                        listMonth = listMonthFull;
        //                        listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                    && ((h.OfficeId != null && User.OfficeIds.Contains("," + h.OfficeId + ","))
        //                    || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                    }
        //                }
        //            }
        //            else if (User.OfficeId != null)
        //            {
        //                offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                if (OfficeId == null)
        //                {
        //                    listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                    && (User.OfficeId == h.OfficeId
        //                    || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                    if (UserId == null)
        //                    {
        //                        listMonth = listMonthFull;
        //                        listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id
        //                    && (User.OfficeId == h.OfficeId
        //                    || historyUsers.Any(hu => hu.OfficeIds != null && h.OfficeId != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
        //                    }
        //                }
        //            }
        //        }
        //        if (ZoneId != null)
        //        {
        //            var zone = _unitOfWork.ZoneRepository.GetById(ZoneId);
        //            offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == ZoneId));
        //            if (OfficeId == null)
        //            {
        //                listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id && (ZoneId == h.ZoneId || (h.OfficeId != null && h.Office.ZoneId == ZoneId))));
        //                UserId = CheckUserId(listUserSelect, UserId);
        //                if (UserId == null)
        //                {
        //                    listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id && (ZoneId == h.ZoneId || (h.OfficeId != null && h.Office.ZoneId == ZoneId))));

        //                    if (User.TypeUser != TypeUser.HO)
        //                        if (User.ZoneId == ZoneId || (User.ZoneIds != null && User.ZoneIds.Contains("," + zone.Id + ",")))
        //                        {
        //                            listMonth.AddRange(Enumerable.Range(1, 12));
        //                        }
        //                        else
        //                        {
        //                            listMonth = historyUsers.Where(a => a.ZoneIds != null && a.ZoneIds.Contains("," + zone.ShortCode + ",")).Select(a => a.Month).Distinct().OrderBy(a => a).ToList();
        //                        }
        //                }
        //            }
        //        }
        //        if (OfficeId != null)
        //        {
        //            var office = _unitOfWork.OfficeRepository.GetById(OfficeId);
        //            listUserSelect = listUserSelect.Where(a => historyQuery.Any(h => h.UserId == a.Id && OfficeId == h.OfficeId));
        //            UserId = CheckUserId(listUserSelect, UserId);
        //            if (UserId == null)
        //            {
        //                listUser = listUser.Where(a => historyQuery.Any(h => h.UserId == a.Id && OfficeId == h.OfficeId));

        //                if (User.TypeUser != TypeUser.HO)
        //                    if (User.OfficeId == OfficeId || (User.OfficeIds != null && User.OfficeIds.Contains("," + office.Id + ",")) || (User.ZoneIds != null && office.ZoneId != null && User.ZoneIds.Contains("," + office.Zone.ShortCode + ",")))
        //                    {
        //                        listMonth = listMonthFull;
        //                    }
        //                    else
        //                    {
        //                        listMonth = historyUsers.Where(a =>
        //                        (a.ZoneIds != null && office.ZoneId != null && a.ZoneIds.Contains("," + office.Zone.ShortCode + ",")) ||
        //                        (a.OfficeIds != null && a.OfficeIds.Contains("," + office.ShortCode + ","))).Select(a => a.Month).Distinct().OrderBy(a => a).ToList();
        //                    }
        //            }
        //        }
        //        UserId = CheckUserId(listUserSelect, UserId);
        //        if (UserId != null)
        //        {
        //            var user = _unitOfWork.UserRepository.GetById(UserId);
        //            listUser = listUser.Where(a => a.Id == UserId);
        //            if (User.TypeUser != TypeUser.HO)

        //                if ((User.ZoneIds != null && ((user.ZoneId != null && User.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
        //                (User.OfficeIds != null && user.OfficeId != null && User.OfficeIds.Contains("," + user.OfficeId + ",")))
        //                {
        //                    listMonth = listMonthFull;
        //                }
        //                else
        //                {
        //                    var listHU = historyUsers.ToList();
        //                    listMonth = listHU.Where(hu => (hu.ZoneIds != null && ((user.ZoneId != null && hu.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
        //                    (hu.OfficeIds != null && user.OfficeId != null && hu.OfficeIds.Contains("," + user.OfficeId + ","))).Select(hu => hu.Month).Distinct().OrderBy(a => a).ToList();
        //                }
        //        }
        //        var userItems = new List<BCTHNVViewModel.UserItem>();
        //        var listReportCategoryId = new List<int> { 87, 88, 95, 96, 99, 100, 103 };
        //        var allUserIds = listUser.Select(u => u.Id).ToList();

        //        var listReportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type2 && a.HistoryUserId.HasValue &&
        //        allUserIds.Contains(a.HistoryUser.UserId) && listReportCategoryId.Contains(a.ReportCategoryId)).AsNoTracking().ToList();
        //        var reportDict = listReportData
        //                .GroupBy(x => (x.Month, x.HistoryUserId, x.ReportCategoryId))
        //                .ToDictionary(
        //                    g => g.Key,
        //                    g => g.Sum(x => x.DataReal ?? 0m)
        //                );

        //        decimal GetData(int month, int historyUserId, int categoryId)
        //        {
        //            return reportDict.TryGetValue(
        //                (month, historyUserId, categoryId),
        //                out var value
        //            ) ? value : 0;
        //        }
        //        var pagedUsers = listUser.OrderBy(u => u.Id).ToPagedList(pageNumber, 10);
        //        var historyUsersAll = _unitOfWork.HistoryUserRepository
        //.GetQuery(a => a.Year == selectedYear && a.Active && allUserIds.Contains(a.UserId))
        //.AsNoTracking()
        //.ToList();

        //        var historyUserDict = historyUsersAll
        //            .GroupBy(x => (x.UserId, x.Month))
        //            .ToDictionary(g => g.Key, g => g.ToList());

        //        foreach (var user in pagedUsers)
        //        {
        //            var listMonthUser = new List<int>();
        //            if (User.TypeUser != TypeUser.HO)

        //                if ((User.ZoneIds != null && ((user.ZoneId != null && User.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
        //                (User.OfficeIds != null && user.OfficeId != null && User.OfficeIds.Contains("," + user.OfficeId + ",")))
        //                {
        //                    listMonthUser = listMonthFull;
        //                }
        //                else
        //                {
        //                    var listHU = historyUsers.ToList();
        //                    listMonthUser = listHU.Where(hu => (hu.ZoneIds != null && ((user.ZoneId != null && hu.ZoneIds.Contains("," + user.Zone.ShortCode + ",")) || (user.OfficeId != null && user.Office.ZoneId != null && User.ZoneIds.Contains("," + user.Office.Zone.ShortCode + ",")))) ||
        //                    (hu.OfficeIds != null && user.OfficeId != null && hu.OfficeIds.Contains("," + user.OfficeId + ","))).Select(hu => hu.Month).Distinct().OrderBy(a => a).ToList();
        //                }
        //            else
        //            {
        //                listMonthUser = listMonthFull;
        //            }
        //            var userItem = new BCTHNVViewModel.UserItem()
        //            {
        //                User = user,
        //                ListHTDSs = new List<string>(),
        //                ListHTCGs = new List<string>(),
        //                ListHTHVs = new List<string>(),
        //                ListTCBQs = new List<string>(),

        //            };
        //            // tạo các biến thực đạt/ chỉ tiêu nhân sự

        //            decimal chitieuDSQuy1 = 0, thucDatDSQuy1 = 0, chitieuCGQuy1 = 0, thucDatCGQuy1 = 0, chitieuHVQuy1 = 0, thucDatHVQuy1 = 0, tongSoThangChotQuy1 = 0;
        //            decimal chitieuDSQuy2 = 0, thucDatDSQuy2 = 0, chitieuCGQuy2 = 0, thucDatCGQuy2 = 0, chitieuHVQuy2 = 0, thucDatHVQuy2 = 0, tongSoThangChotQuy2 = 0;
        //            decimal chitieuDSQuy3 = 0, thucDatDSQuy3 = 0, chitieuCGQuy3 = 0, thucDatCGQuy3 = 0, chitieuHVQuy3 = 0, thucDatHVQuy3 = 0, tongSoThangChotQuy3 = 0;
        //            decimal chitieuDSQuy4 = 0, thucDatDSQuy4 = 0, chitieuCGQuy4 = 0, thucDatCGQuy4 = 0, chitieuHVQuy4 = 0, thucDatHVQuy4 = 0, tongSoThangChotQuy4 = 0;
        //            decimal chitieuDSNam = 0, thucDatDSNam = 0, chitieuCGNam = 0, thucDatCGNam = 0, chitieuHVNam = 0, thucDatHVNam = 0, tongSoThangChotNam = 0;
        //            foreach (var month in listMonth)
        //            {

        //                decimal chitieuDSThang = 0, thucDatDSThang = 0, chitieuCGThang = 0, thucDatCGThang = 0, chitieuHVThang = 0, thucDatHVThang = 0, tongSoThangChotThang = 0;

        //                if (!listMonthUser.Contains(month))
        //                {
        //                    userItem.ListHTDSs.Add("");
        //                    userItem.ListHTCGs.Add("");
        //                    userItem.ListHTHVs.Add("");
        //                    userItem.ListTCBQs.Add("");
        //                }
        //                else
        //                {
        //                    //var listHistoryUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == selectedYear && a.Active && a.UserId == user.Id).AsNoTracking().ToList();
        //                    //decimal chitieuDS = 0, thucDatDS = 0, chitieuCG = 0, thucDatCG = 0, chitieuHV = 0, thucDatHV = 0, tongSoThangChot = 0;
        //                    if (historyUserDict.TryGetValue((user.Id, month), out var listHistoryUser))
        //                    {

        //                        foreach (var item in listHistoryUser)
        //                        {
        //                            chitieuDSThang += GetData(month, item.Id, 87);
        //                            thucDatDSThang += GetData(month, item.Id, 88);
        //                            chitieuCGThang += GetData(month, item.Id, 99);
        //                            thucDatCGThang += GetData(month, item.Id, 100);
        //                            chitieuHVThang += GetData(month, item.Id, 95);
        //                            thucDatHVThang += GetData(month, item.Id, 96);
        //                            tongSoThangChotThang += GetData(month, item.Id, 103);
        //                        }
        //                    }

        //                    //foreach (var item in listHistoryUser)
        //                    //{
        //                    //    chitieuDSThang += GetData(month, item.Id, 87);
        //                    //    thucDatDSThang += GetData(month, item.Id, 88);
        //                    //    chitieuCGThang += GetData(month, item.Id, 99);
        //                    //    thucDatCGThang += GetData(month, item.Id, 100);
        //                    //    chitieuHVThang += GetData(month, item.Id, 95);
        //                    //    thucDatHVThang += GetData(month, item.Id, 96);
        //                    //    tongSoThangChotThang += GetData(month, item.Id, 103);
        //                    //}
        //                    decimal? htDSThang = null, htCGThang = null, htHVThang = null, thangChotBQThang = null;
        //                    if (chitieuDSThang > 0)
        //                    {
        //                        htDSThang = thucDatDSThang / chitieuDSThang * 100;
        //                    }
        //                    if (chitieuCGThang > 0)
        //                    {
        //                        htCGThang = thucDatCGThang / chitieuCGThang * 100;
        //                    }
        //                    if (chitieuHVThang > 0)
        //                    {
        //                        htHVThang = thucDatHVThang / chitieuHVThang * 100;
        //                    }
        //                    if (thucDatHVThang > 0)
        //                    {
        //                        thangChotBQThang = tongSoThangChotThang / thucDatHVThang;
        //                    }
        //                    userItem.ListHTDSs.Add(htDSThang == null ? "" : htDSThang?.ToString("N2") + "%");
        //                    userItem.ListHTCGs.Add(htCGThang == null ? "" : htCGThang?.ToString("N2") + "%");
        //                    userItem.ListHTHVs.Add(htHVThang == null ? "" : htHVThang?.ToString("N2") + "%");
        //                    userItem.ListTCBQs.Add(thangChotBQThang == null ? "" : thangChotBQThang?.ToString("N0"));

        //                    chitieuDSNam += chitieuDSThang;
        //                    thucDatDSNam += thucDatDSThang;
        //                    chitieuCGNam += chitieuCGThang;
        //                    thucDatCGNam += thucDatCGThang;
        //                    chitieuHVNam += chitieuHVThang;
        //                    thucDatHVNam += thucDatHVThang;
        //                    tongSoThangChotNam += tongSoThangChotThang;
        //                    if (month <= 3)
        //                    {
        //                        chitieuDSQuy1 += chitieuDSThang;
        //                        thucDatDSQuy1 += thucDatDSThang;
        //                        chitieuCGQuy1 += chitieuCGThang;
        //                        thucDatCGQuy1 += thucDatCGThang;
        //                        chitieuHVQuy1 += chitieuHVThang;
        //                        thucDatHVQuy1 += thucDatHVThang;
        //                        tongSoThangChotQuy1 += tongSoThangChotThang;
        //                    }
        //                    else if (month <= 6)
        //                    {
        //                        chitieuDSQuy2 += chitieuDSThang;
        //                        thucDatDSQuy2 += thucDatDSThang;
        //                        chitieuCGQuy2 += chitieuCGThang;
        //                        thucDatCGQuy2 += thucDatCGThang;
        //                        chitieuHVQuy2 += chitieuHVThang;
        //                        thucDatHVQuy2 += thucDatHVThang;
        //                        tongSoThangChotQuy2 += tongSoThangChotThang;
        //                    }
        //                    else if (month <= 9)
        //                    {
        //                        chitieuDSQuy3 += chitieuDSThang;
        //                        thucDatDSQuy3 += thucDatDSThang;
        //                        chitieuCGQuy3 += chitieuCGThang;
        //                        thucDatCGQuy3 += thucDatCGThang;
        //                        chitieuHVQuy3 += chitieuHVThang;
        //                        thucDatHVQuy3 += thucDatHVThang;
        //                        tongSoThangChotQuy3 += tongSoThangChotThang;
        //                    }
        //                    else
        //                    {
        //                        chitieuDSQuy4 += chitieuDSThang;
        //                        thucDatDSQuy4 += thucDatDSThang;
        //                        chitieuCGQuy4 += chitieuCGThang;
        //                        thucDatCGQuy4 += thucDatCGThang;
        //                        chitieuHVQuy4 += chitieuHVThang;
        //                        thucDatHVQuy4 += thucDatHVThang;
        //                        tongSoThangChotQuy4 += tongSoThangChotThang;
        //                    }
        //                }
        //            }
        //            if (listMonth.Contains(1) || listMonth.Contains(2) || listMonth.Contains(3))
        //            {
        //                decimal? htDSQuy1 = null, htCGQuy1 = null, htHVQuy1 = null, thangChotBQQuy1 = null;

        //                if (chitieuDSQuy1 > 0)
        //                {
        //                    htDSQuy1 = thucDatDSQuy1 / chitieuDSQuy1 * 100;
        //                }
        //                if (chitieuCGQuy1 > 0)
        //                {
        //                    htCGQuy1 = thucDatCGQuy1 / chitieuCGQuy1 * 100;
        //                }
        //                if (chitieuHVQuy1 > 0)
        //                {
        //                    htHVQuy1 = thucDatHVQuy1 / chitieuHVQuy1 * 100;
        //                }
        //                if (thucDatHVQuy1 > 0)
        //                {
        //                    thangChotBQQuy1 = tongSoThangChotQuy1 / thucDatHVQuy1;
        //                }
        //                userItem.ListHTDSs.Add(htDSQuy1 == null ? "" : htDSQuy1?.ToString("N2") + "%");
        //                userItem.ListHTCGs.Add(htCGQuy1 == null ? "" : htCGQuy1?.ToString("N2") + "%");
        //                userItem.ListHTHVs.Add(htHVQuy1 == null ? "" : htHVQuy1?.ToString("N2") + "%");
        //                userItem.ListTCBQs.Add(thangChotBQQuy1 == null ? "" : thangChotBQQuy1?.ToString("N0"));
        //            }
        //            if (listMonth.Contains(4) || listMonth.Contains(5) || listMonth.Contains(6))
        //            {
        //                decimal? htDSQuy2 = null, htCGQuy2 = null, htHVQuy2 = null, thangChotBQQuy2 = null;

        //                if (chitieuDSQuy2 > 0)
        //                {
        //                    htDSQuy2 = thucDatDSQuy2 / chitieuDSQuy2 * 100;
        //                }
        //                if (chitieuCGQuy2 > 0)
        //                {
        //                    htCGQuy2 = thucDatCGQuy2 / chitieuCGQuy2 * 100;
        //                }
        //                if (chitieuHVQuy2 > 0)
        //                {
        //                    htHVQuy2 = thucDatHVQuy2 / chitieuHVQuy2 * 100;
        //                }
        //                if (thucDatHVQuy2 > 0)
        //                {
        //                    thangChotBQQuy2 = tongSoThangChotQuy2 / thucDatHVQuy2;
        //                }
        //                userItem.ListHTDSs.Add(htDSQuy2 == null ? "" : htDSQuy2?.ToString("N2") + "%");
        //                userItem.ListHTCGs.Add(htCGQuy2 == null ? "" : htCGQuy2?.ToString("N2") + "%");
        //                userItem.ListHTHVs.Add(htHVQuy2 == null ? "" : htHVQuy2?.ToString("N2") + "%");
        //                userItem.ListTCBQs.Add(thangChotBQQuy2 == null ? "" : thangChotBQQuy2?.ToString("N0"));
        //            }
        //            if (listMonth.Contains(7) || listMonth.Contains(8) || listMonth.Contains(9))
        //            {

        //                decimal? htDSQuy3 = null, htCGQuy3 = null, htHVQuy3 = null, thangChotBQQuy3 = null;

        //                if (chitieuDSQuy3 > 0)
        //                {
        //                    htDSQuy3 = thucDatDSQuy3 / chitieuDSQuy3 * 100;
        //                }
        //                if (chitieuCGQuy3 > 0)
        //                {
        //                    htCGQuy3 = thucDatCGQuy3 / chitieuCGQuy3 * 100;
        //                }
        //                if (chitieuHVQuy3 > 0)
        //                {
        //                    htHVQuy3 = thucDatHVQuy3 / chitieuHVQuy3 * 100;
        //                }
        //                if (thucDatHVQuy3 > 0)
        //                {
        //                    thangChotBQQuy3 = tongSoThangChotQuy3 / thucDatHVQuy3;
        //                }
        //                userItem.ListHTDSs.Add(htDSQuy3 == null ? "" : htDSQuy3?.ToString("N2") + "%");
        //                userItem.ListHTCGs.Add(htCGQuy3 == null ? "" : htCGQuy3?.ToString("N2") + "%");
        //                userItem.ListHTHVs.Add(htHVQuy3 == null ? "" : htHVQuy3?.ToString("N2") + "%");
        //                userItem.ListTCBQs.Add(thangChotBQQuy3 == null ? "" : thangChotBQQuy3?.ToString("N0"));
        //            }
        //            if (listMonth.Contains(10) || listMonth.Contains(11) || listMonth.Contains(12))
        //            {
        //                decimal? htDSQuy4 = null, htCGQuy4 = null, htHVQuy4 = null, thangChotBQQuy4 = null;

        //                if (chitieuDSQuy4 > 0)
        //                {
        //                    htDSQuy4 = thucDatDSQuy4 / chitieuDSQuy4 * 100;
        //                }
        //                if (chitieuCGQuy4 > 0)
        //                {
        //                    htCGQuy4 = thucDatCGQuy4 / chitieuCGQuy4 * 100;
        //                }
        //                if (chitieuHVQuy4 > 0)
        //                {
        //                    htHVQuy4 = thucDatHVQuy4 / chitieuHVQuy4 * 100;
        //                }
        //                if (thucDatHVQuy4 > 0)
        //                {
        //                    thangChotBQQuy4 = tongSoThangChotQuy4 / thucDatHVQuy4;
        //                }
        //                userItem.ListHTDSs.Add(htDSQuy4 == null ? "" : htDSQuy4?.ToString("N2") + "%");
        //                userItem.ListHTCGs.Add(htCGQuy4 == null ? "" : htCGQuy4?.ToString("N2") + "%");
        //                userItem.ListHTHVs.Add(htHVQuy4 == null ? "" : htHVQuy4?.ToString("N2") + "%");
        //                userItem.ListTCBQs.Add(thangChotBQQuy4 == null ? "" : thangChotBQQuy4?.ToString("N0"));
        //            }


        //            decimal? htDSNam = null, htCGNam = null, htHVNam = null, thangChotBQNam = null;

        //            if (chitieuDSNam > 0)
        //            {
        //                htDSNam = thucDatDSNam / chitieuDSNam * 100;
        //            }
        //            if (chitieuCGNam > 0)
        //            {
        //                htCGNam = thucDatCGNam / chitieuCGNam * 100;
        //            }
        //            if (chitieuHVNam > 0)
        //            {
        //                htHVNam = thucDatHVNam / chitieuHVNam * 100;
        //            }
        //            if (thucDatHVNam > 0)
        //            {
        //                thangChotBQNam = tongSoThangChotNam / thucDatHVNam;
        //            }
        //            userItem.ListHTDSs.Add(htDSNam == null ? "" : htDSNam?.ToString("N2") + "%");
        //            userItem.ListHTCGs.Add(htCGNam == null ? "" : htCGNam?.ToString("N2") + "%");
        //            userItem.ListHTHVs.Add(htHVNam == null ? "" : htHVNam?.ToString("N2") + "%");
        //            userItem.ListTCBQs.Add(thangChotBQNam == null ? "" : thangChotBQNam?.ToString("N0"));
        //            userItems.Add(userItem);
        //        }
        //        var pagedUserItems = new StaticPagedList<BCTHNVViewModel.UserItem>(userItems, pagedUsers.PageNumber, pagedUsers.PageSize, pagedUsers.TotalItemCount);
        //        var model = new BCTHNVViewModel()
        //        {
        //            Zones = zones.ToList(),
        //            Offices = offices.ToList(),
        //            Users = listUserSelect.ToList(),
        //            UserItems = pagedUserItems,
        //            Year = selectedYear,
        //            UserId = UserId,
        //            OfficeId = OfficeId,
        //            ZoneId = ZoneId,
        //            User = User,
        //            UserType = UserType,
        //            ListMonth = listMonth,
        //        };
        //        return View(model);
        //    }

        public ActionResult ReportTHCN(int? ZoneId, int? OfficeId, int? Year)
        {
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.BM)
                return HttpNotFound();
            //var allHistoryZoneIds = new HashSet<string>(
            //    historyUsers.Where(hu => !string.IsNullOrEmpty(hu.ZoneIds)).SelectMany(hu => hu.ZoneIds.Split(',', (char)StringSplitOptions.RemoveEmptyEntries)));
            var selectedYear = Year ?? DateTime.Now.Year;
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Year == selectedYear).AsNoTracking();

            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Active && h.Year == selectedYear).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId,
                h.Month,
                Name = h.Office.Name
            });
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort));
            var zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            //var users = _unitOfWork.UserRepository.Get(a => a.Active && listHistoryUser.Contains(a.Id));
            var model = new BCTHCNViewModel
            {
                Year = selectedYear,
                Offices = offices,
                User = User,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
            };
            #region Tạo biến

            List<decimal> SoSale = new List<decimal>(), DinhBien = new List<decimal>(),
                ChiTieuDS = new List<decimal>(), ThucDatDS = new List<decimal>(), HTDS = new List<decimal>(), TiTrongSale = new List<decimal>(), TiTrongDaoTao = new List<decimal>(), TiTrongKeToan = new List<decimal>(),
                UuDaiBinhQuan = new List<decimal>(), ChiTieuHocVien = new List<decimal>(), TongSoHocVien = new List<decimal>(), HVGhiDanhLai = new List<decimal>(), HVGhiDanhMoi = new List<decimal>(),
                HTCuocGoi = new List<decimal>(), HTHocVien = new List<decimal>(), ThangChotBinhQuan = new List<decimal>(),
                SaleOver100 = new List<decimal>(), Sale30To50 = new List<decimal>(), Sale20To30 = new List<decimal>(), SaleUnder20 = new List<decimal>(),
                TiLeDoanhThuNen = new List<decimal>(), TiLeDoanhThuHocBong = new List<decimal>(), TiLeDoanhThuVang = new List<decimal>(), TiLeDoanhThuSuKien = new List<decimal>();


            // Tạo các biến cho quý

            decimal soSaleQuy1 = 0, dinhBienQuy1 = 0, chiTieuDSQuy1 = 0, thucDatDSQuy1 = 0, DSSaleQuy1 = 0, DSDaoTaoQuy1 = 0, DSKeToanQuy1 = 0, tongSoHocVienQuy1 = 0, tongSoThangDKQuy1 = 0, hVGhiDanhLaiQuy1 = 0, hVGhiDanhMoiQuy1 = 0,
            chiTieuHocVienQuy1 = 0, chiTieuCuocGoiQuy1 = 0, hoanThanhCuocGoiQuy1 = 0, saleOver100Quy1 = 0, sale30To50Quy1 = 0, sale20To30Quy1 = 0, saleUnder20Quy1 = 0, doanhThuNenQuy1 = 0, doanhThuHocBongQuy1 = 0, doanhThuVangQuy1 = 0, doanhThuSuKienQuy1 = 0,

            soSaleQuy2 = 0, dinhBienQuy2 = 0, chiTieuDSQuy2 = 0, thucDatDSQuy2 = 0, DSSaleQuy2 = 0, DSDaoTaoQuy2 = 0, DSKeToanQuy2 = 0, tongSoHocVienQuy2 = 0, tongSoThangDKQuy2 = 0, hVGhiDanhLaiQuy2 = 0, hVGhiDanhMoiQuy2 = 0,
            chiTieuHocVienQuy2 = 0, chiTieuCuocGoiQuy2 = 0, hoanThanhCuocGoiQuy2 = 0, saleOver100Quy2 = 0, sale30To50Quy2 = 0, sale20To30Quy2 = 0, saleUnder20Quy2 = 0, doanhThuNenQuy2 = 0, doanhThuHocBongQuy2 = 0, doanhThuVangQuy2 = 0, doanhThuSuKienQuy2 = 0,

            soSaleQuy3 = 0, dinhBienQuy3 = 0, chiTieuDSQuy3 = 0, thucDatDSQuy3 = 0, DSSaleQuy3 = 0, DSDaoTaoQuy3 = 0, DSKeToanQuy3 = 0, tongSoHocVienQuy3 = 0, tongSoThangDKQuy3 = 0, hVGhiDanhLaiQuy3 = 0, hVGhiDanhMoiQuy3 = 0,
            chiTieuHocVienQuy3 = 0, chiTieuCuocGoiQuy3 = 0, hoanThanhCuocGoiQuy3 = 0, saleOver100Quy3 = 0, sale30To50Quy3 = 0, sale20To30Quy3 = 0, saleUnder20Quy3 = 0, doanhThuNenQuy3 = 0, doanhThuHocBongQuy3 = 0, doanhThuVangQuy3 = 0, doanhThuSuKienQuy3 = 0,

            soSaleQuy4 = 0, dinhBienQuy4 = 0, chiTieuDSQuy4 = 0, thucDatDSQuy4 = 0, DSSaleQuy4 = 0, DSDaoTaoQuy4 = 0, DSKeToanQuy4 = 0, tongSoHocVienQuy4 = 0, tongSoThangDKQuy4 = 0, hVGhiDanhLaiQuy4 = 0, hVGhiDanhMoiQuy4 = 0,
            chiTieuHocVienQuy4 = 0, chiTieuCuocGoiQuy4 = 0, hoanThanhCuocGoiQuy4 = 0, saleOver100Quy4 = 0, sale30To50Quy4 = 0, sale20To30Quy4 = 0, saleUnder20Quy4 = 0, doanhThuNenQuy4 = 0, doanhThuHocBongQuy4 = 0, doanhThuVangQuy4 = 0, doanhThuSuKienQuy4 = 0,

            soSaleNam = 0, dinhBienNam = 0, chiTieuDSNam = 0, thucDatDSNam = 0, DSSaleNam = 0, DSDaoTaoNam = 0, DSKeToanNam = 0, tongSoHocVienNam = 0, tongSoThangDKNam = 0, hVGhiDanhLaiNam = 0, hVGhiDanhMoiNam = 0,
            chiTieuHocVienNam = 0, chiTieuCuocGoiNam = 0, hoanThanhCuocGoiNam = 0, saleOver100Nam = 0, sale30To50Nam = 0, sale20To30Nam = 0, saleUnder20Nam = 0, doanhThuNenNam = 0, doanhThuHocBongNam = 0, doanhThuVangNam = 0, doanhThuSuKienNam = 0;

            var listMonth = new List<int>();
            var listReportCategoryId = new List<int> { 22, 23, 34, 35, 40, 43, 119, 31, 30, 58, 60, 26, 27, 64, 78, 80, 82, 84, 53, 54, 55, 56, 62 };
            var allOfficeIds = new List<int>();
            var listReportData = new List<ReportData>();
            #endregion

            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = zones;
                if (model.ZoneId == null && model.OfficeId == null)
                {
                    model.Offices = model.Offices.ToList();
                    listMonth.AddRange(Enumerable.Range(1, 12));
                }

            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));
                }
            }
            else
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));

                            if (model.OfficeId == null)
                            {

                            }
                        }
                    }
                    else if (User.ZoneId != null)
                    {
                        model.Zones = zones.Where(a => a.Id == User.ZoneId || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneId == h.ZoneId || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));

                            if (model.OfficeId == null)
                            {

                            }
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                    }
                    else
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",") || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                    }
                }
            }
            if (model.Zones?.Count() == 1)
            {
                model.ZoneId = model.Zones.First().Id;
            }
            if (model.ZoneId != null)
            {
                var zone = zones.FirstOrDefault(a => a.Id == model.ZoneId);
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId)).ToList();
                if (model.OfficeId != null)
                {
                    var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                    if (!model.Offices.Contains(office))
                    {
                        model.OfficeId = null;
                    }
                }
                if (model.OfficeId == null)
                {
                    if (User.TypeUser == TypeUser.HO)
                    {
                        listMonth.AddRange(Enumerable.Range(1, 12));
                    }
                    else if (User.TypeUser == TypeUser.CV)
                    {
                        if (User.ZoneIds.Contains("," + zone.ShortCode + ","))
                        {
                            listMonth.AddRange(Enumerable.Range(1, 12));
                        }
                        else
                        {
                            foreach (var item in historyUsers.Where(hu => hu.ZoneIds.Contains("," + zone.ShortCode + ",")))
                            {
                                if (!listMonth.Contains(item.Month))
                                    listMonth.Add(item.Month);
                            }
                        }
                    }
                    else if (User.TypeUser == TypeUser.ASM)
                    {
                        if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                        {
                            if (User.ZoneIds.Contains("," + zone.ShortCode + ","))
                            {
                                listMonth.AddRange(Enumerable.Range(1, 12));
                            }
                            else
                            {
                                foreach (var item in historyUsers.Where(hu => hu.ZoneIds.Contains("," + zone.ShortCode + ",")))
                                {
                                    if (!listMonth.Contains(item.Month))
                                        listMonth.Add(item.Month);
                                }
                            }
                        }
                        else
                        {
                            if (User.ZoneId == model.ZoneId)
                            {
                                listMonth.AddRange(Enumerable.Range(1, 12));
                            }
                            else
                            {
                                foreach (var item in historyUsers.Where(hu => hu.ZoneIds.Contains("," + zone.ShortCode + ",")))
                                {
                                    if (!listMonth.Contains(item.Month))
                                        listMonth.Add(item.Month);
                                }
                            }
                        }
                    }
                }
            }

            if (model.Offices.Count() == 1)
            {
                model.OfficeId = model.Offices.First().Id;
            }

            if (model.OfficeId != null)
            {
                // Nếu chọn CN - show các tháng đã từng quản lý / hoặc show all nếu đang quản lý
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                allOfficeIds = new List<int> { office.Id };

                var hOffices = historyOffices.Where(a => a.OfficeId == model.OfficeId);

                if (User.TypeUser == TypeUser.HO)
                {
                    listMonth.AddRange(Enumerable.Range(1, 12));
                }
                else if (User.TypeUser == TypeUser.CV)
                {
                    if (User.ZoneIds.Contains("," + office.Zone.ShortCode + ","))
                    {
                        listMonth.AddRange(Enumerable.Range(1, 12));
                    }
                    else
                    {
                        var listMonthManaged = historyUsers.Where(hu => hOffices.Any(ho => hu.ZoneIds != null && hu.ZoneIds.Contains("," + ho.ZoneShortCode + ","))).Select(hu => hu.Month).ToList();
                        listMonth.AddRange(listMonthManaged);
                    }
                }
                else if (User.TypeUser == TypeUser.ASM)
                {
                    if ((!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2 && User.ZoneIds.Contains("," + office.Zone.ShortCode + ",")) || (User.ZoneId != null && User.ZoneId == office.ZoneId))
                    {
                        listMonth.AddRange(Enumerable.Range(1, 12));
                    }
                    else
                    {
                        var listMonthManaged = historyUsers.Where(hu => hOffices.Any(ho => hu.ZoneIds != null && hu.ZoneIds.Contains("," + ho.ZoneShortCode + ","))).Select(hu => hu.Month).ToList();
                        listMonth.AddRange(listMonthManaged);
                    }
                }
                else if (User.TypeUser == TypeUser.BM)
                {
                    if ((!string.IsNullOrEmpty(User.OfficeIds) && User.OfficeIds.Contains("," + office.Id + ",")) || (model.OfficeId == User.OfficeId))
                    {
                        listMonth.AddRange(Enumerable.Range(1, 12));
                    }
                    else
                    {
                        var listMonthManaged = historyUsers.Where(hu => hOffices.Any(ho => (hu.OfficeIds != null && hu.OfficeIds.Contains("," + ho.OfficeId + ",")) || (hu.OfficeId == ho.OfficeId))).Select(hu => hu.Month).ToList();
                        listMonth.AddRange(listMonthManaged);
                    }
                }
            }
            else
            {
                allOfficeIds = model.Offices.Select(o => o.Id).ToList();
            }

            listReportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type1 && a.OfficeId.HasValue &&
            allOfficeIds.Contains(a.OfficeId.Value) && listReportCategoryId.Contains(a.ReportCategoryId)).AsNoTracking().ToList();
            var reportDict = listReportData
            .GroupBy(x => (x.Month, x.OfficeId, x.ReportCategoryId))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => x.DataReal ?? 0m)
            );

            decimal GetData(int month, int officeId, int categoryId)
            {
                return reportDict.TryGetValue(
                    (month, officeId, categoryId),
                    out var value
                ) ? value : 0;
            }

            listMonth.OrderBy(a => a);
            foreach (var month in listMonth)
            {
                decimal soSale = 0, dinhBien = 0, chiTieuDS = 0, thucDatDS = 0, hTDS = 0, DSSale = 0, DSDaoTao = 0, DSKeToan = 0, tiTrongSale = 0, tiTrongDaoTao = 0, tiTrongKeToan = 0, uuDaiBinhQuan = 0,
                 hVGhiDanhLai = 0, hVGhiDanhMoi = 0, tileHVGDM = 0, tileHVGDL = 0, chiTieuCuocGoi = 0, chiTieuHocVien = 0, tongSoHocVien = 0, hoanThanhCuocGoi = 0, thangChotBinhQuan = 0, tongSoThangDK = 0, hTCuocGoi = 0, hTHocVien = 0,
                 saleOver100 = 0, sale30To50 = 0, sale20To30 = 0, saleUnder20 = 0, doanhThuNen = 0, doanhThuHocBong = 0, doanhThuVang = 0, doanhThuSuKien = 0, tiLeDoanhThuNen = 0, tiLeDoanhThuHocBong = 0, tiLeDoanhThuVang = 0, tiLeDoanhThuSuKien = 0;
                var officeIds = model.Offices.Select(h => h.Id).ToList();
                foreach (var officeId in officeIds)
                {
                    soSale += GetData(month, officeId, 23);
                    dinhBien += GetData(month, officeId, 22);
                    chiTieuDS += GetData(month, officeId, 34);
                    thucDatDS += GetData(month, officeId, 35);
                    DSSale += GetData(month, officeId, 40);
                    DSDaoTao += GetData(month, officeId, 43);
                    DSKeToan += GetData(month, officeId, 119);

                    tongSoHocVien += GetData(month, officeId, 31);
                    chiTieuHocVien += GetData(month, officeId, 30);
                    hVGhiDanhLai += GetData(month, officeId, 58);
                    hVGhiDanhMoi += GetData(month, officeId, 60);

                    chiTieuCuocGoi += GetData(month, officeId, 26);
                    hoanThanhCuocGoi += GetData(month, officeId, 27);
                    tongSoThangDK += GetData(month, officeId, 62);

                    saleOver100 += GetData(month, officeId, 78);
                    sale30To50 += GetData(month, officeId, 80);
                    sale20To30 += GetData(month, officeId, 82);
                    saleUnder20 += GetData(month, officeId, 84);

                    doanhThuNen += GetData(month, officeId, 53);
                    doanhThuHocBong += GetData(month, officeId, 54);
                    doanhThuVang += GetData(month, officeId, 55);
                    doanhThuSuKien += GetData(month, officeId, 56);

                    soSaleNam += GetData(month, officeId, 23);
                    dinhBienNam += GetData(month, officeId, 22);
                    chiTieuDSNam += GetData(month, officeId, 34);
                    thucDatDSNam += GetData(month, officeId, 35);
                    DSSaleNam += GetData(month, officeId, 40);
                    DSDaoTaoNam += GetData(month, officeId, 43);
                    DSKeToanNam += GetData(month, officeId, 119);

                    tongSoHocVienNam += GetData(month, officeId, 31);
                    tongSoThangDKNam += GetData(month, officeId, 62);
                    hVGhiDanhLaiNam += GetData(month, officeId, 58);
                    hVGhiDanhMoiNam += GetData(month, officeId, 60);
                    chiTieuHocVienNam += GetData(month, officeId, 30);

                    chiTieuCuocGoiNam += GetData(month, officeId, 26);
                    hoanThanhCuocGoiNam += GetData(month, officeId, 27);

                    saleOver100Nam += GetData(month, officeId, 78);
                    sale30To50Nam += GetData(month, officeId, 80);
                    sale20To30Nam += GetData(month, officeId, 82);
                    saleUnder20Nam += GetData(month, officeId, 84);

                    doanhThuNenNam += GetData(month, officeId, 53);
                    doanhThuHocBongNam += GetData(month, officeId, 54);
                    doanhThuVangNam += GetData(month, officeId, 55);
                    doanhThuSuKienNam += GetData(month, officeId, 56);
                    if (month == 1 || month == 2 || month == 3)
                    {
                        soSaleQuy1 += GetData(month, officeId, 23);
                        dinhBienQuy1 += GetData(month, officeId, 22);
                        chiTieuDSQuy1 += GetData(month, officeId, 34);
                        thucDatDSQuy1 += GetData(month, officeId, 35);
                        DSSaleQuy1 += GetData(month, officeId, 40);
                        DSDaoTaoQuy1 += GetData(month, officeId, 43);
                        DSKeToanQuy1 += GetData(month, officeId, 119);

                        tongSoHocVienQuy1 += GetData(month, officeId, 31);
                        tongSoThangDKQuy1 += GetData(month, officeId, 62);
                        hVGhiDanhLaiQuy1 += GetData(month, officeId, 58);
                        hVGhiDanhMoiQuy1 += GetData(month, officeId, 60);
                        chiTieuHocVienQuy1 += GetData(month, officeId, 30);

                        chiTieuCuocGoiQuy1 += GetData(month, officeId, 26);
                        hoanThanhCuocGoiQuy1 += GetData(month, officeId, 27);

                        saleOver100Quy1 += GetData(month, officeId, 78);
                        sale30To50Quy1 += GetData(month, officeId, 80);
                        sale20To30Quy1 += GetData(month, officeId, 82);
                        saleUnder20Quy1 += GetData(month, officeId, 84);

                        doanhThuNenQuy1 += GetData(month, officeId, 53);
                        doanhThuHocBongQuy1 += GetData(month, officeId, 54);
                        doanhThuVangQuy1 += GetData(month, officeId, 55);
                        doanhThuSuKienQuy1 += GetData(month, officeId, 56);
                    }
                    if (month == 4 || month == 5 || month == 6)
                    {
                        soSaleQuy2 += GetData(month, officeId, 23);
                        dinhBienQuy2 += GetData(month, officeId, 22);
                        chiTieuDSQuy2 += GetData(month, officeId, 34);
                        thucDatDSQuy2 += GetData(month, officeId, 35);
                        DSSaleQuy2 += GetData(month, officeId, 40);
                        DSDaoTaoQuy2 += GetData(month, officeId, 43);
                        DSKeToanQuy2 += GetData(month, officeId, 119);

                        tongSoHocVienQuy2 += GetData(month, officeId, 31);
                        tongSoThangDKQuy2 += GetData(month, officeId, 62);
                        hVGhiDanhLaiQuy2 += GetData(month, officeId, 58);
                        hVGhiDanhMoiQuy2 += GetData(month, officeId, 60);
                        chiTieuHocVienQuy2 += GetData(month, officeId, 30);

                        chiTieuCuocGoiQuy2 += GetData(month, officeId, 26);
                        hoanThanhCuocGoiQuy2 += GetData(month, officeId, 27);

                        saleOver100Quy2 += GetData(month, officeId, 78);
                        sale30To50Quy2 += GetData(month, officeId, 80);
                        sale20To30Quy2 += GetData(month, officeId, 82);
                        saleUnder20Quy2 += GetData(month, officeId, 84);

                        doanhThuNenQuy2 += GetData(month, officeId, 53);
                        doanhThuHocBongQuy2 += GetData(month, officeId, 54);
                        doanhThuVangQuy2 += GetData(month, officeId, 55);
                        doanhThuSuKienQuy2 += GetData(month, officeId, 56);
                    }
                    if (month == 7 || month == 8 || month == 9)
                    {
                        soSaleQuy3 += GetData(month, officeId, 23);
                        dinhBienQuy3 += GetData(month, officeId, 22);
                        chiTieuDSQuy3 += GetData(month, officeId, 34);
                        thucDatDSQuy3 += GetData(month, officeId, 35);
                        DSSaleQuy3 += GetData(month, officeId, 40);
                        DSDaoTaoQuy3 += GetData(month, officeId, 43);
                        DSKeToanQuy3 += GetData(month, officeId, 119);

                        tongSoHocVienQuy3 += GetData(month, officeId, 31);
                        tongSoThangDKQuy3 += GetData(month, officeId, 62);
                        hVGhiDanhLaiQuy3 += GetData(month, officeId, 58);
                        hVGhiDanhMoiQuy3 += GetData(month, officeId, 60);
                        chiTieuHocVienQuy3 += GetData(month, officeId, 30);

                        chiTieuCuocGoiQuy3 += GetData(month, officeId, 26);
                        hoanThanhCuocGoiQuy3 += GetData(month, officeId, 27);

                        saleOver100Quy3 += GetData(month, officeId, 78);
                        sale30To50Quy3 += GetData(month, officeId, 80);
                        sale20To30Quy3 += GetData(month, officeId, 82);
                        saleUnder20Quy3 += GetData(month, officeId, 84);

                        doanhThuNenQuy3 += GetData(month, officeId, 53);
                        doanhThuHocBongQuy3 += GetData(month, officeId, 54);
                        doanhThuVangQuy3 += GetData(month, officeId, 55);
                        doanhThuSuKienQuy3 += GetData(month, officeId, 56);
                    }
                    if (month == 10 || month == 11 || month == 12)
                    {
                        soSaleQuy4 += GetData(month, officeId, 23);
                        dinhBienQuy4 += GetData(month, officeId, 22);
                        chiTieuDSQuy4 += GetData(month, officeId, 34);
                        thucDatDSQuy4 += GetData(month, officeId, 35);
                        DSSaleQuy4 += GetData(month, officeId, 40);
                        DSDaoTaoQuy4 += GetData(month, officeId, 43);
                        DSKeToanQuy4 += GetData(month, officeId, 119);

                        tongSoHocVienQuy4 += GetData(month, officeId, 31);
                        tongSoThangDKQuy4 += GetData(month, officeId, 62);
                        hVGhiDanhLaiQuy4 += GetData(month, officeId, 58);
                        hVGhiDanhMoiQuy4 += GetData(month, officeId, 60);
                        chiTieuHocVienQuy4 += GetData(month, officeId, 30);

                        chiTieuCuocGoiQuy4 += GetData(month, officeId, 26);
                        hoanThanhCuocGoiQuy4 += GetData(month, officeId, 27);

                        saleOver100Quy4 += GetData(month, officeId, 78);
                        sale30To50Quy4 += GetData(month, officeId, 80);
                        sale20To30Quy4 += GetData(month, officeId, 82);
                        saleUnder20Quy4 += GetData(month, officeId, 84);

                        doanhThuNenQuy4 += GetData(month, officeId, 53);
                        doanhThuHocBongQuy4 += GetData(month, officeId, 54);
                        doanhThuVangQuy4 += GetData(month, officeId, 55);
                        doanhThuSuKienQuy4 += GetData(month, officeId, 56);
                    }
                }

                // Tính các % hoàn thành: ht dthu, tỉ trọng sale, đào tạo, kế toán; % hv gd mới, gd lại; ht cuộc gọi, ht học viên; % dthu nền,vàng,...

                //ht dthu
                if (chiTieuDS > 0)
                    hTDS = (thucDatDS / chiTieuDS) * 100;

                // tỉ trọng sale, đào tạo, kế toán
                // % dthu nền,vàng
                if (thucDatDS > 0)
                {
                    tiTrongSale = (DSSale / thucDatDS) * 100;
                    tiTrongDaoTao = (DSDaoTao / thucDatDS) * 100;
                    tiTrongKeToan = (DSKeToan / thucDatDS) * 100;
                    tiLeDoanhThuHocBong = (doanhThuHocBong / thucDatDS) * 100;
                    tiLeDoanhThuNen = (doanhThuNen / thucDatDS) * 100;
                    tiLeDoanhThuSuKien = (doanhThuSuKien / thucDatDS) * 100;
                    tiLeDoanhThuVang = (doanhThuVang / thucDatDS) * 100;
                }

                //% hv gd mới, gd lại
                if (tongSoHocVien > 0)
                {
                    tileHVGDM = (hVGhiDanhMoi / tongSoHocVien) * 100;
                    tileHVGDL = (hVGhiDanhLai / tongSoHocVien) * 100;
                    thangChotBinhQuan = tongSoThangDK / tongSoHocVien;
                }

                // ht cuộc gọi
                if (chiTieuCuocGoi > 0)
                    hTCuocGoi = (hoanThanhCuocGoi / chiTieuCuocGoi) * 100;

                // ht học viên
                if (chiTieuHocVien > 0)
                    hTHocVien = (tongSoHocVien / chiTieuHocVien) * 100;

                //add item từng list
                SoSale.Add(soSale);
                DinhBien.Add(dinhBien);
                ChiTieuDS.Add(chiTieuDS);
                ThucDatDS.Add(thucDatDS);
                HTDS.Add(hTDS);
                TiTrongSale.Add(tiTrongSale);
                TiTrongDaoTao.Add(tiTrongDaoTao);
                TiTrongKeToan.Add(tiTrongKeToan);
                ChiTieuHocVien.Add(chiTieuHocVien);
                TongSoHocVien.Add(tongSoHocVien);
                HVGhiDanhLai.Add(tileHVGDL);
                HVGhiDanhMoi.Add(tileHVGDM);
                HTCuocGoi.Add(hTCuocGoi);
                HTHocVien.Add(hTHocVien);
                ThangChotBinhQuan.Add(thangChotBinhQuan);
                SaleOver100.Add(saleOver100);
                Sale30To50.Add(sale30To50);
                Sale20To30.Add(sale20To30);
                SaleUnder20.Add(saleUnder20);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNen);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBong);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKien);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVang);
            }

            // Tinh toán các quý, năm;

            // Quý:
            var countMonthQuy1 = listMonth.Count(a => a == 1 || a == 2 || a == 3);
            if (countMonthQuy1 > 0)
            {
                var averageSoSaleQuy1 = soSaleQuy1 / countMonthQuy1;
                SoSale.Add(averageSoSaleQuy1);
                var averageDinhBienQuy1 = dinhBienQuy1 / countMonthQuy1;
                DinhBien.Add(averageDinhBienQuy1);
                ChiTieuDS.Add(chiTieuDSQuy1);
                ThucDatDS.Add(thucDatDSQuy1);
                decimal htDSQuy1 = 0;
                decimal tiTrongSaleQuy1 = 0;
                decimal tiTrongDaoTaoQuy1 = 0;
                decimal tiTrongKeToanQuy1 = 0;
                decimal tiLeDoanhThuHocBongQuy1 = 0;
                decimal tiLeDoanhThuVangQuy1 = 0;
                decimal tiLeDoanhThuSuKienQuy1 = 0;
                decimal tiLeDoanhThuNenQuy1 = 0;
                if (chiTieuDSQuy1 > 0)
                {
                    htDSQuy1 = thucDatDSQuy1 / chiTieuDSQuy1 * 100;
                }
                HTDS.Add(htDSQuy1);
                if (thucDatDSQuy1 > 0)
                {
                    tiTrongSaleQuy1 = DSSaleQuy1 / thucDatDSQuy1 * 100;
                    tiTrongDaoTaoQuy1 = DSDaoTaoQuy1 / thucDatDSQuy1 * 100;
                    tiTrongKeToanQuy1 = DSKeToanQuy1 / thucDatDSQuy1 * 100;
                    tiLeDoanhThuHocBongQuy1 = (doanhThuHocBongQuy1 / thucDatDSQuy1) * 100;
                    tiLeDoanhThuVangQuy1 = (doanhThuVangQuy1 / thucDatDSQuy1) * 100;
                    tiLeDoanhThuSuKienQuy1 = (doanhThuSuKienQuy1 / thucDatDSQuy1) * 100;
                    tiLeDoanhThuNenQuy1 = (doanhThuNenQuy1 / thucDatDSQuy1) * 100;
                }
                TiTrongSale.Add(tiTrongSaleQuy1);
                TiTrongDaoTao.Add(tiTrongDaoTaoQuy1);
                TiTrongKeToan.Add(tiTrongKeToanQuy1);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNenQuy1);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVangQuy1);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKienQuy1);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBongQuy1);

                //% hv gd mới, gd lại
                decimal tileHVGDLQuy1 = 0;
                decimal tileHVGDMQuy1 = 0;
                decimal thangChotBQQuy1 = 0;
                if (tongSoHocVienQuy1 > 0)
                {
                    tileHVGDMQuy1 = (hVGhiDanhMoiQuy1 / tongSoHocVienQuy1) * 100;
                    tileHVGDLQuy1 = (hVGhiDanhLaiQuy1 / tongSoHocVienQuy1) * 100;
                    thangChotBQQuy1 = tongSoThangDKQuy1 / tongSoHocVienQuy1;
                }
                ThangChotBinhQuan.Add(thangChotBQQuy1);
                HVGhiDanhLai.Add(tileHVGDLQuy1);
                HVGhiDanhMoi.Add(tileHVGDMQuy1);
                TongSoHocVien.Add(tongSoHocVienQuy1);

                // ht cuộc gọi
                decimal htCGQuy1 = 0;
                if (chiTieuCuocGoiQuy1 > 0)
                    htCGQuy1 = (hoanThanhCuocGoiQuy1 / chiTieuCuocGoiQuy1) * 100;
                HTCuocGoi.Add(htCGQuy1);

                // ht học viên

                decimal htHVQuy1 = 0;
                if (chiTieuHocVienQuy1 > 0)
                    htHVQuy1 = (tongSoHocVienQuy1 / chiTieuHocVienQuy1) * 100;
                HTHocVien.Add(htHVQuy1);
                // Năng lực Sale
                SaleOver100.Add(saleOver100Quy1);
                Sale30To50.Add(sale30To50Quy1);
                Sale20To30.Add(sale20To30Quy1);
                SaleUnder20.Add(saleUnder20Quy1);
            }

            var countMonthQuy2 = listMonth.Count(a => a == 4 || a == 5 || a == 6);
            if (countMonthQuy2 > 0)
            {
                var averageSoSaleQuy2 = soSaleQuy2 / countMonthQuy2;
                SoSale.Add(averageSoSaleQuy2);
                var averageDinhBienQuy2 = dinhBienQuy2 / countMonthQuy2;
                DinhBien.Add(averageDinhBienQuy2);
                ChiTieuDS.Add(chiTieuDSQuy2);
                ThucDatDS.Add(thucDatDSQuy2);
                decimal htDSQuy2 = 0;
                decimal tiTrongSaleQuy2 = 0;
                decimal tiTrongDaoTaoQuy2 = 0;
                decimal tiTrongKeToanQuy2 = 0;
                decimal tiLeDoanhThuHocBongQuy2 = 0;
                decimal tiLeDoanhThuVangQuy2 = 0;
                decimal tiLeDoanhThuSuKienQuy2 = 0;
                decimal tiLeDoanhThuNenQuy2 = 0;
                if (chiTieuDSQuy2 > 0)
                {
                    htDSQuy2 = thucDatDSQuy2 / chiTieuDSQuy2 * 100;
                }
                HTDS.Add(htDSQuy2);
                if (thucDatDSQuy2 > 0)
                {
                    tiTrongSaleQuy2 = DSSaleQuy2 / thucDatDSQuy2 * 100;
                    tiTrongDaoTaoQuy2 = DSDaoTaoQuy2 / thucDatDSQuy2 * 100;
                    tiTrongKeToanQuy2 = DSKeToanQuy2 / thucDatDSQuy2 * 100;
                    tiLeDoanhThuHocBongQuy2 = (doanhThuHocBongQuy2 / thucDatDSQuy2) * 100;
                    tiLeDoanhThuVangQuy2 = (doanhThuVangQuy2 / thucDatDSQuy2) * 100;
                    tiLeDoanhThuSuKienQuy2 = (doanhThuSuKienQuy2 / thucDatDSQuy2) * 100;
                    tiLeDoanhThuNenQuy2 = (doanhThuNenQuy2 / thucDatDSQuy2) * 100;
                }
                TiTrongSale.Add(tiTrongSaleQuy2);
                TiTrongDaoTao.Add(tiTrongDaoTaoQuy2);
                TiTrongKeToan.Add(tiTrongKeToanQuy2);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNenQuy2);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVangQuy2);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKienQuy2);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBongQuy2);

                //% hv gd mới, gd lại
                decimal tileHVGDLQuy2 = 0;
                decimal tileHVGDMQuy2 = 0;
                decimal thangChotBQQuy2 = 0;
                if (tongSoHocVienQuy2 > 0)
                {
                    tileHVGDMQuy2 = (hVGhiDanhMoiQuy2 / tongSoHocVienQuy2) * 100;
                    tileHVGDLQuy2 = (hVGhiDanhLaiQuy2 / tongSoHocVienQuy2) * 100;
                    thangChotBQQuy2 = tongSoThangDKQuy2 / tongSoHocVienQuy2;
                }
                ThangChotBinhQuan.Add(thangChotBQQuy2);
                HVGhiDanhLai.Add(tileHVGDLQuy2);
                HVGhiDanhMoi.Add(tileHVGDMQuy2);
                TongSoHocVien.Add(tongSoHocVienQuy2);

                // ht cuộc gọi
                decimal htCGQuy2 = 0;
                if (chiTieuCuocGoiQuy2 > 0)
                    htCGQuy2 = (hoanThanhCuocGoiQuy2 / chiTieuCuocGoiQuy2) * 100;
                HTCuocGoi.Add(htCGQuy2);

                // ht học viên

                decimal htHVQuy2 = 0;
                if (chiTieuHocVienQuy2 > 0)
                    htHVQuy2 = (tongSoHocVienQuy2 / chiTieuHocVienQuy2) * 100;
                HTHocVien.Add(htHVQuy2);
                // Năng lực Sale
                SaleOver100.Add(saleOver100Quy2);
                Sale30To50.Add(sale30To50Quy2);
                Sale20To30.Add(sale20To30Quy2);
                SaleUnder20.Add(saleUnder20Quy2);
            }

            var countMonthQuy3 = listMonth.Count(a => a == 7 || a == 8 || a == 9);
            if (countMonthQuy3 > 0)
            {
                var averageSoSaleQuy3 = soSaleQuy3 / countMonthQuy3;
                SoSale.Add(averageSoSaleQuy3);
                var averageDinhBienQuy3 = dinhBienQuy3 / countMonthQuy3;
                DinhBien.Add(averageDinhBienQuy3);
                ChiTieuDS.Add(chiTieuDSQuy3);
                ThucDatDS.Add(thucDatDSQuy3);
                decimal htDSQuy3 = 0;
                decimal tiTrongSaleQuy3 = 0;
                decimal tiTrongDaoTaoQuy3 = 0;
                decimal tiTrongKeToanQuy3 = 0;
                decimal tiLeDoanhThuHocBongQuy3 = 0;
                decimal tiLeDoanhThuVangQuy3 = 0;
                decimal tiLeDoanhThuSuKienQuy3 = 0;
                decimal tiLeDoanhThuNenQuy3 = 0;
                if (chiTieuDSQuy3 > 0)
                {
                    htDSQuy3 = thucDatDSQuy3 / chiTieuDSQuy3 * 100;
                }
                HTDS.Add(htDSQuy3);
                if (thucDatDSQuy3 > 0)
                {
                    tiTrongSaleQuy3 = DSSaleQuy3 / thucDatDSQuy3 * 100;
                    tiTrongDaoTaoQuy3 = DSDaoTaoQuy3 / thucDatDSQuy3 * 100;
                    tiTrongKeToanQuy3 = DSKeToanQuy3 / thucDatDSQuy3 * 100;
                    tiLeDoanhThuHocBongQuy3 = (doanhThuHocBongQuy3 / thucDatDSQuy3) * 100;
                    tiLeDoanhThuVangQuy3 = (doanhThuVangQuy3 / thucDatDSQuy3) * 100;
                    tiLeDoanhThuSuKienQuy3 = (doanhThuSuKienQuy3 / thucDatDSQuy3) * 100;
                    tiLeDoanhThuNenQuy3 = (doanhThuNenQuy3 / thucDatDSQuy3) * 100;
                }
                TiTrongSale.Add(tiTrongSaleQuy3);
                TiTrongDaoTao.Add(tiTrongDaoTaoQuy3);
                TiTrongKeToan.Add(tiTrongKeToanQuy3);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNenQuy3);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVangQuy3);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKienQuy3);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBongQuy3);

                //% hv gd mới, gd lại
                decimal tileHVGDLQuy3 = 0;
                decimal tileHVGDMQuy3 = 0;
                decimal thangChotBQQuy3 = 0;
                if (tongSoHocVienQuy3 > 0)
                {
                    tileHVGDMQuy3 = (hVGhiDanhMoiQuy3 / tongSoHocVienQuy3) * 100;
                    tileHVGDLQuy3 = (hVGhiDanhLaiQuy3 / tongSoHocVienQuy3) * 100;
                    thangChotBQQuy3 = tongSoThangDKQuy3 / tongSoHocVienQuy3;
                }
                ThangChotBinhQuan.Add(thangChotBQQuy3);
                HVGhiDanhLai.Add(tileHVGDLQuy3);
                HVGhiDanhMoi.Add(tileHVGDMQuy3);
                TongSoHocVien.Add(tongSoHocVienQuy3);

                // ht cuộc gọi
                decimal htCGQuy3 = 0;
                if (chiTieuCuocGoiQuy3 > 0)
                    htCGQuy3 = (hoanThanhCuocGoiQuy3 / chiTieuCuocGoiQuy3) * 100;
                HTCuocGoi.Add(htCGQuy3);

                // ht học viên

                decimal htHVQuy3 = 0;
                if (chiTieuHocVienQuy3 > 0)
                    htHVQuy3 = (tongSoHocVienQuy3 / chiTieuHocVienQuy3) * 100;
                HTHocVien.Add(htHVQuy3);
                // Năng lực Sale
                SaleOver100.Add(saleOver100Quy3);
                Sale30To50.Add(sale30To50Quy3);
                Sale20To30.Add(sale20To30Quy3);
                SaleUnder20.Add(saleUnder20Quy3);
            }

            var countMonthQuy4 = listMonth.Count(a => a == 10 || a == 11 || a == 12);
            if (countMonthQuy4 > 0)
            {
                var averageSoSaleQuy4 = soSaleQuy4 / countMonthQuy4;
                SoSale.Add(averageSoSaleQuy4);
                var averageDinhBienQuy4 = dinhBienQuy4 / countMonthQuy4;
                DinhBien.Add(averageDinhBienQuy4);
                ChiTieuDS.Add(chiTieuDSQuy4);
                ThucDatDS.Add(thucDatDSQuy4);
                decimal htDSQuy4 = 0;
                decimal tiTrongSaleQuy4 = 0;
                decimal tiTrongDaoTaoQuy4 = 0;
                decimal tiTrongKeToanQuy4 = 0;
                decimal tiLeDoanhThuHocBongQuy4 = 0;
                decimal tiLeDoanhThuVangQuy4 = 0;
                decimal tiLeDoanhThuSuKienQuy4 = 0;
                decimal tiLeDoanhThuNenQuy4 = 0;
                if (chiTieuDSQuy4 > 0)
                {
                    htDSQuy4 = thucDatDSQuy4 / chiTieuDSQuy4 * 100;
                }
                HTDS.Add(htDSQuy4);
                if (thucDatDSQuy4 > 0)
                {
                    tiTrongSaleQuy4 = DSSaleQuy4 / thucDatDSQuy4 * 100;
                    tiTrongDaoTaoQuy4 = DSDaoTaoQuy4 / thucDatDSQuy4 * 100;
                    tiTrongKeToanQuy4 = DSKeToanQuy4 / thucDatDSQuy4 * 100;
                    tiLeDoanhThuHocBongQuy4 = (doanhThuHocBongQuy4 / thucDatDSQuy4) * 100;
                    tiLeDoanhThuVangQuy4 = (doanhThuVangQuy4 / thucDatDSQuy4) * 100;
                    tiLeDoanhThuSuKienQuy4 = (doanhThuSuKienQuy4 / thucDatDSQuy4) * 100;
                    tiLeDoanhThuNenQuy4 = (doanhThuNenQuy4 / thucDatDSQuy4) * 100;
                }
                TiTrongSale.Add(tiTrongSaleQuy4);
                TiTrongDaoTao.Add(tiTrongDaoTaoQuy4);
                TiTrongKeToan.Add(tiTrongKeToanQuy4);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNenQuy4);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVangQuy4);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKienQuy4);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBongQuy4);

                //% hv gd mới, gd lại
                decimal tileHVGDLQuy4 = 0;
                decimal tileHVGDMQuy4 = 0;
                decimal thangChotBQQuy4 = 0;
                if (tongSoHocVienQuy4 > 0)
                {
                    tileHVGDMQuy4 = (hVGhiDanhMoiQuy4 / tongSoHocVienQuy4) * 100;
                    tileHVGDLQuy4 = (hVGhiDanhLaiQuy4 / tongSoHocVienQuy4) * 100;
                    thangChotBQQuy4 = tongSoThangDKQuy4 / tongSoHocVienQuy4;
                }
                ThangChotBinhQuan.Add(thangChotBQQuy4);
                HVGhiDanhLai.Add(tileHVGDLQuy4);
                HVGhiDanhMoi.Add(tileHVGDMQuy4);
                TongSoHocVien.Add(tongSoHocVienQuy4);

                // ht cuộc gọi
                decimal htCGQuy4 = 0;
                if (chiTieuCuocGoiQuy4 > 0)
                    htCGQuy4 = (hoanThanhCuocGoiQuy4 / chiTieuCuocGoiQuy4) * 100;
                HTCuocGoi.Add(htCGQuy4);

                // ht học viên

                decimal htHVQuy4 = 0;
                if (chiTieuHocVienQuy4 > 0)
                    htHVQuy4 = (tongSoHocVienQuy4 / chiTieuHocVienQuy4) * 100;
                HTHocVien.Add(htHVQuy4);
                // Năng lực Sale
                SaleOver100.Add(saleOver100Quy4);
                Sale30To50.Add(sale30To50Quy4);
                Sale20To30.Add(sale20To30Quy4);
                SaleUnder20.Add(saleUnder20Quy4);
            }

            // Năm:

            var countMonthNam = listMonth.Count();
            if (countMonthNam > 0)
            {
                var averageSoSaleNam = soSaleNam / countMonthNam;
                SoSale.Add(averageSoSaleNam);
                var averageDinhBienNam = dinhBienNam / countMonthNam;
                DinhBien.Add(averageDinhBienNam);
                ChiTieuDS.Add(chiTieuDSNam);
                ThucDatDS.Add(thucDatDSNam);
                decimal htDSNam = 0;
                decimal tiTrongSaleNam = 0;
                decimal tiTrongDaoTaoNam = 0;
                decimal tiTrongKeToanNam = 0;
                decimal tiLeDoanhThuHocBongNam = 0;
                decimal tiLeDoanhThuVangNam = 0;
                decimal tiLeDoanhThuSuKienNam = 0;
                decimal tiLeDoanhThuNenNam = 0;
                if (chiTieuDSNam > 0)
                {
                    htDSNam = thucDatDSNam / chiTieuDSNam * 100;
                }
                HTDS.Add(htDSNam);
                if (thucDatDSNam > 0)
                {
                    tiTrongSaleNam = DSSaleNam / thucDatDSNam * 100;
                    tiTrongDaoTaoNam = DSDaoTaoNam / thucDatDSNam * 100;
                    tiTrongKeToanNam = DSKeToanNam / thucDatDSNam * 100;
                    tiLeDoanhThuHocBongNam = (doanhThuHocBongNam / thucDatDSNam) * 100;
                    tiLeDoanhThuVangNam = (doanhThuVangNam / thucDatDSNam) * 100;
                    tiLeDoanhThuSuKienNam = (doanhThuSuKienNam / thucDatDSNam) * 100;
                    tiLeDoanhThuNenNam = (doanhThuNenNam / thucDatDSNam) * 100;
                }
                TiTrongSale.Add(tiTrongSaleNam);
                TiTrongDaoTao.Add(tiTrongDaoTaoNam);
                TiTrongKeToan.Add(tiTrongKeToanNam);
                TiLeDoanhThuNen.Add(tiLeDoanhThuNenNam);
                TiLeDoanhThuVang.Add(tiLeDoanhThuVangNam);
                TiLeDoanhThuSuKien.Add(tiLeDoanhThuSuKienNam);
                TiLeDoanhThuHocBong.Add(tiLeDoanhThuHocBongNam);

                //% hv gd mới, gd lại
                decimal tileHVGDLNam = 0;
                decimal tileHVGDMNam = 0;
                decimal thangChotBQNam = 0;
                if (tongSoHocVienNam > 0)
                {
                    tileHVGDMNam = (hVGhiDanhMoiNam / tongSoHocVienNam) * 100;
                    tileHVGDLNam = (hVGhiDanhLaiNam / tongSoHocVienNam) * 100;
                    thangChotBQNam = tongSoThangDKNam / tongSoHocVienNam;
                }
                ThangChotBinhQuan.Add(thangChotBQNam);
                HVGhiDanhLai.Add(tileHVGDLNam);
                HVGhiDanhMoi.Add(tileHVGDMNam);
                TongSoHocVien.Add(tongSoHocVienNam);

                // ht cuộc gọi
                decimal htCGNam = 0;
                if (chiTieuCuocGoiNam > 0)
                    htCGNam = (hoanThanhCuocGoiNam / chiTieuCuocGoiNam) * 100;
                HTCuocGoi.Add(htCGNam);

                // ht học viên

                decimal htHVNam = 0;
                if (chiTieuHocVienNam > 0)
                    htHVNam = (tongSoHocVienNam / chiTieuHocVienNam) * 100;
                HTHocVien.Add(htHVNam);
                // Năng lực Sale
                SaleOver100.Add(saleOver100Nam);
                Sale30To50.Add(sale30To50Nam);
                Sale20To30.Add(sale20To30Nam);
                SaleUnder20.Add(saleUnder20Nam);
            }
            model.Months = listMonth.ToList();
            model.SoSale = SoSale;
            model.DinhBien = DinhBien;
            model.ChiTieuDS = ChiTieuDS;
            model.ThucDatDS = ThucDatDS;
            model.HTDS = HTDS;
            model.TiTrongSale = TiTrongSale;
            model.TiTrongDaoTao = TiTrongDaoTao;
            model.TiTrongKeToan = TiTrongKeToan;
            model.UuDaiBinhQuan = UuDaiBinhQuan;
            model.TongSoHocVien = TongSoHocVien;
            model.TongSoHocVien = TongSoHocVien;
            model.HVGhiDanhLai = HVGhiDanhLai;
            model.HVGhiDanhMoi = HVGhiDanhMoi;
            model.HTCuocGoi = HTCuocGoi;
            model.HTHocVien = HTHocVien;
            model.ThangChotBinhQuan = ThangChotBinhQuan;
            model.SaleOver100 = SaleOver100;
            model.Sale30To50 = Sale30To50;
            model.Sale20To30 = Sale20To30;
            model.SaleUnder20 = SaleUnder20;
            model.TiLeDoanhThuNen = TiLeDoanhThuNen;
            model.TiLeDoanhThuHocBong = TiLeDoanhThuHocBong;
            model.TiLeDoanhThuVang = TiLeDoanhThuVang;
            model.TiLeDoanhThuSuKien = TiLeDoanhThuSuKien;
            return View(model);
        }
        #region CallLogs
        public ActionResult ReportCall(int? page, int? ZoneId, int? OfficeId, string startDay, string endDay)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;

            if (string.IsNullOrEmpty(startDay))
                startDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            if (string.IsNullOrEmpty(endDay))
                endDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");

            DateTime startDate, endDate;
            if (!DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                cd = DateTime.Now;
            if (!DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                crd = DateTime.Now;

            startDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
            endDate = new DateTime(crd.Year, crd.Month, crd.Day, 0, 0, 0);

            var model = new ListCallViewModel
            {
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
                StartDay = startDay,
                EndDay = endDay
            };

            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h =>
                h.Month >= startDate.Month &&
                h.Month <= endDate.Month &&
                h.Year == startDate.Year)
                .Select(h => new { h.OfficeId, ZoneShortCode = h.Zone.ShortCode, h.ZoneId });

            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(o =>
                        historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                }
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                {
                    model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                    if (model.ZoneId == null)
                    {
                        model.Offices = model.Offices.Where(o =>
                            historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    }
                }
                else
                {
                    model.ZoneId = User.ZoneId;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(User.OfficeIds))
                    model.OfficeId = User.OfficeId;
                else
                {
                    model.Offices = model.Offices.Where(a =>
                        historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                }
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a =>
                    historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
            }

            if (model.Offices.Count() == 1)
                model.OfficeId = model.Offices.First().Id;

            if (model.OfficeId != null)
            {
                var officeId = model.OfficeId.Value;
                var months = Enumerable.Range(startDate.Month, endDate.Month - startDate.Month + 1).ToList();

                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(u =>
                    u.Active &&
                    months.Contains(u.Month) &&
                    u.OfficeId == officeId &&
                    (u.DayEnd == null || u.DayEnd >= startDate) &&
                    u.DayStart <= endDate &&
                    u.TypeUser != TypeUser.ASM &&
                    u.TypeUser != TypeUser.HO &&
                    u.TypeUser != TypeUser.CV &&
                    u.TypeUser != TypeUser.PKT &&
                    u.TypeUser != TypeUser.BM,
                    q => q.OrderBy(a => a.Sort).ThenBy(a => a.UserId).ThenBy(a => a.Month)).ToList();

                var userIds = historyUsers.Select(u => (int?)u.Id).ToList();
                var endDatePlusOne = endDate.AddDays(1);

                var callLogs = _unitOfWork.CallLogRepository.GetQuery(c =>
                    c.CallDate >= startDate &&
                    c.CallDate < endDatePlusOne &&
                    c.HistoryUser.OfficeId == officeId &&
                    userIds.Contains(c.HistoryUserId))
                    .Select(c => new { c.HistoryUserId, c.BillSec, c.Disposition }).ToList();

                // Gom lại theo HistoryUserId 
                var groupedLogs = callLogs.GroupBy(c => c.HistoryUserId)
                    .ToDictionary(
                        g => g.Key,
                        g => new
                        {
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
                        });

                var userItems = historyUsers.Select(u =>
                {
                    groupedLogs.TryGetValue(u.Id, out var stat);
                    return new ListCallViewModel.UserItem
                    {
                        HistoryUser = u,
                        Over120s = stat?.Over120s ?? 0,
                        Over90s = stat?.Over90s ?? 0,
                        Over60s = stat?.Over60s ?? 0,
                        Under60s = stat?.Under60s ?? 0,
                        Under30s = stat?.Under30s ?? 0,
                        NoAns = stat?.NoAns ?? 0,
                        Busy = stat?.Busy ?? 0,
                        Failed = stat?.Failed ?? 0,
                        TotalOver60s = stat?.TotalOver60s ?? 0,
                        TotalOver30s = stat?.TotalOver30s ?? 0,
                        Total = stat?.Total ?? 0
                    };
                }).ToList();

                model.UserItems = userItems;
                model.TotalOver120s = userItems.Sum(a => a.Over120s);
                model.TotalOver90s = userItems.Sum(a => a.Over90s);
                model.TotalOver60s = userItems.Sum(a => a.Over60s);
                model.TotalUnder60s = userItems.Sum(a => a.Under60s);
                model.TotalUnder30s = userItems.Sum(a => a.Under30s);
            }

            return View(model);
        }

        public PartialViewResult LoadListCallDay(int userId, string startDay, string endDay)
        {
            var huser = _unitOfWork.HistoryUserRepository.GetById(userId);
            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();
            if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                startDate = cd.Date;

            if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                endDate = crd.Date;

            var listCallLog = _unitOfWork.CallLogRepository.GetQuery(a => DbFunctions.TruncateTime(a.CallDate) >= startDate && DbFunctions.TruncateTime(a.CallDate) <= endDate && a.HistoryUserId == userId).AsNoTracking().ToList();
            var listDate = new List<DateTime>();
            for (var day = startDate; day <= endDate; day = day.AddDays(1))
            {
                listDate.Add(day);
            }

            var dateItems = listDate.Select(a => new LoadListCallDayViewModel.DateItem
            {
                Date = a,
                Total = listCallLog.Count(x => x.CallDate.Date == a.Date),
                Over60s = listCallLog.Count(x => x.CallDate.Date == a.Date && x.BillSec >= 60),
                Over30s = listCallLog.Count(x => x.CallDate.Date == a.Date && x.BillSec >= 30),
            });
            var model = new LoadListCallDayViewModel
            {
                StartDay = startDay,
                EndDay = endDay,
                DateItems = dateItems,
                User = huser,
            };
            return PartialView(model);
        }
        public PartialViewResult LoadListCall(int userId, int type, string startDay, string endDay)
        {
            var model = new LoadListCallViewModel
            {
                StartDay = startDay,
                EndDay = endDay,
                User = _unitOfWork.HistoryUserRepository.GetById(userId),
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
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.HistoryUserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 120);
                    ViewBag.Type = "trên 2 phút";
                    break;
                case 90:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.HistoryUserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 90 && p.BillSec <= 120);
                    ViewBag.Type = "trên 1,5 phút";
                    break;
                case 60:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.HistoryUserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 60 && p.BillSec <= 90);
                    ViewBag.Type = "trên 1 phút";
                    break;
                case 59:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.HistoryUserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 30 && p.BillSec < 60);
                    ViewBag.Type = "dưới 1 phút";
                    break;
                case 30:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.HistoryUserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec < 30 && p.Disposition == "ANSWERED");
                    ViewBag.Type = "Dưới 30s";
                    break;
                default:
                    ViewBag.Type = "";
                    break;
            }

            return PartialView(model);
        }
        public void ExportCallLogUser(string startDay, string endDay, int userId)
        {
            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();
            var hUser = _unitOfWork.HistoryUserRepository.GetById(userId);
            if (hUser == null)
                return;
            if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                startDate = cd.Date;
            else
                return;
            if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                endDate = crd.Date;
            else
                return;
            var listCallLog = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == userId && DbFunctions.TruncateTime(a.CallDate) >= startDate && DbFunctions.TruncateTime(a.CallDate) <= endDate,
                q => q.OrderBy(a => a.CallDate));
            // Tạo bảng dữ liệu
            var dt = new DataTable();
            dt.Columns.Add("Mã cuộc gọi");
            dt.Columns.Add("Ngày gọi");
            //dt.Columns.Add("Mã nhân viên");
            //dt.Columns.Add("Tên nhân viên");
            dt.Columns.Add("Phone");
            dt.Columns.Add("Duration");
            dt.Columns.Add("BillSec");
            dt.Columns.Add("RecordingFile");

            var filename = $"danh-sach-cuoc-goi.xlsx";
            foreach (var item in listCallLog)
            {
                dt.Rows.Add(item.UniqueId, item.CallDate, /*item.Exten, item.User.Fullname,*/ item.Phone, item.Duration, item.BillSec, item.RecordingFile);
            }

            // Xuất Excel
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Báo cáo cuộc gọi");
                ws.Cells[1, 1].Value = "Danh sách cuộc gọi nhân sự " + hUser.User.Fullname + " - MNV: " + hUser.User.MaNhanVien + ", từ " + startDay + " đến " + endDay;
                ws.Cells[1, 1, 1, 6].Merge = true;

                ws.Cells[1, 1].Style.Font.Bold = true;
                ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // 👉 Đổ DataTable từ dòng 2
                ws.Cells[2, 1].LoadFromDataTable(dt, true);

                ws.Cells.AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        //public ActionResult ChangeCallLogDataCN(int officeId)
        //{
        //    var o = _unitOfWork.OfficeRepository.GetById(officeId);
        //    if (o == null)
        //        return Content("không có CN " + officeId);
        //    var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.OfficeId == officeId);
        //    var count = 0;
        //    foreach (var h in historyUsers)
        //    {
        //        var calllogs = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUser.UserId == h.UserId && a.HistoryUser.TypeUser == h.TypeUser && a.HistoryUser.Status == h.Status
        //        && a.HistoryUser.Month == h.Month && a.HistoryUser.Year == h.Year && a.HistoryUser.OfficeId == null);
        //        foreach (var c in calllogs)
        //        {
        //            c.HistoryUserId = h.Id;
        //            count++;
        //        }
        //    }
        //    _unitOfWork.Save();
        //    return Content("Đã chuyển dữ liệu cuộc gọi CN " + o.Name + ": " + count + " cuộc gọi");

        //}
        //public ActionResult ChangeCallLogDataAll()
        //{
        //    var os = _unitOfWork.OfficeRepository.GetQuery();
        //    var count = 0;
        //    foreach (var o in os)
        //    {
        //        var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.OfficeId == o.Id);
        //        foreach (var h in historyUsers)
        //        {
        //            var calllogs = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUser.UserId == h.UserId && a.HistoryUser.TypeUser == h.TypeUser && a.HistoryUser.Status == h.Status
        //            && a.HistoryUser.Month == h.Month && a.HistoryUser.Year == h.Year && a.HistoryUser.OfficeId == null);
        //            foreach (var c in calllogs)
        //            {
        //                c.HistoryUserId = h.Id;
        //                count++;
        //            }
        //        }
        //    }

        //    _unitOfWork.Save();
        //    return Content("Đã chuyển dữ liệu cuộc gọi CN All: " + count + " cuộc gọi");

        //}

        //        public ActionResult ChangeCallLogData(int day)
        //        {
        //            for (int i = 0; i < day; i++) // ví dụ 30 ngày gần đây
        //            {
        //                var date = DateTime.Today.AddDays(-i);
        //                string sql = $@"
        //    UPDATE CallLogs
        //    SET HistoryUserId = (
        //        SELECT TOP 1 h.Id
        //        FROM HistoryUsers h
        //        WHERE h.UserId = CallLogs.UserId
        //          AND h.DayStart <= CallLogs.CallDate
        //          AND (h.DayEnd IS NULL OR h.DayEnd >= CallLogs.CallDate)
        //        ORDER BY 
        //CASE WHEN h.DayEnd IS NULL THEN 1 ELSE 0 END,
        //        h.DayEnd ASC  
        //    )
        //    WHERE HistoryUserId IS NULL AND CAST(CallDate AS DATE) = '{date:yyyy-MM-dd}'";

        //                _unitOfWork.ExecuteSqlCommand(sql);
        //            }
        //            return Content("Đã chuyển dữ liệu cuộc gọi");

        //        }

        #endregion
    }
}
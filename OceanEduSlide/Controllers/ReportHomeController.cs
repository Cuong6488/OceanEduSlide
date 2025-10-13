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
using static Microsoft.IO.RecyclableMemoryStreamManager;
using OfficeOpenXml;
using System.Data;
using OceanEduSlide.EnumHelpers;

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
        public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year, int? categoryid, int sort = 1)
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            categoryid = categoryid ?? 35;
            int currentMonth = Month ?? DateTime.Now.Month;
            int currentYear = Year ?? DateTime.Now.Year;
            int pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == currentMonth && h.Year == currentYear).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            // 1. Truy vấn danh sách Office theo quyền truy cập
            var officeQuery = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsQueryable();

            if (User.TypeUser == TypeUser.CV)
            {
                if (!ZoneId.HasValue)
                    officeQuery = officeQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                if (!ZoneId.HasValue)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        officeQuery = officeQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    }
                    else
                    {
                        officeQuery = officeQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.Zone.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                    }
                }
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                if (string.IsNullOrEmpty(User.OfficeIds))
                    officeQuery = officeQuery.Where(a => a.Id == User.OfficeId);
                else
                {
                    officeQuery = officeQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                }
            }

            if (ZoneId.HasValue)
            {
                officeQuery = officeQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == ZoneId.Value));
            }
            //if (OfficeId.HasValue)
            //{
            //    officeQuery = officeQuery.Where(a =>  a.Id == OfficeId.Value);
            //}

            var allOffices = officeQuery.AsNoTracking().ToList();
            var officeIds = allOffices.Select(o => o.Id).ToList();

            // 2. Truy vấn ReportData cho chỉ ReportCategoryId
            var reportDataRaw = _unitOfWork.ReportDataRepository
                .GetQuery(a =>
                    a.Active &&
                    a.Month == currentMonth &&
                    a.Year == currentYear &&
                    a.ReportCategory.TypeCat == TypeCat.Type1 &&
                    a.ReportCategoryId == categoryid &&
                    officeIds.Contains(a.OfficeId ?? 0)) // giới hạn trong office được truy cập
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
                        decimal val;
                        var cleaned = r.Data?.Replace(".", "").Replace(",", "").Replace("%", "") ?? "0";
                        return decimal.TryParse(cleaned, out val) ? val : 0;
                    })
                );

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
                a.Month == currentMonth &&
                a.Year == currentYear &&
                a.ReportCategory.TypeCat == TypeCat.Type1 &&
                pagedOfficeIds.Contains(a.OfficeId ?? 0))
                .AsNoTracking()
                .ToList();

            // 6. Chuẩn bị ViewModel
            var model = new ListReportHomeViewModel
            {
                Month = currentMonth,
                Year = currentYear,
                User = User,
                ZoneId = ZoneId,
                categoryId = categoryid,
                sort = sort,
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
                //model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);

                    }
                    else
                    {
                        model.ZoneId = User.ZoneId;
                    }

                }
            }

            // 8. OfficeIds cho ViewBag
            ViewBag.OfficeIds = "," + string.Join(",", reportDatas.Select(d => d.OfficeId).Distinct()) + ",";

            return View(model);
        }
        public void ExportKDCN(int Year, int Month, int? ZoneId, int? ReportCategoryId)
        {
            // Truy xuất historyOffices trước để dùng filter
            var historyOffices = _unitOfWork.HistoryOfficeRepository
                .GetQuery(h => h.Month == Month && h.Year == Year)
                .Select(h => new
                {
                    h.OfficeId,
                    ZoneShortCode = h.Zone.ShortCode,
                    h.ZoneId
                })
                .ToList();

            // Truy xuất Office ban đầu
            var offices = _unitOfWork.OfficeRepository
                .GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId))
                .ToList();

            // Lọc quyền người dùng
            if (User.TypeUser == TypeUser.CV)
            {
                if (ZoneId == null)
                {
                    offices = offices.Where(o => o.ZoneId != null && User.ZoneIds.Contains("," + o.Zone.ShortCode + ",")).ToList();
                }
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        if (ZoneId == null)
                        {
                            var allowedOfficeIds = historyOffices
                                .Where(h => User.ZoneIds.Contains("," + h.ZoneShortCode + ","))
                                .Select(h => h.OfficeId)
                                .ToHashSet();

                            offices = offices.Where(o => allowedOfficeIds.Contains(o.Id)).ToList();
                        }
                    }
                    else
                    {
                        ZoneId = User.ZoneId;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(User.OfficeIds))
                    {
                        var allowedOfficeIds = historyOffices
                            .Where(h => User.OfficeIds.Contains("," + h.OfficeId + ","))
                            .Select(h => h.OfficeId)
                            .ToHashSet();

                        offices = offices.Where(o => allowedOfficeIds.Contains(o.Id)).ToList();
                    }
                    else
                    {
                        offices = offices.Where(a => a.Id == User.OfficeId).ToList();
                    }
                }
            }

            if (ZoneId != null)
            {
                var zoneOfficeIds = historyOffices
                    .Where(h => h.ZoneId == ZoneId)
                    .Select(h => h.OfficeId)
                    .ToHashSet();

                offices = offices.Where(o => zoneOfficeIds.Contains(o.Id)).ToList();
            }

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
                .GetQuery(r => r.Active && r.Month == Month && r.Year == Year && r.OfficeId != null && officeIds.Contains(r.OfficeId.Value))
                .ToList();

            // Gom dữ liệu report theo OfficeId + CategoryId để truy xuất nhanh
            var reportDataDict = reportDataList
                .GroupBy(r => (r.OfficeId.Value, r.ReportCategoryId))
                .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Data ?? "");

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
            $"{Month}/{Year}",                 // Tháng
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
                            var report = reportDataDict.ContainsKey(key) ? reportDataDict[key] : "";
                            listData.Add(report);
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

        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? UserType, int? Month, int? Year, int? categoryid, int sort = 1)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;
            //categoryid = categoryid ?? 88;
            var selectedMonth = Month ?? DateTime.Now.Month;
            var selectedYear = Year ?? DateTime.Now.Year;

            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == selectedMonth && h.Year == selectedYear).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == selectedMonth && a.Year == selectedYear
            && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == selectedMonth) || a.DayEnd.Value.Month != selectedMonth)))
            && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM);

            if (User.TypeUser != TypeUser.ASM)
            {
                historyQuery = historyQuery.Where(a => a.TypeUser != TypeUser.AEC);
            }
            if (UserType != null)
            {
                historyQuery = historyQuery.Where(a => (int)a.TypeUser == UserType);
            }
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort));
            var zones = _unitOfWork.ZoneRepository.Get(a => a.Active);

            var model = new ListReportNVHomeViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                Offices = offices,
                User = User,
                ZoneId = ZoneId,
                categoryId = categoryid,
                sort = sort,
                UserType = UserType,
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
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    if (model.OfficeId == null)
                        historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                }
            }
            else
            {
                //model.ZoneId = User.ZoneId;

                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ","));
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            if (model.OfficeId == null)
                                historyQuery = historyQuery.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")))
                                || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && User.ZoneIds.Contains("," + a.Zone.ShortCode + ",")));
                        }
                    }
                    else
                    {
                        model.ZoneId = User.ZoneId;
                    }
                    //filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                    {
                        model.OfficeId = User.OfficeId;
                    }
                    else
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                        if (model.OfficeId == null)
                        {
                            historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.OfficeIds.Contains("," + h.OfficeId + ",")));
                        }
                    }
                }
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
                //filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);
                if (User.TypeUser != TypeUser.ASM)
                    historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId));
                else
                    historyQuery = historyQuery.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId))
                    || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && model.ZoneId == a.ZoneId));
            }

            if (model.OfficeId != null)
            {
                historyQuery = historyQuery.Where(a => a.OfficeId == model.OfficeId);
            }
            IEnumerable<HistoryUser> filteredHistoryUsers = historyQuery.OrderBy(a => a.OfficeId).ToList();

            // LẤY ReportData CHỈ CHO categoryid (dùng để sort user)
            //var userIds = filteredUsers.Select(u => u.Id).ToList();
            var historyUserIds = filteredHistoryUsers.Select(h => h.Id).ToList();
            var reportData88 = _unitOfWork.ReportDataRepository.GetQuery(a =>
                a.Active &&
                a.Month == selectedMonth &&
                a.Year == selectedYear &&
                a.ReportCategory.TypeCat == TypeCat.Type2 &&
                a.ReportCategoryId == categoryid &&
                historyUserIds.Contains(a.HistoryUserId ?? 0)).ToList();

            // Tính tổng
            var userDataDict = reportData88
                .GroupBy(r => r.HistoryUserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r =>
                    {
                        int val;
                        var cleanedData = r.Data?.Replace(",", "").Replace(".", "").Replace("%", "");
                        return int.TryParse(cleanedData, out val) ? val : 0;
                    })
                );


            // SẮP XẾP LẠI USER TRƯỚC KHI PHÂN TRANG
            //filteredHistoryUsers = filteredHistoryUsers
            //    .OrderByDescending(u => userDataDict.ContainsKey(u.Id) ? userDataDict[u.Id] : 0)
            //    .ThenBy(u => u.OfficeId);

            //var usersHasData = filteredHistoryUsers
            //    .Where(u => userDataDict.ContainsKey(u.Id))
            //    .ToList();

            //var usersNoData = filteredHistoryUsers
            //    .Where(u => !userDataDict.ContainsKey(u.Id))
            //    .ToList();

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
            //if (sort == 1)
            //{
            //    // Giảm dần (mặc định)
            //    usersHasData = usersHasData
            //        .OrderByDescending(u => userDataDict[u.Id])
            //        .ThenBy(u => u.OfficeId)
            //        .ToList();

            //    sortedUsers = usersHasData.Concat(usersNoData.OrderBy(u => u.OfficeId)).ToList();
            //}
            //else
            //{
            //    // Tăng dần, và user không có data sẽ lên trước
            //    usersHasData = usersHasData
            //        .OrderBy(u => userDataDict[u.Id])
            //        .ThenBy(u => u.OfficeId)
            //        .ToList();

            //    sortedUsers = usersNoData.OrderBy(u => u.OfficeId).Concat(usersHasData).ToList();
            //}

            filteredHistoryUsers = sortedUsers;


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
        public void ExportKDNV(int Year, int Month, int? ZoneId, int? OfficeId, int? UserType)
        {
            // Lấy thông tin HistoryOffice trước
            // Lấy historyOffice với đầy đủ thông tin
            var historyOfficeList = _unitOfWork.HistoryOfficeRepository
                .GetQuery(h => h.Month == Month && h.Year == Year)
                .Include(h => h.Zone)
                .ToList();

            var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a =>
                a.Active &&
                a.Month == Month &&
                a.Year == Year &&
                (a.DayEnd == null || (a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == Month) || a.DayEnd.Value.Month != Month) &&
                a.TypeUser != TypeUser.HO &&
                a.TypeUser != TypeUser.CV &&
                a.TypeUser != TypeUser.PKT &&
                a.TypeUser != TypeUser.ASM
            );

            if (User.TypeUser != TypeUser.ASM)
            {
                historyQuery = historyQuery.Where(a => a.TypeUser != TypeUser.AEC);
            }

            if (UserType != null)
            {
                historyQuery = historyQuery.Where(a => (int)a.TypeUser == UserType);
            }

            // Quyền người dùng
            if (User.TypeUser == TypeUser.CV)
            {
                if (ZoneId == null && OfficeId == null)
                {
                    var allowedOfficeIds = historyOfficeList
                        .Where(h => User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                        .Select(h => h.OfficeId)
                        .Distinct()
                        .ToList();

                    historyQuery = historyQuery.Where(a => a.OfficeId.HasValue && allowedOfficeIds.Contains(a.OfficeId.Value));
                }
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                {
                    if (ZoneId == null && OfficeId == null)
                    {
                        var allowedOfficeIds = historyOfficeList
                            .Where(h => User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            .Select(h => h.OfficeId)
                            .Distinct()
                            .ToList();

                        var allowedZoneIds = historyOfficeList
                            .Where(h => User.ZoneIds.Contains("," + h.Zone.ShortCode + ","))
                            .Select(h => h.ZoneId)
                            .Distinct()
                            .ToList();

                        historyQuery = historyQuery.Where(a =>
                            (a.TypeUser != TypeUser.AEC && a.OfficeId.HasValue && allowedOfficeIds.Contains(a.OfficeId.Value)) ||
                            (a.TypeUser == TypeUser.AEC && a.ZoneId != null && allowedZoneIds.Contains(a.ZoneId.Value)));
                    }
                }
                else
                {
                    ZoneId = User.ZoneId;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(User.OfficeIds))
                {
                    OfficeId = User.OfficeId;
                }
                else
                {
                    if (OfficeId == null)
                    {
                        var allowedOfficeIds = historyOfficeList
                            .Where(h => User.OfficeIds.Contains("," + h.OfficeId + ","))
                            .Select(h => h.OfficeId)
                            .Distinct()
                            .ToList();

                        historyQuery = historyQuery.Where(a => a.OfficeId.HasValue && allowedOfficeIds.Contains(a.OfficeId.Value));
                    }
                }
            }

            if (ZoneId != null)
            {
                var zoneOfficeIds = historyOfficeList
                    .Where(h => h.ZoneId == ZoneId)
                    .Select(h => h.OfficeId)
                    .ToList();

                if (User.TypeUser != TypeUser.ASM)
                {
                    historyQuery = historyQuery.Where(a => a.OfficeId.HasValue && zoneOfficeIds.Contains(a.OfficeId.Value));
                }
                else
                {
                    historyQuery = historyQuery.Where(a =>
                        (a.TypeUser != TypeUser.AEC && a.OfficeId.HasValue && zoneOfficeIds.Contains(a.OfficeId.Value)) ||
                        (a.TypeUser == TypeUser.AEC && a.ZoneId == ZoneId));
                }
            }

            if (OfficeId != null)
            {
                historyQuery = historyQuery.Where(a => a.OfficeId == OfficeId);
            }

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
                            var report = userReports.FirstOrDefault(r => r.ReportCategoryId == category.Id)?.Data ?? "";
                            listData.Add(report);
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
            for(var day = startDate; day <= endDate; day = day.AddDays(1))
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
                User = huser.User,
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
        public ActionResult ChangeCallLogDataCN(int officeId)
        {
            var o = _unitOfWork.OfficeRepository.GetById(officeId);
            if (o == null)
                return Content("không có CN " + officeId);
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.OfficeId == officeId);
            var count = 0;
            foreach (var h in historyUsers)
            {
                var calllogs = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUser.UserId == h.UserId && a.HistoryUser.TypeUser == h.TypeUser && a.HistoryUser.Status == h.Status
                && a.HistoryUser.Month == h.Month && a.HistoryUser.Year == h.Year && a.HistoryUser.OfficeId == null);
                foreach (var c in calllogs)
                {
                    c.HistoryUserId = h.Id;
                    count++;
                }
            }
            _unitOfWork.Save();
            return Content("Đã chuyển dữ liệu cuộc gọi CN " + o.Name + ": " + count + " cuộc gọi");

        }
        public ActionResult ChangeCallLogDataAll()
        {
            var os = _unitOfWork.OfficeRepository.GetQuery();
            var count = 0;
            foreach (var o in os)
            {
                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.OfficeId == o.Id);
                foreach (var h in historyUsers)
                {
                    var calllogs = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUser.UserId == h.UserId && a.HistoryUser.TypeUser == h.TypeUser && a.HistoryUser.Status == h.Status
                    && a.HistoryUser.Month == h.Month && a.HistoryUser.Year == h.Year && a.HistoryUser.OfficeId == null);
                    foreach (var c in calllogs)
                    {
                        c.HistoryUserId = h.Id;
                        count++;
                    }
                }
            }

            _unitOfWork.Save();
            return Content("Đã chuyển dữ liệu cuộc gọi CN All: " + count + " cuộc gọi");

        }

        public ActionResult ChangeCallLogData(int day)
        {
            for (int i = 0; i < day; i++) // ví dụ 30 ngày gần đây
            {
                var date = DateTime.Today.AddDays(-i);
                string sql = $@"
    UPDATE CallLogs
    SET HistoryUserId = (
        SELECT TOP 1 h.Id
        FROM HistoryUsers h
        WHERE h.UserId = CallLogs.UserId
          AND h.DayStart <= CallLogs.CallDate
          AND (h.DayEnd IS NULL OR h.DayEnd >= CallLogs.CallDate)
        ORDER BY 
CASE WHEN h.DayEnd IS NULL THEN 1 ELSE 0 END,
        h.DayEnd ASC  
    )
    WHERE HistoryUserId IS NULL AND CAST(CallDate AS DATE) = '{date:yyyy-MM-dd}'";

                _unitOfWork.ExecuteSqlCommand(sql);
            }
            return Content("Đã chuyển dữ liệu cuộc gọi");

        }
        public async Task<ActionResult> TestSync()
        {
            var service = new CallLogService();
            await service.SyncRecentlyAsync();
            return Content("Đã đồng bộ thủ công.");
        }
        public async Task<ActionResult> SyncCustom(int month, int day)
        {
            var service = new CallLogService();
            await service.SyncCusTom(month, day);
            return Content("Đã đồng bộ 7 ngày. " + day + " - " + month);
        }
        public async Task<ActionResult> CheckCountCallLog()
        {
            string user = "lvd";
            string pass = "qazplm123`$%^";
            string baseUrl = "https://voip.ocean.edu.vn/api/report.php";

            using (var http = new HttpClient()) // dùng một lần
            {

                for (int i = 17; i <= 31; i++)
                {
                    DateTime date = new DateTime(2025, 8, i);
                    string tbegin = date.ToString("yyyy/MM/dd");
                    string tend = date.AddDays(1).ToString("yyyy/MM/dd");

                    string url = $"{baseUrl}?user={user}&pass={Uri.EscapeDataString(pass)}&tbegin={tbegin}&tend={tend}&type=1";

                    try
                    {
                        var json = await http.GetStringAsync(url);

                        if (string.IsNullOrWhiteSpace(json))
                        {
                            continue;
                        }

                        var allLogs = JsonConvert.DeserializeObject<List<CallLog>>(json);

                        if (allLogs == null || allLogs.Count == 0)
                        {
                            continue;
                        }

                        // Dùng LINQ một lần cho cả hai kết quả
                        var filteredLogs = allLogs.Where(a => a.Exten == "25050544").ToList();
                        int totalCount = filteredLogs.Count;
                        int count60s = filteredLogs.Count(a => a.BillSec >= 60);
                    }
                    catch (JsonException jsonEx)
                    {
                    }
                    catch (HttpRequestException httpEx)
                    {
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            return Content("Checked");
        }


        #endregion
    }
}
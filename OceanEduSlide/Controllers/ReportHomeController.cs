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

        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? UserId, int? UserType, int? Month, int? Year, int? categoryid/*, List<int> ListMonth*/, int sort = 1)
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            //var allHistoryZoneIds = new HashSet<string>(
            //    historyUsers.Where(hu => !string.IsNullOrEmpty(hu.ZoneIds)).SelectMany(hu => hu.ZoneIds.Split(',', (char)StringSplitOptions.RemoveEmptyEntries)));
            var pageNumber = page ?? 1;
            ViewBag.Page = pageNumber;
            //categoryid = categoryid ?? 88;
            var selectedMonth = Month ?? DateTime.Now.Month;
            var selectedYear = Year ?? DateTime.Now.Year;
            //ListMonth = ListMonth ?? new List<int>() { DateTime.Now.Month };
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && a.Month == selectedMonth && a.Year == selectedYear).AsNoTracking();

            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == selectedMonth && h.Year == selectedYear).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            var historyQuery = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == selectedMonth && a.Year == selectedYear
            && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == selectedMonth) || a.DayEnd.Value.Month != selectedMonth)))
            && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM);
            var listHistoryUser = historyQuery;
            //if (User.TypeUser != TypeUser.ASM)
            //{
            //    historyQuery = historyQuery.Where(a => a.TypeUser != TypeUser.AEC);
            //}
            if (UserType != null)
            {
                if (UserId == null)
                    historyQuery = historyQuery.Where(a => (int)a.TypeUser == UserType);
                //listHistoryUser = listHistoryUser.Where(a => (int)a.TypeUser == UserType);
            }
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort));
            var zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            //var users = _unitOfWork.UserRepository.Get(a => a.Active && listHistoryUser.Contains(a.Id));
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
                OfficeId = OfficeId,
                UserId = UserId,
                //ListMonth = ListMonth
            };

            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = zones;
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));
                    if (model.OfficeId == null)
                    {
                        listHistoryUser = listHistoryUser.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
                        if (model.UserId == null)
                            historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))));
                    }
                }
            }
            else
            {
                //model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = zones.Where(a => User.ZoneIds.Contains("," + a.ShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                        if (model.ZoneId == null)
                        {
                            //model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));

                            if (model.OfficeId == null)
                            {
                                listHistoryUser = listHistoryUser.Where(a => (a.TypeUser != TypeUser.AEC && (historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))))
                                                               || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && (User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.Zone.ShortCode + ",")))));
                                if (model.UserId == null)
                                    historyQuery = historyQuery.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))))
                                                               || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && (User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.Zone.ShortCode + ",")))));
                            }
                        }
                    }
                    else if (User.ZoneId != null)
                    {
                        model.Zones = zones.Where(a => a.Id == User.ZoneId || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.ShortCode + ","))));
                        if (model.ZoneId == null)
                        {
                            //model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.ZoneId == h.ZoneId || (historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))));

                            if (model.OfficeId == null)
                            {
                                listHistoryUser = listHistoryUser.Where(a => (a.TypeUser != TypeUser.AEC && (historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ","))))))
                                                               || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && (User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.Zone.ShortCode + ",")))));
                                if (model.UserId == null)
                                    historyQuery = historyQuery.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.ZoneIds.Contains("," + h.ZoneShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + h.ZoneShortCode + ",")))))
                                                               || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && (User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") || historyUsers.Any(hu => hu.ZoneIds != null && hu.ZoneIds.Contains("," + a.Zone.ShortCode + ",")))));
                            }
                        }
                        //model.ZoneId = User.ZoneId;
                    }
                    //filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));

                        if (model.OfficeId == null)
                        {
                            listHistoryUser = listHistoryUser.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                            if (model.UserId == null)
                                historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.OfficeId == h.OfficeId || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));

                        }

                        //model.OfficeId = User.OfficeId;


                    }
                    else
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && (User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",") || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));

                        if (model.OfficeId == null)
                        {
                            listHistoryUser = listHistoryUser.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.OfficeIds.Contains("," + h.OfficeId + ",") || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));
                            if (model.UserId == null)
                                historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && (User.OfficeIds.Contains("," + h.OfficeId + ",") || historyUsers.Any(hu => hu.OfficeIds != null && hu.OfficeIds.Contains("," + h.OfficeId + ",")))));

                        }
                    }
                }
            }

            if (model.ZoneId != null)
            {
                //var zone = zones.FirstOrDefault(a => a.Id == ZoneId);
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
                if (model.OfficeId == null)
                {
                    listHistoryUser = listHistoryUser.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId))
                                            || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && model.ZoneId == a.ZoneId));
                    if (model.UserId == null)
                        historyQuery = historyQuery.Where(a => (a.TypeUser != TypeUser.AEC && historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId))
                                                || (a.TypeUser == TypeUser.AEC && a.ZoneId != null && model.ZoneId == a.ZoneId));
                }
            }

            if (model.OfficeId != null)
            {
                listHistoryUser = listHistoryUser.Where(a => a.OfficeId == model.OfficeId);

                if (model.UserId == null)
                    historyQuery = historyQuery.Where(a => a.OfficeId == model.OfficeId);
            }
            model.ListHistoryUser = listHistoryUser.ToList();
            if (model.UserId != null)
            {
                historyQuery = historyQuery.Where(a => a.Id == model.UserId);
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

            //if (User.TypeUser != TypeUser.ASM)
            //{
            //    historyQuery = historyQuery.Where(a => a.TypeUser != TypeUser.AEC);
            //}

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
            else if (User.TypeUser != TypeUser.HO)
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

        public ActionResult ReportTHCN(int? ZoneId, int? OfficeId, int? Year)
        {
            if (User.TypeUser == null)
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
            List<decimal> SoSale = new List<decimal>();
            List<decimal> DinhBien = new List<decimal>();
            List<decimal> ChiTieuDS = new List<decimal>();
            List<decimal> ThucDatDS = new List<decimal>();
            List<decimal> HTDS = new List<decimal>();
            List<decimal> TiTrongSale = new List<decimal>();
            List<decimal> TiTrongDaoTao = new List<decimal>();
            List<decimal> TiTrongKeToan = new List<decimal>();
            List<decimal> UuDaiBinhQuan = new List<decimal>();
            List<decimal> ChiTieuHocVien = new List<decimal>();
            List<decimal> TongSoHocVien = new List<decimal>();
            List<decimal> HVGhiDanhLai = new List<decimal>();
            List<decimal> HVGhiDanhMoi = new List<decimal>();
            List<decimal> HTCuocGoi = new List<decimal>();
            List<decimal> HTHocVien = new List<decimal>();
            List<decimal> ThangChotBinhQuan = new List<decimal>();
            List<decimal> SaleOver100 = new List<decimal>();
            List<decimal> Sale30To50 = new List<decimal>();
            List<decimal> Sale20To30 = new List<decimal>();
            List<decimal> SaleUnder20 = new List<decimal>();
            List<decimal> TiLeDoanhThuNen = new List<decimal>();
            List<decimal> TiLeDoanhThuHocBong = new List<decimal>();
            List<decimal> TiLeDoanhThuVang = new List<decimal>();
            List<decimal> TiLeDoanhThuSuKien = new List<decimal>();


            // Tạo các biến cho quý

            decimal soSaleQuy1 = 0;
            decimal dinhBienQuy1 = 0;
            decimal chiTieuDSQuy1 = 0;
            decimal thucDatDSQuy1 = 0;
            decimal DSSaleQuy1 = 0;
            decimal DSDaoTaoQuy1 = 0;
            decimal DSKeToanQuy1 = 0;
            decimal tongSoHocVienQuy1 = 0; // hoàn thành hv
            decimal hVGhiDanhLaiQuy1 = 0;
            decimal hVGhiDanhMoiQuy1 = 0;
            decimal chiTieuHocVienQuy1 = 0;
            decimal chiTieuCuocGoiQuy1 = 0;
            decimal hoanThanhCuocGoiQuy1 = 0;
            decimal saleOver100Quy1 = 0;
            decimal sale30To50Quy1 = 0;
            decimal sale20To30Quy1 = 0;
            decimal saleUnder20Quy1 = 0;
            decimal doanhThuNenQuy1 = 0;
            decimal doanhThuHocBongQuy1 = 0;
            decimal doanhThuVangQuy1 = 0;
            decimal doanhThuSuKienQuy1 = 0;

            decimal soSaleQuy2 = 0;
            decimal dinhBienQuy2 = 0;
            decimal chiTieuDSQuy2 = 0;
            decimal thucDatDSQuy2 = 0;
            decimal DSSaleQuy2 = 0;
            decimal DSDaoTaoQuy2 = 0;
            decimal DSKeToanQuy2 = 0;
            decimal tongSoHocVienQuy2 = 0; // hoàn thành hv
            decimal hVGhiDanhLaiQuy2 = 0;
            decimal hVGhiDanhMoiQuy2 = 0;
            decimal chiTieuHocVienQuy2 = 0;
            decimal chiTieuCuocGoiQuy2 = 0;
            decimal hoanThanhCuocGoiQuy2 = 0;
            decimal saleOver100Quy2 = 0;
            decimal sale30To50Quy2 = 0;
            decimal sale20To30Quy2 = 0;
            decimal saleUnder20Quy2 = 0;
            decimal doanhThuNenQuy2 = 0;
            decimal doanhThuHocBongQuy2 = 0;
            decimal doanhThuVangQuy2 = 0;
            decimal doanhThuSuKienQuy2 = 0;

            decimal soSaleQuy3 = 0;
            decimal dinhBienQuy3 = 0;
            decimal chiTieuDSQuy3 = 0;
            decimal thucDatDSQuy3 = 0;
            decimal DSSaleQuy3 = 0;
            decimal DSDaoTaoQuy3 = 0;
            decimal DSKeToanQuy3 = 0;
            decimal tongSoHocVienQuy3 = 0; // hoàn thành hv
            decimal hVGhiDanhLaiQuy3 = 0;
            decimal hVGhiDanhMoiQuy3 = 0;
            decimal chiTieuHocVienQuy3 = 0;
            decimal chiTieuCuocGoiQuy3 = 0;
            decimal hoanThanhCuocGoiQuy3 = 0;
            decimal saleOver100Quy3 = 0;
            decimal sale30To50Quy3 = 0;
            decimal sale20To30Quy3 = 0;
            decimal saleUnder20Quy3 = 0;
            decimal doanhThuNenQuy3 = 0;
            decimal doanhThuHocBongQuy3 = 0;
            decimal doanhThuVangQuy3 = 0;
            decimal doanhThuSuKienQuy3 = 0;

            decimal soSaleQuy4 = 0;
            decimal dinhBienQuy4 = 0;
            decimal chiTieuDSQuy4 = 0;
            decimal thucDatDSQuy4 = 0;
            decimal DSSaleQuy4 = 0;
            decimal DSDaoTaoQuy4 = 0;
            decimal DSKeToanQuy4 = 0;
            decimal tongSoHocVienQuy4 = 0; // hoàn thành hv
            decimal hVGhiDanhLaiQuy4 = 0;
            decimal hVGhiDanhMoiQuy4 = 0;
            decimal chiTieuHocVienQuy4 = 0;
            decimal chiTieuCuocGoiQuy4 = 0;
            decimal hoanThanhCuocGoiQuy4 = 0;
            decimal saleOver100Quy4 = 0;
            decimal sale30To50Quy4 = 0;
            decimal sale20To30Quy4 = 0;
            decimal saleUnder20Quy4 = 0;
            decimal doanhThuNenQuy4 = 0;
            decimal doanhThuHocBongQuy4 = 0;
            decimal doanhThuVangQuy4 = 0;
            decimal doanhThuSuKienQuy4 = 0;

            decimal soSaleNam = 0;
            decimal dinhBienNam = 0;
            decimal chiTieuDSNam = 0;
            decimal thucDatDSNam = 0;
            decimal DSSaleNam = 0;
            decimal DSDaoTaoNam = 0;
            decimal DSKeToanNam = 0;
            decimal tongSoHocVienNam = 0; // hoàn thành hv
            decimal hVGhiDanhLaiNam = 0;
            decimal hVGhiDanhMoiNam = 0;
            decimal chiTieuHocVienNam = 0;
            decimal chiTieuCuocGoiNam = 0;
            decimal hoanThanhCuocGoiNam = 0;
            decimal saleOver100Nam = 0;
            decimal sale30To50Nam = 0;
            decimal sale20To30Nam = 0;
            decimal saleUnder20Nam = 0;
            decimal doanhThuNenNam = 0;
            decimal doanhThuHocBongNam = 0;
            decimal doanhThuVangNam = 0;
            decimal doanhThuSuKienNam = 0;
            var listMonth = new List<int>();
            var listReportCategoryId = new List<int> { 22, 23, 34, 35, 40, 43, 119, 31, 30, 58, 60, 26, 27, 64, 78, 80, 82, 84, 53, 54, 55, 56 };
            #endregion

            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = zones;
                if (model.ZoneId == null && model.OfficeId == null)
                {
                    model.Offices = model.Offices.ToList();
                    // Nếu chọn vùng - show các tháng đã từng quản lý / hoặc show all nếu đang quản lý
                    //var allOfficeIds = new HashSet<string>(model.Offices.Select(o => o.Id.ToString()));
                    var allOfficeIds = model.Offices.Select(o => o.Id).ToList();

                    var listReportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type1 && a.OfficeId.HasValue &&
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


                    listMonth.AddRange(Enumerable.Range(1, 12));

                    foreach (var month in listMonth)
                    {
                        decimal soSale = 0;
                        decimal dinhBien = 0;
                        decimal chiTieuDS = 0;
                        decimal thucDatDS = 0;
                        decimal hTDS = 0;
                        decimal DSSale = 0;
                        decimal DSDaoTao = 0;
                        decimal DSKeToan = 0;
                        decimal tiTrongSale = 0;
                        decimal tiTrongDaoTao = 0;
                        decimal tiTrongKeToan = 0;
                        decimal uuDaiBinhQuan = 0;
                        decimal hVGhiDanhLai = 0;
                        decimal hVGhiDanhMoi = 0;
                        decimal tileHVGDM = 0;
                        decimal tileHVGDL = 0;
                        decimal chiTieuCuocGoi = 0;
                        decimal chiTieuHocVien = 0;
                        decimal tongSoHocVien = 0; // hoàn thành
                        decimal hoanThanhCuocGoi = 0;
                        decimal thangChotBinhQuan = 0;
                        decimal hTCuocGoi = 0;
                        decimal hTHocVien = 0;
                        decimal saleOver100 = 0;
                        decimal sale30To50 = 0;
                        decimal sale20To30 = 0;
                        decimal saleUnder20 = 0;
                        decimal doanhThuNen = 0;
                        decimal doanhThuHocBong = 0;
                        decimal doanhThuVang = 0;
                        decimal doanhThuSuKien = 0;
                        decimal tiLeDoanhThuNen = 0;
                        decimal tiLeDoanhThuHocBong = 0;
                        decimal tiLeDoanhThuVang = 0;
                        decimal tiLeDoanhThuSuKien = 0;
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
                            thangChotBinhQuan += GetData(month, officeId, 64);

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
                        if (tongSoHocVienQuy1 > 0)
                        {
                            tileHVGDLQuy1 = (hVGhiDanhMoiQuy1 / tongSoHocVienQuy1) * 100;
                            tileHVGDMQuy1 = (hVGhiDanhLaiQuy1 / tongSoHocVienQuy1) * 100;
                        }
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
                        if (tongSoHocVienQuy2 > 0)
                        {
                            tileHVGDLQuy2 = (hVGhiDanhMoiQuy2 / tongSoHocVienQuy2) * 100;
                            tileHVGDMQuy2 = (hVGhiDanhLaiQuy2 / tongSoHocVienQuy2) * 100;
                        }
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
                        if (tongSoHocVienQuy3 > 0)
                        {
                            tileHVGDLQuy3 = (hVGhiDanhMoiQuy3 / tongSoHocVienQuy3) * 100;
                            tileHVGDMQuy3 = (hVGhiDanhLaiQuy3 / tongSoHocVienQuy3) * 100;
                        }
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
                        if (tongSoHocVienQuy4 > 0)
                        {
                            tileHVGDLQuy4 = (hVGhiDanhMoiQuy4 / tongSoHocVienQuy4) * 100;
                            tileHVGDMQuy4 = (hVGhiDanhLaiQuy4 / tongSoHocVienQuy4) * 100;
                        }
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
                        if (tongSoHocVienNam > 0)
                        {
                            tileHVGDLNam = (hVGhiDanhMoiNam / tongSoHocVienNam) * 100;
                            tileHVGDMNam = (hVGhiDanhLaiNam / tongSoHocVienNam) * 100;
                        }
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
            if (model.Zones.Count() == 1)
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
                    // Nếu chọn vùng - show các tháng đã từng quản lý / hoặc show all nếu đang quản lý
                    var allOfficeIds = model.Offices.Select(o => o.Id).ToList();

                    var listReportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type1 && a.OfficeId.HasValue &&
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

                    listMonth.OrderBy(a => a);
                    foreach (var month in listMonth)
                    {
                        decimal soSale = 0;
                        decimal dinhBien = 0;
                        decimal chiTieuDS = 0;
                        decimal thucDatDS = 0;
                        decimal hTDS = 0;
                        decimal DSSale = 0;
                        decimal DSDaoTao = 0;
                        decimal DSKeToan = 0;
                        decimal tiTrongSale = 0;
                        decimal tiTrongDaoTao = 0;
                        decimal tiTrongKeToan = 0;
                        decimal uuDaiBinhQuan = 0;
                        decimal hVGhiDanhLai = 0;
                        decimal hVGhiDanhMoi = 0;
                        decimal tileHVGDM = 0;
                        decimal tileHVGDL = 0;
                        decimal chiTieuCuocGoi = 0;
                        decimal chiTieuHocVien = 0;
                        decimal tongSoHocVien = 0; // hoàn thành
                        decimal hoanThanhCuocGoi = 0;
                        decimal thangChotBinhQuan = 0;
                        decimal hTCuocGoi = 0;
                        decimal hTHocVien = 0;
                        decimal saleOver100 = 0;
                        decimal sale30To50 = 0;
                        decimal sale20To30 = 0;
                        decimal saleUnder20 = 0;
                        decimal doanhThuNen = 0;
                        decimal doanhThuHocBong = 0;
                        decimal doanhThuVang = 0;
                        decimal doanhThuSuKien = 0;
                        decimal tiLeDoanhThuNen = 0;
                        decimal tiLeDoanhThuHocBong = 0;
                        decimal tiLeDoanhThuVang = 0;
                        decimal tiLeDoanhThuSuKien = 0;
                        var officeIds = historyOffices.Where(h => h.Month == month && h.ZoneId == model.ZoneId).Select(h => h.OfficeId).Distinct().ToList();
                        foreach (var officeId in officeIds)
                        {
                            // ====== CỘNG THEO THÁNG ======
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
                            thangChotBinhQuan += GetData(month, officeId, 64);
                            saleOver100 += GetData(month, officeId, 78);
                            sale30To50 += GetData(month, officeId, 80);
                            sale20To30 += GetData(month, officeId, 82);
                            saleUnder20 += GetData(month, officeId, 84);
                            doanhThuNen += GetData(month, officeId, 53);
                            doanhThuHocBong += GetData(month, officeId, 54);
                            doanhThuVang += GetData(month, officeId, 55);
                            doanhThuSuKien += GetData(month, officeId, 56);

                            // ====== CỘNG THEO NĂM ======
                            soSaleNam += GetData(month, officeId, 23);
                            dinhBienNam += GetData(month, officeId, 22);
                            chiTieuDSNam += GetData(month, officeId, 34);
                            thucDatDSNam += GetData(month, officeId, 35);
                            DSSaleNam += GetData(month, officeId, 40);
                            DSDaoTaoNam += GetData(month, officeId, 43);
                            DSKeToanNam += GetData(month, officeId, 119);
                            tongSoHocVienNam += GetData(month, officeId, 31);
                            chiTieuHocVienNam += GetData(month, officeId, 30);
                            hVGhiDanhLaiNam += GetData(month, officeId, 58);
                            hVGhiDanhMoiNam += GetData(month, officeId, 60);
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

                            // ====== QUÝ 1 ======
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
                                chiTieuHocVienQuy1 += GetData(month, officeId, 30);
                                hVGhiDanhLaiQuy1 += GetData(month, officeId, 58);
                                hVGhiDanhMoiQuy1 += GetData(month, officeId, 60);
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

                            // ====== QUÝ 2 ======
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
                                chiTieuHocVienQuy2 += GetData(month, officeId, 30);
                                hVGhiDanhLaiQuy2 += GetData(month, officeId, 58);
                                hVGhiDanhMoiQuy2 += GetData(month, officeId, 60);
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

                            // ====== QUÝ 3 ======
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
                                chiTieuHocVienQuy3 += GetData(month, officeId, 30);
                                hVGhiDanhLaiQuy3 += GetData(month, officeId, 58);
                                hVGhiDanhMoiQuy3 += GetData(month, officeId, 60);
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

                            // ====== QUÝ 4 ======
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
                                chiTieuHocVienQuy4 += GetData(month, officeId, 30);
                                hVGhiDanhLaiQuy4 += GetData(month, officeId, 58);
                                hVGhiDanhMoiQuy4 += GetData(month, officeId, 60);
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
                        if (tongSoHocVienQuy1 > 0)
                        {
                            tileHVGDLQuy1 = (hVGhiDanhMoiQuy1 / tongSoHocVienQuy1) * 100;
                            tileHVGDMQuy1 = (hVGhiDanhLaiQuy1 / tongSoHocVienQuy1) * 100;
                        }
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
                        if (tongSoHocVienQuy2 > 0)
                        {
                            tileHVGDLQuy2 = (hVGhiDanhMoiQuy2 / tongSoHocVienQuy2) * 100;
                            tileHVGDMQuy2 = (hVGhiDanhLaiQuy2 / tongSoHocVienQuy2) * 100;
                        }
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
                        if (tongSoHocVienQuy3 > 0)
                        {
                            tileHVGDLQuy3 = (hVGhiDanhMoiQuy3 / tongSoHocVienQuy3) * 100;
                            tileHVGDMQuy3 = (hVGhiDanhLaiQuy3 / tongSoHocVienQuy3) * 100;
                        }
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
                        if (tongSoHocVienQuy4 > 0)
                        {
                            tileHVGDLQuy4 = (hVGhiDanhMoiQuy4 / tongSoHocVienQuy4) * 100;
                            tileHVGDMQuy4 = (hVGhiDanhLaiQuy4 / tongSoHocVienQuy4) * 100;
                        }
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
                        if (tongSoHocVienNam > 0)
                        {
                            tileHVGDLNam = (hVGhiDanhMoiNam / tongSoHocVienNam) * 100;
                            tileHVGDMNam = (hVGhiDanhLaiNam / tongSoHocVienNam) * 100;
                        }
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
                var hOffices = historyOffices.Where(a => a.OfficeId == model.OfficeId);

                var listReportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type1 && model.OfficeId == a.OfficeId &&
                listReportCategoryId.Contains(a.ReportCategoryId)).AsNoTracking().ToList();

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

                listMonth.OrderBy(a => a);
                foreach (var month in listMonth)
                {

                    decimal soSale = 0;
                    decimal dinhBien = 0;
                    decimal chiTieuDS = 0;
                    decimal thucDatDS = 0;
                    decimal hTDS = 0;
                    decimal DSSale = 0;
                    decimal DSDaoTao = 0;
                    decimal DSKeToan = 0;
                    decimal tiTrongSale = 0;
                    decimal tiTrongDaoTao = 0;
                    decimal tiTrongKeToan = 0;
                    decimal uuDaiBinhQuan = 0;
                    decimal hVGhiDanhLai = 0;
                    decimal hVGhiDanhMoi = 0;
                    decimal tileHVGDM = 0;
                    decimal tileHVGDL = 0;
                    decimal chiTieuCuocGoi = 0;
                    decimal chiTieuHocVien = 0;
                    decimal tongSoHocVien = 0; // hoàn thành
                    decimal hoanThanhCuocGoi = 0;
                    decimal thangChotBinhQuan = 0;
                    decimal hTCuocGoi = 0;
                    decimal hTHocVien = 0;
                    decimal saleOver100 = 0;
                    decimal sale30To50 = 0;
                    decimal sale20To30 = 0;
                    decimal saleUnder20 = 0;
                    decimal doanhThuNen = 0;
                    decimal doanhThuHocBong = 0;
                    decimal doanhThuVang = 0;
                    decimal doanhThuSuKien = 0;
                    decimal tiLeDoanhThuNen = 0;
                    decimal tiLeDoanhThuHocBong = 0;
                    decimal tiLeDoanhThuVang = 0;
                    decimal tiLeDoanhThuSuKien = 0;
                    soSale = GetData(month, office.Id, 23);
                    dinhBien = GetData(month, office.Id, 22);
                    chiTieuDS = GetData(month, office.Id, 34);
                    thucDatDS = GetData(month, office.Id, 35);
                    DSSale = GetData(month, office.Id, 40);
                    DSDaoTao = GetData(month, office.Id, 43);
                    DSKeToan = GetData(month, office.Id, 119);
                    tongSoHocVien = GetData(month, office.Id, 31);
                    chiTieuHocVien = GetData(month, office.Id, 30);
                    hVGhiDanhLai = GetData(month, office.Id, 58);
                    hVGhiDanhMoi = GetData(month, office.Id, 60);
                    chiTieuCuocGoi = GetData(month, office.Id, 26);
                    hoanThanhCuocGoi = GetData(month, office.Id, 27);
                    thangChotBinhQuan = GetData(month, office.Id, 64);
                    saleOver100 = GetData(month, office.Id, 78);
                    sale30To50 = GetData(month, office.Id, 80);
                    sale20To30 = GetData(month, office.Id, 82);
                    saleUnder20 = GetData(month, office.Id, 84);
                    doanhThuNen = GetData(month, office.Id, 53);
                    doanhThuHocBong = GetData(month, office.Id, 54);
                    doanhThuVang = GetData(month, office.Id, 55);
                    doanhThuSuKien = GetData(month, office.Id, 56);

                    // Cộng dồn theo năm
                    soSaleNam += soSale;
                    dinhBienNam += dinhBien;
                    chiTieuDSNam += chiTieuDS;
                    thucDatDSNam += thucDatDS;
                    DSSaleNam += DSSale;
                    DSDaoTaoNam += DSDaoTao;
                    DSKeToanNam += DSKeToan;
                    tongSoHocVienNam += tongSoHocVien;
                    hVGhiDanhLaiNam += hVGhiDanhLai;
                    hVGhiDanhMoiNam += hVGhiDanhMoi;
                    chiTieuHocVienNam += chiTieuHocVien;
                    chiTieuCuocGoiNam += chiTieuCuocGoi;
                    hoanThanhCuocGoiNam += hoanThanhCuocGoi;
                    saleOver100Nam += saleOver100;
                    sale30To50Nam += sale30To50;
                    sale20To30Nam += sale20To30;
                    saleUnder20Nam += saleUnder20;
                    doanhThuNenNam += doanhThuNen;
                    doanhThuHocBongNam += doanhThuHocBong;
                    doanhThuVangNam += doanhThuVang;
                    doanhThuSuKienNam += doanhThuSuKien;

                    // Cộng dồn theo quý
                    if (month >= 1 && month <= 3)
                    {
                        soSaleQuy1 += soSale;
                        dinhBienQuy1 += dinhBien;
                        chiTieuDSQuy1 += chiTieuDS;
                        thucDatDSQuy1 += thucDatDS;
                        DSSaleQuy1 += DSSale;
                        DSDaoTaoQuy1 += DSDaoTao;
                        DSKeToanQuy1 += DSKeToan;
                        tongSoHocVienQuy1 += tongSoHocVien;
                        hVGhiDanhLaiQuy1 += hVGhiDanhLai;
                        hVGhiDanhMoiQuy1 += hVGhiDanhMoi;
                        chiTieuHocVienQuy1 += chiTieuHocVien;
                        chiTieuCuocGoiQuy1 += chiTieuCuocGoi;
                        hoanThanhCuocGoiQuy1 += hoanThanhCuocGoi;
                        saleOver100Quy1 += saleOver100;
                        sale30To50Quy1 += sale30To50;
                        sale20To30Quy1 += sale20To30;
                        saleUnder20Quy1 += saleUnder20;
                        doanhThuNenQuy1 += doanhThuNen;
                        doanhThuHocBongQuy1 += doanhThuHocBong;
                        doanhThuVangQuy1 += doanhThuVang;
                        doanhThuSuKienQuy1 += doanhThuSuKien;
                    }
                    else if (month >= 4 && month <= 6)
                    {
                        soSaleQuy2 += soSale;
                        dinhBienQuy2 += dinhBien;
                        chiTieuDSQuy2 += chiTieuDS;
                        thucDatDSQuy2 += thucDatDS;
                        DSSaleQuy2 += DSSale;
                        DSDaoTaoQuy2 += DSDaoTao;
                        DSKeToanQuy2 += DSKeToan;
                        tongSoHocVienQuy2 += tongSoHocVien;
                        hVGhiDanhLaiQuy2 += hVGhiDanhLai;
                        hVGhiDanhMoiQuy2 += hVGhiDanhMoi;
                        chiTieuHocVienQuy2 += chiTieuHocVien;
                        chiTieuCuocGoiQuy2 += chiTieuCuocGoi;
                        hoanThanhCuocGoiQuy2 += hoanThanhCuocGoi;
                        saleOver100Quy2 += saleOver100;
                        sale30To50Quy2 += sale30To50;
                        sale20To30Quy2 += sale20To30;
                        saleUnder20Quy2 += saleUnder20;
                        doanhThuNenQuy2 += doanhThuNen;
                        doanhThuHocBongQuy2 += doanhThuHocBong;
                        doanhThuVangQuy2 += doanhThuVang;
                        doanhThuSuKienQuy2 += doanhThuSuKien;
                    }
                    else if (month >= 7 && month <= 9)
                    {
                        soSaleQuy3 += soSale;
                        dinhBienQuy3 += dinhBien;
                        chiTieuDSQuy3 += chiTieuDS;
                        thucDatDSQuy3 += thucDatDS;
                        DSSaleQuy3 += DSSale;
                        DSDaoTaoQuy3 += DSDaoTao;
                        DSKeToanQuy3 += DSKeToan;
                        tongSoHocVienQuy3 += tongSoHocVien;
                        hVGhiDanhLaiQuy3 += hVGhiDanhLai;
                        hVGhiDanhMoiQuy3 += hVGhiDanhMoi;
                        chiTieuHocVienQuy3 += chiTieuHocVien;
                        chiTieuCuocGoiQuy3 += chiTieuCuocGoi;
                        hoanThanhCuocGoiQuy3 += hoanThanhCuocGoi;
                        saleOver100Quy3 += saleOver100;
                        sale30To50Quy3 += sale30To50;
                        sale20To30Quy3 += sale20To30;
                        saleUnder20Quy3 += saleUnder20;
                        doanhThuNenQuy3 += doanhThuNen;
                        doanhThuHocBongQuy3 += doanhThuHocBong;
                        doanhThuVangQuy3 += doanhThuVang;
                        doanhThuSuKienQuy3 += doanhThuSuKien;
                    }
                    else // 10-12
                    {
                        soSaleQuy4 += soSale;
                        dinhBienQuy4 += dinhBien;
                        chiTieuDSQuy4 += chiTieuDS;
                        thucDatDSQuy4 += thucDatDS;
                        DSSaleQuy4 += DSSale;
                        DSDaoTaoQuy4 += DSDaoTao;
                        DSKeToanQuy4 += DSKeToan;
                        tongSoHocVienQuy4 += tongSoHocVien;
                        hVGhiDanhLaiQuy4 += hVGhiDanhLai;
                        hVGhiDanhMoiQuy4 += hVGhiDanhMoi;
                        chiTieuHocVienQuy4 += chiTieuHocVien;
                        chiTieuCuocGoiQuy4 += chiTieuCuocGoi;
                        hoanThanhCuocGoiQuy4 += hoanThanhCuocGoi;
                        saleOver100Quy4 += saleOver100;
                        sale30To50Quy4 += sale30To50;
                        sale20To30Quy4 += sale20To30;
                        saleUnder20Quy4 += saleUnder20;
                        doanhThuNenQuy4 += doanhThuNen;
                        doanhThuHocBongQuy4 += doanhThuHocBong;
                        doanhThuVangQuy4 += doanhThuVang;
                        doanhThuSuKienQuy4 += doanhThuSuKien;
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
                    if (tongSoHocVienQuy1 > 0)
                    {
                        tileHVGDLQuy1 = (hVGhiDanhMoiQuy1 / tongSoHocVienQuy1) * 100;
                        tileHVGDMQuy1 = (hVGhiDanhLaiQuy1 / tongSoHocVienQuy1) * 100;
                    }
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
                    if (tongSoHocVienQuy2 > 0)
                    {
                        tileHVGDLQuy2 = (hVGhiDanhMoiQuy2 / tongSoHocVienQuy2) * 100;
                        tileHVGDMQuy2 = (hVGhiDanhLaiQuy2 / tongSoHocVienQuy2) * 100;
                    }
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
                    if (tongSoHocVienQuy3 > 0)
                    {
                        tileHVGDLQuy3 = (hVGhiDanhMoiQuy3 / tongSoHocVienQuy3) * 100;
                        tileHVGDMQuy3 = (hVGhiDanhLaiQuy3 / tongSoHocVienQuy3) * 100;
                    }
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
                    if (tongSoHocVienQuy4 > 0)
                    {
                        tileHVGDLQuy4 = (hVGhiDanhMoiQuy4 / tongSoHocVienQuy4) * 100;
                        tileHVGDMQuy4 = (hVGhiDanhLaiQuy4 / tongSoHocVienQuy4) * 100;
                    }
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
                    if (tongSoHocVienNam > 0)
                    {
                        tileHVGDLNam = (hVGhiDanhMoiNam / tongSoHocVienNam) * 100;
                        tileHVGDMNam = (hVGhiDanhLaiNam / tongSoHocVienNam) * 100;
                    }
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
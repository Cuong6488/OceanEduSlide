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
                        decimal val;
                        var cleaned = r.Data?.Replace(".", "").Replace(",", "") ?? "0";
                        return decimal.TryParse(cleaned, out val) ? val : 0;
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

        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
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
            && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.OfficeId != null);

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
                                historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
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
                historyQuery = historyQuery.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId));
            }

            if (model.OfficeId != null)
            {
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
        public ActionResult ReportCall(int? page, int? ZoneId, int? OfficeId, string startDay, string endDay)
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            var pageNumber = page ?? 1;

            if (string.IsNullOrEmpty(startDay))
                startDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            if (string.IsNullOrEmpty(endDay))
                endDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
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
            var model = new ListCallViewModel
            {
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
                StartDay = startDay,
                EndDay = endDay
            };
            // Đã yêu cầu người dùng phải chọn khoảng thời gian trong 1 năm
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month >= StartDate.Month && h.Month <= EndDate.Month && h.Year == StartDate.Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                }
            }
            else
            {
                //model.ZoneId = User.ZoneId;
                //if (User.TypeUser == TypeUser.ASM)
                //    model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                //else
                //    model.OfficeId = User.OfficeId;
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                        //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
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
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                        if (model.Offices.Count() == 1)
                            model.OfficeId = model.Offices.First().Id;
                    }
                }
            }

            if (model.ZoneId != null)
            {
                //model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));

            }
            if (model.OfficeId != null && !string.IsNullOrEmpty(startDay) && !string.IsNullOrEmpty(endDay))
            {

                var startDate = StartDate.Date;
                var endDate = EndDate.Date.AddDays(1);
                var callData = _unitOfWork.CallLogRepository.GetQuery(p => p.CallDate >= startDate && p.CallDate < endDate && p.HistoryUser.OfficeId == model.OfficeId);
                var aggregated = callData.GroupBy(p => p.HistoryUserId).Select(g => new
                {
                    HistoryUserId = g.Key,
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
                var khoang = EndDate.Month - StartDate.Month;
                List<int> months = new List<int>();
                if(khoang >= 0)
                {
                    for(int i = StartDate.Month;i<= EndDate.Month; i++)
                    {
                        months.Add(i);
                    }
                }
                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && months.Contains(a.Month) && a.OfficeId == model.OfficeId && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd >= startDate)) && a.DayStart <= endDate
                && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.BM, q => q.OrderBy(a => a.Sort).ThenBy(a => a.UserId).ThenBy(a => a.Month)).ToList();
                var userItems = historyUsers.Select(u =>
                {
                    var match = aggregated.FirstOrDefault(x => x.HistoryUserId == u.Id);

                    return new ListCallViewModel.UserItem
                    {
                        //User = u,
                        HistoryUser = u,
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
            await service.SyncYesterdayAsync();
            return Content("Đã đồng bộ thủ công.");
        }
        public async Task<ActionResult> SyncCustom(int month, int day)
        {
            var service = new CallLogService();
            await service.SyncCusTom(month, day);
            return Content("Đã đồng bộ 7 ngày. " + day+" - " + month);
        }

        #endregion
    }
}
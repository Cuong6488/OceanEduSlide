using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Z.EntityFramework.Plus;
using OceanEduSlide.EnumHelpers;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    [ForcePasswordChangeFilter]
    public class EventController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        #region Sự_Kiện
        public ActionResult Index(int? ZoneId, int? Month, int? OfficeId, int? Year, int? Week, int? UserType, int? TypeView, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(Year ?? DateTime.Now.Year, Month ?? DateTime.Now.Month, DateTime.Now);
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.Year = DateTime.Now.Year;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek(Week ?? currentWeek, Year ?? DateTime.Now.Year, Month ?? DateTime.Now.Month);
            ViewBag.Result = Result;
            var model = new EventViewModel
            {
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                Week = Week ?? currentWeek,
                OfficeId = OfficeId,
                ZoneId = ZoneId,
                UserType = UserType,
                User = User,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId))
            };
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == model.Month && h.Year == model.Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            var events = _unitOfWork.EventRepository.GetQuery(a => a.Month == model.Month && a.Year == model.Year && (int)a.WeekNumber == model.Week);
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    if (model.OfficeId == null)
                        events = events.Where(a => a.Office.ZoneId != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ","));
                }
            }
            else
            {
                //model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            if (model.OfficeId == null)
                                events = events.Where(a => User.Zone.OfficeIds.Contains("," + a.OfficeId.ToString() + ","));
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
                        if (model.OfficeId == null)
                            events = events.Where(a => User.OfficeIds.Contains("," + a.OfficeId.ToString() + ","));
                    }
                }

            }
            if (model.ZoneId != null)
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
            if (model.Offices.Count() == 1)
                model.OfficeId = model.Offices.First().Id;
            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                {
                    var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == model.OfficeId);
                    var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.OfficeId == model.OfficeId && a.Year == model.Year && a.Month == model.Month
                    && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == model.Month) || a.DayEnd.Value.Month != model.Month))), q => q.OrderBy(a => a.Sort));

                    if (UserType != null)
                    {
                        users = users.Where(a => (int)a.TypeUser == UserType);
                        historyUsers = historyUsers.Where(a => (int)a.TypeUser == UserType);
                    }
                    else
                    {
                        users = users.Where(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM);
                        historyUsers = historyUsers.Where(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM);
                    }

                    var userItems = historyUsers.ToList().Select(a => new EventViewModel.UserItem
                    {
                        HistoryUser = a,
                        Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(p => p.HistoryUserId == a.Id && p.UserId == a.UserId && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week, q => q.OrderByDescending(p => p.CreateDate)),
                        RevenueUser_Week = _unitOfWork.RevenueUser_WeekRepository.GetQuery(p => p.HistoryUserId == a.Id && p.UserId == a.UserId && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week, q => q.OrderByDescending(p => p.CreateDate)).FirstOrDefault(),
                    });
                    model.Users = users;
                    model.UserItems = userItems;
                    model.Events = events.OrderByDescending(p => p.CreateDate).Where(a => a.OfficeId == model.OfficeId);
                    model.Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(p => p.TargetBM != 0 && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week && p.User.OfficeId == model.OfficeId, q => q.OrderByDescending(p => p.CreateDate));
                }
                return View(model);
            }
            //var latestEventsPerGroup = events.GroupBy(e => new { e.Month, e.Year, e.WeekNumber, e.OfficeId, e.DayofWeek })
            //    .Select(g => g.OrderByDescending(e => e.CreateDate).FirstOrDefault());
            //model.Events = latestEventsPerGroup;
            // Chọn bản ghi mới nhất cho mỗi nhóm
            if (TypeView >= 1 && TypeView <= 4)
            {
                events = events.Where(a => (int)a.TypeEvent == TypeView);
            }
            if (TypeView != 5)
            {
                var latestEventsPerGroup = events
                    .GroupBy(e => new { e.OfficeId, e.DayofWeek, e.TypeEvent }) // Group theo key phù hợp
                    .Select(g => g.OrderByDescending(e => e.CreateDate).FirstOrDefault()) // Lấy bản ghi mới nhất
                    .ToList();

                model.Events = latestEventsPerGroup;
                var eventDict = model.Events
        .GroupBy(e => $"{e.OfficeId}_{(int)e.DayofWeek}_{(int)e.TypeEvent}")
        .ToDictionary(g => g.Key, g => g.First());

                ViewBag.EventDict = eventDict;
            }
            model.TypeView = TypeView;
            var allRevenues = GetAllRevenues(model.Week ?? 0, model.Month ?? 0, model.Year ?? 0);
            ViewBag.AllRevenues = allRevenues;
            return View("EventManager", model);
        }

        public void ExportEvent(int Year, int Month, int Week, int? OfficeId, int? ZoneId, int? TypeView)
        {
            var events = _unitOfWork.EventRepository.GetQuery(a => a.Year == Year && a.Month == Month && (int)a.WeekNumber == Week);
            var offices1 = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId));
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == Month && h.Year == Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            if (User.TypeUser == TypeUser.HO)
            {

            }
            else if (User.TypeUser == TypeUser.CV)
            {
                if (ZoneId == null)
                {
                    offices1 = offices1.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));

                    if (OfficeId == null)
                        events = events.Where(a => a.Office.ZoneId != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ","));
                }
            }
            else
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        if (ZoneId == null)
                        {
                            offices1 = offices1.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            if (OfficeId == null)
                                events = events.Where(a => User.Zone.OfficeIds.Contains("," + a.OfficeId.ToString() + ","));
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
                        OfficeId = User.OfficeId;
                    else
                    {
                        if (OfficeId == null)
                        {
                            events = events.Where(a => User.OfficeIds.Contains("," + a.OfficeId.ToString() + ","));
                            offices1 = offices1.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId + ",")));
                        }
                    }
                }

            }
            if (ZoneId != null && OfficeId == null)
            {
                offices1 = offices1.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == ZoneId));
                events = events.Where(a => a.Office.ZoneId == ZoneId);

            }
            else if (OfficeId != null)
            {
                events = events.Where(a => a.OfficeId == OfficeId);
                offices1 = offices1.Where(a => a.Id == OfficeId);
            }
            //var latestEventsPerGroup = events.GroupBy(e => new { e.Month, e.Year, e.WeekNumber, e.OfficeId, e.DayofWeek })
            //    .Select(g => g.OrderByDescending(e => e.CreateDate).FirstOrDefault());
            //model.Events = latestEventsPerGroup;
            // Chọn bản ghi mới nhất cho mỗi nhóm
            if (TypeView >= 1 && TypeView <= 4)
            {
                events = events.Where(a => (int)a.TypeEvent == TypeView);
            }
            // Lấy toàn bộ sự kiện thỏa điều kiện, group theo OfficeId + WeekNumber + DayofWeek
            var eventsList = events.AsNoTracking().GroupBy(a => new { a.OfficeId, a.WeekNumber, a.DayofWeek }).Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault()).ToList();
            // Dictionary tra cứu nhanh: OfficeId -> WeekEnum -> DayEnum
            //var eventDict = eventsList
            //    .GroupBy(e => e.OfficeId)
            //    .ToDictionary(
            //        g => g.Key,
            //        g => g.GroupBy(e => e.WeekNumber)
            //              .ToDictionary(
            //                  wg => wg.Key,
            //                  wg => wg.ToDictionary(e => e.DayofWeek, e => e)
            //              )
            //    );
            var eventDict = events
                    .GroupBy(e => e.OfficeId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(e => e.WeekNumber)
                              .ToDictionary(
                                  wg => wg.Key,
                                  wg => wg.GroupBy(e => e.DayofWeek)
                                          .ToDictionary(
                                              dg => dg.Key,
                                              dg => dg.GroupBy(e => e.TypeEvent) // Mỗi loại hoạt động chỉ lấy bản mới nhất
                                                    .Select(k => k.OrderByDescending(e => e.CreateDate).First())
                                                    .ToList()
                                          )
                              )
                    );

            var offices = offices1.AsNoTracking().ToList();

            var dt = new DataTable();
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Tuần");

            for (int d = 2; d <= 8; d++) // Thứ 2 đến Chủ nhật
            {
                dt.Columns.Add(d == 8 ? "Chủ nhật" : $"Thứ {d}");
            }

            foreach (var office in offices)
            {
                var row = dt.NewRow();
                row["Chi nhánh"] = office.ShortName;
                row["Vùng"] = office.Zone?.Name;
                row["Tháng"] = Month.ToString();
                row["Tuần"] = Week;

                // Ép kiểu từ int sang enum (WeekEnum và DayEnum là enum thực tế bạn đang dùng)
                var weekEnum = (WeekNumber)Week;

                //for (int j = 2; j <= 8; j++)
                //{
                //    var dayEnum = (DayofWeek)j;

                //    if (eventDict.TryGetValue(office.Id, out var weekDict) &&
                //        weekDict.TryGetValue(weekEnum, out var dayDict) &&
                //        dayDict.TryGetValue(dayEnum, out var eventDay))
                //    {
                //        var eventInfo = string.Join("\n", new[]
                //        {
                //        $"Loại hoạt động: {EnumExtensions.GetDisplayName(eventDay.TypeEvent)}",
                //        $"Tên hoạt động: {eventDay.Name}",
                //        $"Đối tượng tham gia: {EnumExtensions.GetDisplayName(eventDay.TypeJoin)}",
                //        $"Lứa tuổi: {eventDay.Ages}",
                //        $"Thời gian: {eventDay.TimeFrom} - {eventDay.TimeTo}",
                //    });

                //        var columnName = j == 8 ? "Chủ nhật" : $"Thứ {j}";
                //        row[columnName] = eventInfo.Trim();
                //    }
                //}
                for (int j = 2; j <= 8; j++)
                {
                    var dayEnum = (DayofWeek)j;

                    if (eventDict.TryGetValue(office.Id, out var weekDict) &&
                        weekDict.TryGetValue(weekEnum, out var dayDict) &&
                        dayDict.TryGetValue(dayEnum, out var eventList) && eventList.Any())
                    {
                        // Gộp thông tin các sự kiện trong ngày đó (mỗi loại 1 bản mới nhất)
                        var eventInfoList = eventList.Select(eventDay => string.Join("\n", new[]
                        {
            $"Loại hoạt động: {EnumExtensions.GetDisplayName(eventDay.TypeEvent)}",
            $"Tên hoạt động: {eventDay.Name}",
            $"Đối tượng tham gia: {EnumExtensions.GetDisplayName(eventDay.TypeJoin)}",
            $"Lứa tuổi: {eventDay.Ages}",
            $"Thời gian: {eventDay.TimeFrom} - {eventDay.TimeTo}"
        }));

                        var columnName = j == 8 ? "Chủ nhật" : $"Thứ {j}";
                        row[columnName] = string.Join("\n\n---\n\n", eventInfoList); // Ngăn cách giữa các sự kiện
                    }
                }

                dt.Rows.Add(row);

            }

            var filename = $"danh-sach-su-kien.xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Danh sách sự kiện");

                ws.Cells["A1"].LoadFromDataTable(dt, true);

                if (ws.Dimension != null)
                {
                    ws.Cells[ws.Dimension.Address].Style.WrapText = true;
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public List<RevenueUser_DayOfWeek> GetAllRevenues(int week, int month, int year)
        {
            var query = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a =>
                a.HistoryUserId != null && a.TargetBM != null &&
                a.Month == month &&
                a.Year == year &&
                (int)a.WeekNumber == week);

            // Lấy bản ghi mới nhất theo CreateDate cho từng HistoryUserId, DayOfWeek
            var grouped = query
                .GroupBy(a => new
                {
                    a.Year,
                    a.Month,
                    a.HistoryUserId,
                    a.WeekNumber,
                    a.DayofWeek
                })
                .Select(g => g.OrderByDescending(x => x.CreateDate).FirstOrDefault())
                .ToList();

            return grouped;

        }

        public ActionResult UpdatePercent(int userId)
        {

            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return HttpNotFound();
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
                return RedirectToAction("Index");
            var model = new UpdatePercentViewModel { UserId = userId, Fullname = user.Fullname ?? user.Username, CF1 = user.Confirm1, CF2 = user.Confirm2, CF3 = user.Confirm3, CI = user.CI, DT = user.DT, RevenueAverage = user.RevenueAverage.ToString("N0") };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdatePercent(UpdatePercentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _unitOfWork.UserRepository.GetById(model.UserId);
                if (user == null)
                {
                    return RedirectToAction("Index");
                }
                user.RevenueAverage = Convert.ToDecimal(model.RevenueAverage.Replace(",", ""));
                user.Confirm1 = model.CF1;
                user.Confirm2 = model.CF2;
                user.Confirm3 = model.CF3;
                user.CI = model.CI;
                user.DT = model.DT;
                _unitOfWork.Save();
                return RedirectToAction("Index", new { Result = "add" });
            }
            return View(model);
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueDay(int year, int month, int week, int historyUserId, decimal? targetBM, int dayOfWeek)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            if (historyUser == null)
                return Json(new { status = false, msg = "Cập nhật thất bại" });

            if (historyUser.User.DT > 0 && historyUser.User.CI > 0 && historyUser.User.Confirm2 > 0 && historyUser.User.Confirm1 > 0 && historyUser.User.RevenueAverage > 0)
            {
                var newRevenue = new RevenueUser_DayOfWeek
                {
                    TargetBM = targetBM ?? 0,
                    Active = true,
                    Year = year,
                    Month = month,
                    UserId = historyUser.UserId,
                    HistoryUserId = historyUserId
                };
                switch (week)
                {
                    case 1:
                        newRevenue.WeekNumber = WeekNumber.Week1;
                        break;
                    case 2:
                        newRevenue.WeekNumber = WeekNumber.Week2;
                        break;
                    case 3:
                        newRevenue.WeekNumber = WeekNumber.Week3;
                        break;
                    case 4:
                        newRevenue.WeekNumber = WeekNumber.Week4;
                        break;
                    case 5:
                        newRevenue.WeekNumber = WeekNumber.Week5;
                        break;
                    case 6:
                        newRevenue.WeekNumber = WeekNumber.Week6;
                        break;
                    default:
                        break;
                }
                switch (dayOfWeek)
                {
                    case 2:
                        newRevenue.DayofWeek = DayofWeek.Monday;
                        break;
                    case 3:
                        newRevenue.DayofWeek = DayofWeek.Tuesday;
                        break;
                    case 4:
                        newRevenue.DayofWeek = DayofWeek.Wednessday;
                        break;
                    case 5:
                        newRevenue.DayofWeek = DayofWeek.Thursday;
                        break;
                    case 6:
                        newRevenue.DayofWeek = DayofWeek.Friday;
                        break;
                    case 7:
                        newRevenue.DayofWeek = DayofWeek.Saturday;
                        break;
                    case 8:
                        newRevenue.DayofWeek = DayofWeek.Sunday;
                        break;
                    default:
                        break;
                }
                _unitOfWork.RevenueUser_DayOfWeekRepository.Insert(newRevenue);
                _unitOfWork.Save();
                return Json(new { status = true/*, msg = "Cập nhật thành công" */});
            }
            return Json(new { status = false, msg = "Chưa cập nhật các tỉ lệ chuyển đổi cho người dùng này" });

        }

        [HttpPost]
        public JsonResult AddOrUpdateRevenueDay2(int year, int month, int week, int historyUserId, int dayOfWeek, decimal? targetBM_DT)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            if (historyUser == null)
                return Json(new { status = false, msg = "Cập nhật thất bại" });
            var user = historyUser.User;
            if (historyUser.User.DT > 0 && historyUser.User.CI > 0 && historyUser.User.Confirm2 > 0 && historyUser.User.Confirm1 > 0 && historyUser.User.RevenueAverage > 0)
            {
                //var ev = _unitOfWork.EventRepository.GetQuery(a => a.Year == year && a.Month == month && (int)a.WeekNumber == week && (int)a.DayofWeek == dayOfWeek, q => q.OrderByDescending(a => a.CreateDate)).FirstOrDefault();
                var evs = _unitOfWork.EventRepository.GetQuery(a => a.Year == year && a.Month == month && (int)a.WeekNumber == week && (int)a.DayofWeek == dayOfWeek && a.OfficeId == historyUser.OfficeId && (a.TypeEvent == TypeEvent.SKDT || a.TypeEvent == TypeEvent.SKSale), q => q.OrderByDescending(a => a.CreateDate));
                if (evs.Any())
                {
                    foreach (var item in evs.Skip(1))
                    {
                        var rvns = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.EventId == item.Id && a.HistoryUserId == historyUserId);
                        rvns.Delete();
                    }
                    _unitOfWork.Save();
                }
                var ev = evs.FirstOrDefault();
                if (ev != null && ev.UserIds.Contains("," + historyUser.UserId.ToString() + ","))
                {
                    List<int> days = ev.Days.Split(',').Select(int.Parse).OrderBy(x => x).ToList();

                    var dt = targetBM_DT ?? 0;
                    var ci = Math.Round(dt / historyUser.User.DT * 100);
                    var cf3 = Math.Round(ci / historyUser.User.CI * 100);
                    var cf2 = cf3;
                    if (historyUser.User.Confirm3 > 0)
                        cf2 = Math.Round(cf3 / historyUser.User.Confirm3 * 100);
                    var cf1 = Math.Round(cf2 / historyUser.User.Confirm2 * 100);
                    var dataQuantity = Math.Round(cf1 / historyUser.User.Confirm1 * 100);
                    decimal dataDay = 0, cf1Day = 0, cf2Day = 0;
                    if (days.Count > 1)
                    {
                        dataDay = Math.Round(dataQuantity / (days.Count() - 1));
                        cf1Day = Math.Round(cf1 / (days.Count() - 1));
                        cf2Day = Math.Round(cf2 / 2);
                    }

                    int i = 1;
                    foreach (var day in days)
                    {
                        var revenue = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.EventId == ev.Id && (int)a.DayofWeek == day && a.HistoryUserId == historyUserId).FirstOrDefault();
                        if (revenue == null)
                        {
                            revenue = new RevenueUser_DayOfWeek
                            {
                                Year = year,
                                Month = month,
                                UserId = historyUser.UserId,
                                HistoryUserId = historyUserId,
                                EventId = ev.Id,
                                Event = ev,
                                //TargetBM = targetBM,
                                Active = true,
                            };
                            switch (week)
                            {
                                case 1:
                                    revenue.WeekNumber = WeekNumber.Week1;
                                    break;
                                case 2:
                                    revenue.WeekNumber = WeekNumber.Week2;
                                    break;
                                case 3:
                                    revenue.WeekNumber = WeekNumber.Week3;
                                    break;
                                case 4:
                                    revenue.WeekNumber = WeekNumber.Week4;
                                    break;
                                case 5:
                                    revenue.WeekNumber = WeekNumber.Week5;
                                    break;
                                case 6:
                                    revenue.WeekNumber = WeekNumber.Week6;
                                    break;
                                default:
                                    break;
                            }
                            switch (day)
                            {
                                case 2:
                                    revenue.DayofWeek = DayofWeek.Monday;
                                    break;
                                case 3:
                                    revenue.DayofWeek = DayofWeek.Tuesday;
                                    break;
                                case 4:
                                    revenue.DayofWeek = DayofWeek.Wednessday;
                                    break;
                                case 5:
                                    revenue.DayofWeek = DayofWeek.Thursday;
                                    break;
                                case 6:
                                    revenue.DayofWeek = DayofWeek.Friday;
                                    break;
                                case 7:
                                    revenue.DayofWeek = DayofWeek.Saturday;
                                    break;
                                case 8:
                                    revenue.DayofWeek = DayofWeek.Sunday;
                                    break;
                                default:
                                    break;
                            }

                            _unitOfWork.RevenueUser_DayOfWeekRepository.Insert(revenue);
                        }
                        if (days.Count() > 1)
                        {
                            if (i < days.Count())
                            {
                                revenue.DataQuantity = dataDay;
                                revenue.Confirm1 = cf1Day;
                            }
                            if (i >= days.Count() - 1)
                            {
                                revenue.Confirm2 = cf2Day;
                                if (i == days.Count())
                                {
                                    revenue.DT = dt;
                                    revenue.CI = ci;
                                }

                            }
                            i++;
                        }
                        else
                        {
                            revenue.DataQuantity = dataQuantity;
                            revenue.Confirm1 = cf1;
                            revenue.Confirm2 = cf2;
                            revenue.DT = dt;
                            revenue.CI = ci;
                        }
                    }
                    _unitOfWork.Save();
                    var listrevenue = _unitOfWork.RevenueUser_DayOfWeekRepository
                                        .GetQuery(a => a.User.OfficeId == User.OfficeId && a.Year == year && a.Month == month && (int)a.WeekNumber == week && (int)a.DayofWeek == dayOfWeek && a.DT != null, q => q.OrderByDescending(a => a.CreateDate))
                                        .GroupBy(a => a.UserId).Select(g => g.FirstOrDefault()).ToList();
                    ev.RangeStudent = 0;
                    ev.RangeNewCustomer = 0;
                    ev.Range = 0;
                    foreach (var item in listrevenue)
                    {
                        if (item.User.TypeUser == TypeUser.EC || item.User.TypeUser == TypeUser.CM || item.User.TypeUser == TypeUser.ALT)
                        {
                            ev.Range += (int)item.CI;
                            if (item.User.TypeUser == TypeUser.EC || item.User.TypeUser == TypeUser.ALT)
                                ev.RangeNewCustomer += (int)item.CI;
                            else
                                ev.RangeStudent += (int)item.CI;
                        }
                    }

                    _unitOfWork.Save();
                }
                else
                {
                    return Json(new { status = false, msg = "Ngày này không có sự kiện hoặc nhân sự không được phân công " });
                }


                return Json(new { status = true/*, msg = "Cập nhật thành công" */});
            }
            return Json(new { status = false, msg = "Chưa cập nhật các tỉ lệ chuyển đổi cho người dùng này" });

        }

        public PartialViewResult LoadHistoryEvent(int year, int month, int officeId, int weekNumber, int dayOfWeek, int Type)
        {
            var model = new LoadHistoryEventViewModel
            {
                Year = year,
                Month = month,
                Office = _unitOfWork.OfficeRepository.GetById(officeId),
                Events = _unitOfWork.EventRepository.GetQuery(a => a.Year == year && a.Month == month && a.OfficeId == officeId && (int)a.WeekNumber == weekNumber && (int)a.DayofWeek == dayOfWeek && (int)a.TypeEvent == Type, q => q.OrderBy(a => a.CreateDate)),
            };
            switch (weekNumber)
            {
                case 1:
                    model.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    model.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    model.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    model.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    model.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    model.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            switch (dayOfWeek)
            {
                case 2:
                    model.DayofWeek = DayofWeek.Monday;
                    break;
                case 3:
                    model.DayofWeek = DayofWeek.Tuesday;
                    break;
                case 4:
                    model.DayofWeek = DayofWeek.Wednessday;
                    break;
                case 5:
                    model.DayofWeek = DayofWeek.Thursday;
                    break;
                case 6:
                    model.DayofWeek = DayofWeek.Friday;
                    break;
                case 7:
                    model.DayofWeek = DayofWeek.Saturday;
                    break;
                case 8:
                    model.DayofWeek = DayofWeek.Sunday;
                    break;
                default:
                    break;
            }

            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueUser_Day(int year, int month, int historyUserId, int weekNumber, int dayOfWeek)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            var model = new LoadHistoryRevenueUser_DayViewModel
            {
                Year = year,
                Month = month,
                User = historyUser.User,
                HistoryUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId),
                Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.HistoryUserId == historyUserId && (int)a.WeekNumber == weekNumber && (int)a.DayofWeek == dayOfWeek && a.TargetBM != null, q => q.OrderBy(a => a.CreateDate)),
            };
            switch (weekNumber)
            {
                case 1:
                    model.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    model.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    model.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    model.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    model.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    model.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            switch (dayOfWeek)
            {
                case 2:
                    model.DayofWeek = DayofWeek.Monday;
                    break;
                case 3:
                    model.DayofWeek = DayofWeek.Tuesday;
                    break;
                case 4:
                    model.DayofWeek = DayofWeek.Wednessday;
                    break;
                case 5:
                    model.DayofWeek = DayofWeek.Thursday;
                    break;
                case 6:
                    model.DayofWeek = DayofWeek.Friday;
                    break;
                case 7:
                    model.DayofWeek = DayofWeek.Saturday;
                    break;
                case 8:
                    model.DayofWeek = DayofWeek.Sunday;
                    break;
                default:
                    break;
            }

            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueUser_Day2(int year, int month, int userId, int weekNumber, int dayOfWeek)
        {
            var model = new LoadHistoryRevenueUser_DayViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId && (int)a.WeekNumber == weekNumber && (int)a.DayofWeek == dayOfWeek && a.DT != null, q => q.OrderBy(a => a.CreateDate)),
            };
            switch (weekNumber)
            {
                case 1:
                    model.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    model.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    model.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    model.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    model.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    model.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            switch (dayOfWeek)
            {
                case 2:
                    model.DayofWeek = DayofWeek.Monday;
                    break;
                case 3:
                    model.DayofWeek = DayofWeek.Tuesday;
                    break;
                case 4:
                    model.DayofWeek = DayofWeek.Wednessday;
                    break;
                case 5:
                    model.DayofWeek = DayofWeek.Thursday;
                    break;
                case 6:
                    model.DayofWeek = DayofWeek.Friday;
                    break;
                case 7:
                    model.DayofWeek = DayofWeek.Saturday;
                    break;
                case 8:
                    model.DayofWeek = DayofWeek.Sunday;
                    break;
                default:
                    break;
            }

            return PartialView(model);
        }
        public ActionResult AddEvent(int month, int year, int week, int dayOfWeek, int typeEvent, int officeId)
        {
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return HttpNotFound();
            var ev = new Event
            {
                Month = month,
                Year = year,
                OfficeId = officeId,
            };
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(year, month, DateTime.Now);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek(week, year, month);

            switch (typeEvent)
            {
                case 1:
                    ev.TypeEvent = TypeEvent.HDDT;
                    break;
                case 2:
                    ev.TypeEvent = TypeEvent.SKDT;
                    break;
                case 3:
                    ev.TypeEvent = TypeEvent.HDSale;
                    break;
                case 4:
                    ev.TypeEvent = TypeEvent.SKSale;
                    break;
                default:
                    break;
            }
            switch (week)
            {
                case 1:
                    ev.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    ev.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    ev.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    ev.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    ev.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    ev.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            switch (dayOfWeek)
            {
                case 2:
                    ev.DayofWeek = DayofWeek.Monday;
                    break;
                case 3:
                    ev.DayofWeek = DayofWeek.Tuesday;
                    break;
                case 4:
                    ev.DayofWeek = DayofWeek.Wednessday;
                    break;
                case 5:
                    ev.DayofWeek = DayofWeek.Thursday;
                    break;
                case 6:
                    ev.DayofWeek = DayofWeek.Friday;
                    break;
                case 7:
                    ev.DayofWeek = DayofWeek.Saturday;
                    break;
                case 8:
                    ev.DayofWeek = DayofWeek.Sunday;
                    break;
                default:
                    break;
            }
            var model = new AddEventViewModel
            {
                Event = ev,
                Users = _unitOfWork.UserRepository.Get(a => a.OfficeId == officeId && a.Active && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM))
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult AddEvent(AddEventViewModel model, int DayQuantity)
        {
            if (ModelState.IsValid)
            {
                model.Event.UserIds = "," + model.Event.UserIds;
                //if (model.Event.Days.Length == 2 && DayQuantity != 1)
                //    ModelState.AddModelError("", @"Phải có ít nhất 2 ngày triển khai");
                ////else if (model.Event.Days.Length == 2 && DayQuantity == 1)
                ////    ModelState.AddModelError("", @"Phải có ít nhất 1 ngày triển khai");
                //else
                //{
                model.Event.Days = model.Event.Days.TrimEnd(',');
                _unitOfWork.EventRepository.Insert(model.Event);
                _unitOfWork.Save();
                return RedirectToAction("Index", new { result = "add" });
                //}

            }
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(model.Event.Year, model.Event.Month, DateTime.Now);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)model.Event.WeekNumber, model.Event.Year, model.Event.Month);
            model.Users = _unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId == model.Event.OfficeId);
            return View(model);
        }
        public ActionResult UpdateEvent(int evId)
        {
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return HttpNotFound();
            var ev = _unitOfWork.EventRepository.GetById(evId);
            if (ev == null)
                return RedirectToAction("Index");
            ev.Days += ",";
            var model = new AddEventViewModel
            {
                Event = new Event
                {
                    OfficeId = ev.OfficeId,
                    Year = ev.Year,
                    Month = ev.Month,
                    WeekNumber = ev.WeekNumber,
                    DayofWeek = ev.DayofWeek,
                    TypeEvent = ev.TypeEvent,
                    TypeJoin = ev.TypeJoin,
                    UserIds = ev.UserIds,
                    Days = ev.Days,
                    Name = ev.Name,
                    Ages = ev.Ages,
                    Range = ev.Range,
                    RangeStudent = ev.RangeStudent,
                    RangeNewCustomer = ev.RangeNewCustomer,
                    TimeFrom = ev.TimeFrom,
                    TimeTo = ev.TimeTo,
                    LinkName = ev.LinkName,
                    LinkUrl = ev.LinkUrl,
                    Office = ev.Office,
                },
                EventParentId = ev.Id,
                Users = _unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId == ev.OfficeId && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM))
            };
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(ev.Year, ev.Month, DateTime.Now);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)ev.WeekNumber, ev.Year, ev.Month);
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateEvent(AddEventViewModel model, int DayQuantity)
        {
            var ev = _unitOfWork.EventRepository.GetById(model.EventParentId);
            if (ev == null)
                return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                //if (model.Event.Days.Length == 2 && DayQuantity != 1)
                //{
                //    ModelState.AddModelError("", @"Phải có ít nhất 2 ngày triển khai");
                //}

                //else
                //{
                //ev.TimeFrom = model.Event.TimeFrom;
                //ev.TimeTo = model.Event.TimeTo;
                //ev.Ages = model.Event.Ages;
                //ev.TypeJoin = model.Event.TypeJoin;
                //ev.TypeEvent = model.Event.TypeEvent;
                //ev.LinkUrl = model.Event.LinkUrl;
                //ev.LinkName = model.Event.LinkName;
                ////ev.Range = model.Event.Range;
                ////ev.RangeStudent = model.Event.RangeStudent;
                ////ev.RangeNewCustomer = model.Event.RangeNewCustomer;
                //ev.Name = model.Event.Name;
                //ev.UserIds = model.Event.UserIds;
                //ev.Days = model.Event.Days.TrimEnd(',');

                model.Event.Days = model.Event.Days.TrimEnd(',');
                model.Event.Year = ev.Year;
                model.Event.Month = ev.Month;
                model.Event.WeekNumber = ev.WeekNumber;
                model.Event.DayofWeek = ev.DayofWeek;

                _unitOfWork.EventRepository.Insert(model.Event);
                _unitOfWork.Save();
                return RedirectToAction("Index", new { result = "add" });
                //}

            }
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(model.Event.Year, model.Event.Month, DateTime.Now);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)model.Event.WeekNumber, model.Event.Year, model.Event.Month);
            model.Users = _unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId == model.Event.OfficeId);
            return View(model);
        }
        [HttpPost]
        public JsonResult DeleteEvent(int evId)
        {
            var ev = _unitOfWork.EventRepository.GetById(evId);
            if (ev == null)
                return Json(new { status = false });
            var listCV = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.EventId == evId);
            listCV.Delete();
            var listev = _unitOfWork.EventRepository.GetQuery(a => a.OfficeId == ev.OfficeId && a.DayofWeek == ev.DayofWeek && a.WeekNumber == ev.WeekNumber && a.Month == ev.Month && a.Year == ev.Year);
            listev.Delete();
            //_unitOfWork.EventRepository.Delete(ev);
            //_unitOfWork.Save();
            return Json(new { status = true });
        }
        #endregion

        #region Công_nợ
        public ActionResult ListDebt(int? ZoneId, int? Month, int? OfficeId, int? Year, int? Week, int? UserType, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            ViewBag.Result = Result;
            var model = new DebtViewModel
            {
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Name)),
                User = User,
                ZoneId = ZoneId,
                UserType = UserType,
                OfficeId = OfficeId,
            };
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == model.Month && h.Year == model.Year).Select(h => new
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
                    }
                }
            }
            if (model.ZoneId != null)
                //model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
            if(model.Offices.Count() == 1)
            {
                model.OfficeId = model.Offices.First().Id;
            }
            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                var debts = _unitOfWork.DebtRepository.GetQuery(a => a.Year < model.Year || (a.Year == model.Year && a.Month <= model.Month) && a.DebtId == null);
                if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.ASM)
                    debts = debts.Where(a => a.UserId == User.Id);
                if (office != null)
                    debts = debts.Where(a => a.User.OfficeId == model.OfficeId);
                if (UserType != null)
                    debts = debts.Where(a => (int)a.User.TypeUser == UserType);
                var debtitems = debts.ToList().Select(x => new DebtViewModel.DebtItem
                {
                    DebtParentId = x.Id,
                    Debt = _unitOfWork.DebtRepository.GetQuery(a => (a.Id == x.Id || a.DebtId == x.Id), q => q.OrderByDescending(a => a.CreateDate)).FirstOrDefault()
                }).Where(x => x.Debt != null);
                model.DebtItems = debtitems;
            }
            return View(model);
        }
        public void ExportDebt(int Year, int Month, int OfficeId, int? UserType, int Active)
        {
            var debts = _unitOfWork.DebtRepository.GetQuery(a => (a.Year < Year || (a.Year == Year && a.Month <= Month)) && a.DebtId == null && a.User.OfficeId == OfficeId);
            if (Active == 1)
                debts = debts.Where(a => a.Active);
            else
                debts = debts.Where(a => !a.Active);
            if (UserType != null)
                debts = debts.Where(a => (int)a.User.TypeUser == UserType);
            var debtitems = debts.ToList().Select(x => new DebtViewModel.DebtItem
            {
                DebtParentId = x.Id,
                Debt = _unitOfWork.DebtRepository.GetQuery(a => (a.Id == x.Id || a.DebtId == x.Id), q => q.OrderByDescending(a => a.CreateDate)).FirstOrDefault()
            }).Where(x => x.Debt != null);
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Ngày phát sinh cọc");
            dt.Columns.Add("Họ tên học viên");
            dt.Columns.Add("Mã học viên");
            dt.Columns.Add("Chương trình học");
            dt.Columns.Add("Tên QĐ ưu đãi");
            dt.Columns.Add("Lộ trình");
            dt.Columns.Add("Thành tiền");
            dt.Columns.Add("Tiền cọc giữ chỗ");
            dt.Columns.Add("Tiền cọc bổ sung làm hồ sơ");
            dt.Columns.Add("Tình trạng khách hàng");
            dt.Columns.Add("Tiền giảm lộ trình");
            dt.Columns.Add("Tiền còn lại phải thanh toán");
            dt.Columns.Add("Hình thức thanh toán");
            dt.Columns.Add("Kênh trả góp");
            dt.Columns.Add("Tình trạng hồ sơ");
            dt.Columns.Add("Ngày phát sinh gộp phí");
            dt.Columns.Add("NS phụ trách");
            dt.Columns.Add("Nội dung khó khăn");
            dt.Columns.Add("Tình trạng liên hệ khách");
            dt.Columns.Add("Hướng xử lý");
            int stt = 1;
            foreach (var item in debtitems)
            {
                dt.Rows.Add(stt, item.Debt.DepositDate, item.Debt.StudentName, item.Debt.StudentCode, item.Debt.Cth, item.Debt.DiscountName, item.Debt.Pathway, item.Debt.TotalMoney, item.Debt.DebtMoney, item.Debt.DebtMoney2,
                    EnumExtensions.GetDisplayName(item.Debt.TypeDebt), item.Debt.DownMoney, item.Debt.RemainMoney, EnumExtensions.GetDisplayName(item.Debt.TypePay), EnumExtensions.GetDisplayName(item.Debt.ChannelPay),
                    item.Debt.FileStatus, item.Debt.GrossDate, item.Debt.User.Fullname, item.Debt.HardContent, item.Debt.ContactStatus, item.Debt.HandleWay);
                stt++;
            }
            var filename = $"danh-sach-cong-no.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách công nợ");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public PartialViewResult LoadHistoryDedt(int debtId)
        {
            var debt = _unitOfWork.DebtRepository.GetById(debtId);

            var model = _unitOfWork.DebtRepository.Get(a => a.DebtId == debt.DebtId || a.Id == debt.DebtId, q => q.OrderByDescending(a => a.CreateDate));
            return PartialView(model);
        }
        public ActionResult CreateDebt()
        {
            if (User.TypeUser == TypeUser.HO || User.TypeUser == TypeUser.CV || User.TypeUser == null)
                return HttpNotFound();
            var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == User.OfficeId && User.TypeUser != null)
            .Select(a => new
            {
                Id = a.Id,
                DisplayName = a.Fullname + " - " + a.MaNhanVien
            }).ToList();

            var model = new InsertDebtViewModel
            {
                Debt = new Debt
                {
                    Year = DateTime.Now.Year,
                    Month = DateTime.Now.Month
                },
                UserSelectList = new SelectList(users, "Id", "DisplayName"),
                User = User
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateDebt(InsertDebtViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Debt.TotalMoney = Convert.ToDecimal(model.TotalMoney.Replace(",", ""));
                model.Debt.DebtMoney = Convert.ToDecimal(model.DebtMoney.Replace(",", ""));
                if (model.Debt.TypeDebt == TypeDebt.Type1 || model.Debt.TypeDebt == TypeDebt.Type2)
                    model.Debt.DownMoney = 0;
                else
                    model.Debt.DownMoney = Convert.ToDecimal((model.DownMoney ?? "0").Replace(",", ""));
                if ((model.Debt.TypePay == TypePay.NoCard || model.Debt.TypePay == TypePay.Card) && !string.IsNullOrEmpty(model.DebtMoney2))
                    //model.Debt.DebtMoney2 = model.Debt.TotalMoney * 20 / 100;
                    model.Debt.DebtMoney2 = Convert.ToDecimal(model.DebtMoney2.Replace(",", ""));
                else
                    model.Debt.DebtMoney2 = 0;
                model.Debt.RemainMoney = model.Debt.TotalMoney - model.Debt.DebtMoney - model.Debt.DebtMoney2;
                if (DateTime.TryParse(model.Debt.DepositDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                {
                    var date = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                    model.Debt.DepositDate = date.ToString("dd/MM/yyyy");
                    model.Debt.Year = date.Year;
                    model.Debt.Month = date.Month;
                }
                _unitOfWork.DebtRepository.Insert(model.Debt);
                _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "add" });
            }
            var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == User.OfficeId && User.TypeUser != null)
             .Select(a => new
             {
                 Id = a.Id,
                 DisplayName = a.Fullname + " - " + a.MaNhanVien
             }).ToList();
            model.UserSelectList = new SelectList(users, "Id", "DisplayName");
            model.User = User;
            return View(model);
        }

        public ActionResult UpdateDebt(int id)
        {
            var debt = _unitOfWork.DebtRepository.GetById(id);
            if (debt == null)
                return RedirectToAction("Index");
            Debt debtparent = null;
            if (debt.DebtId == null)
                debtparent = debt;
            else
            {
                debtparent = _unitOfWork.DebtRepository.GetById(debt.DebtId);
                if (debtparent == null)
                    return RedirectToAction("Index");
            }
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM && User.Id != debt.UserId)
                return HttpNotFound();
            var model = new InsertDebtViewModel
            {
                Debt = new Debt
                {
                    DebtId = debtparent.Id,
                    DepositDate = debt.DepositDate,
                    Month = debt.Month,
                    Year = debt.Year,
                    UserId = debt.UserId,
                    StudentName = debt.StudentName,
                    StudentCode = debt.StudentCode,
                    Cth = debt.Cth,
                    DiscountName = debt.DiscountName,
                    Pathway = debt.Pathway,
                    DebtMoney2 = debt.DebtMoney2,
                    RemainMoney = debt.RemainMoney,
                    TypeDebt = debt.TypeDebt,
                    ChannelPay = debt.ChannelPay,
                    TypePay = debt.TypePay,
                    FileStatus = debt.FileStatus,
                    GrossDate = debt.GrossDate,
                    HardContent = debt.HardContent,
                    ContactStatus = debt.ContactStatus,
                    HandleWay = debt.HandleWay,
                    User = debt.User

                },
                DebtMoney = debt.DebtMoney.ToString("N0"),
                DebtMoney2 = debt.DebtMoney2.ToString("N0"),
                TotalMoney = debt.TotalMoney.ToString("N0"),
                DownMoney = debt.DownMoney.ToString("N0"),
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateDebt(InsertDebtViewModel model)
        {
            var debt = _unitOfWork.DebtRepository.GetById(model.Debt.DebtId);
            if (debt == null)
                return RedirectToAction("ListDebt");
            if (ModelState.IsValid)
            {
                //debt.TypeDebt = model.Debt.TypeDebt;
                //debt.TypePay = model.Debt.TypePay;
                //debt.ChannelPay = model.Debt.ChannelPay;
                //debt.HardContent = model.Debt.HardContent;
                //debt.ContactStatus = model.Debt.ContactStatus;
                //debt.HandleWay = model.Debt.HandleWay;
                //debt.GrossDate = model.Debt.GrossDate;
                //debt.Cth = model.Debt.Cth;
                //debt.DiscountName = model.Debt.DiscountName;
                //debt.StudentCode = model.Debt.StudentCode;
                //debt.StudentName = model.Debt.StudentName;
                model.Debt.TotalMoney = Convert.ToDecimal(model.TotalMoney.Replace(",", ""));
                model.Debt.DebtMoney = Convert.ToDecimal(model.DebtMoney.Replace(",", ""));
                if (model.Debt.TypeDebt == TypeDebt.Type1 || model.Debt.TypeDebt == TypeDebt.Type2)
                    model.Debt.DownMoney = 0;
                else
                    model.Debt.DownMoney = Convert.ToDecimal((model.DownMoney ?? "0").Replace(",", ""));
                if ((model.Debt.TypePay == TypePay.NoCard || model.Debt.TypePay == TypePay.Card) && !string.IsNullOrEmpty(model.DebtMoney2))
                    //model.Debt.DebtMoney2 = model.Debt.TotalMoney * 20 / 100;
                    model.Debt.DebtMoney2 = Convert.ToDecimal(model.DebtMoney2.Replace(",", ""));
                else
                    model.Debt.DebtMoney2 = 0;
                model.Debt.RemainMoney = model.Debt.TotalMoney - model.Debt.DebtMoney - model.Debt.DebtMoney2;
                if (DateTime.TryParse(model.Debt.DepositDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                {
                    var date = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                    model.Debt.DepositDate = date.ToString("dd/MM/yyyy");
                    model.Debt.Year = date.Year;
                    model.Debt.Month = date.Month;
                }
                _unitOfWork.DebtRepository.Insert(model.Debt);
                var debts = _unitOfWork.DebtRepository.GetQuery(a => a.DebtId == model.Debt.DebtId || a.Id == model.Debt.DebtId);
                foreach (var item in debts)
                {
                    //item.DepositDate = model.Debt.DepositDate;
                    item.Month = model.Debt.Month;
                    item.Year = model.Debt.Year;
                    item.TypeDebt = model.Debt.TypeDebt;
                }
                _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "update" });
            }
            return View(model);
        }

        [HttpPost]
        public bool DeleteDebt(int debtId = 0)
        {
            var debt = _unitOfWork.DebtRepository.GetById(debtId);
            if (debt == null)
            {
                return false;
            }
            if (debt.DebtId != null)
            {
                var debts = _unitOfWork.DebtRepository.GetQuery(a => a.Id == debt.DebtId || a.DebtId == debt.DebtId);
                foreach (var item in debts)
                {
                    item.Active = !debt.Active;
                }
            }
            else
            {
                debt.Active = !debt.Active;
            }
            _unitOfWork.Save();
            return true;
        }

        public ActionResult UpdateDownPathway(int debtId)
        {
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return HttpNotFound();
            var debt = _unitOfWork.DebtRepository.GetById(debtId);
            if (debt == null)
                return RedirectToAction("ListDebt");
            var downPathway = new DownPathwayViewModel
            {
                DownPathway = new DownPathway
                {
                    DebtId = debtId,
                    Debt = debt,
                }

            };
            return View(downPathway);
        }
        [HttpPost]
        public ActionResult UpdateDownPathway(DownPathwayViewModel model)
        {
            if (ModelState.IsValid)
            {
                var debt = _unitOfWork.DebtRepository.GetById(model.DownPathway.DebtId);
                if (debt == null)
                    return RedirectToAction("ListDebt");

                model.DownPathway.Money = Convert.ToDecimal(model.Money.Replace(",", ""));
                debt.DownMoney = model.DownPathway.Money;
                _unitOfWork.DownPathwayRepository.Insert(model.DownPathway);
                _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "add" });
            }

            return View(model);
        }
        [ChildActionOnly]
        public PartialViewResult LoadHistoryDownPathway(int debtId)
        {
            var model = _unitOfWork.DownPathwayRepository.Get(a => a.DebtId == debtId);
            return PartialView(model);
        }
        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of any resources here if needed
            }
            base.Dispose(disposing);
        }
    }
}
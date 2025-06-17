using Helpers;
using NSec.Cryptography;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    public class EventController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        #region Sự_Kiện
        public ActionResult Index(int? ZoneId,int? Month, int? OfficeId, int? Year, int? Week, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(Year ?? DateTime.Now.Year, Month ?? DateTime.Now.Month);
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
                User = User,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Name))
            };
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.Id + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.ZoneId.ToString() + ","));
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
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                {
                    var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == model.OfficeId).ToList();
                    var userItems = users.Select(a => new EventViewModel.UserItem
                    {
                        User = a,
                        Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(p => p.UserId == a.Id && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week, q => q.OrderByDescending(p => p.CreateDate)),
                        RevenueUser_Week = _unitOfWork.RevenueUser_WeekRepository.GetQuery(p => p.UserId == a.Id && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week, q => q.OrderByDescending(p => p.CreateDate)).FirstOrDefault(),
                    });
                    model.Users = users;
                    model.UserItems = userItems;
                    model.Events = _unitOfWork.EventRepository.GetQuery(a => a.OfficeId == model.OfficeId && a.Month == model.Month && a.Year == model.Year && (int)a.WeekNumber == model.Week, q => q.OrderByDescending(p => p.CreateDate));
                    model.Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(p => p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week && p.User.OfficeId == model.OfficeId, q => q.OrderByDescending(p => p.CreateDate));
                }
            }
            return View(model);
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
        public JsonResult AddOrUpdateRevenueDay(int year, int month, int week, int userId, decimal targetBM, int dayOfWeek)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
                return Json(new { status = false, msg = "Cập nhật thất bại" });

            if (user.DT > 0 && user.CI > 0 && user.Confirm2 > 0 && user.Confirm1 > 0 && user.RevenueAverage > 0)
            {
                var ev = _unitOfWork.EventRepository.GetQuery(a => a.Year == year && a.Month == month && (int)a.WeekNumber == week && (int)a.DayofWeek == dayOfWeek).FirstOrDefault();
                if (ev != null && ev.UserIds.Contains("," + userId.ToString() + ","))
                {
                    List<int> days = ev.Days.Split(',').Select(int.Parse).OrderBy(x => x).ToList();

                    var dt = Math.Ceiling(targetBM / user.RevenueAverage);
                    var ci = dt / user.DT * 100;
                    var ciDay = ci / 2;
                    var cf3 = ci / user.CI * 100;
                    var cf2 = cf3;
                    if (user.Confirm3 > 0)
                        cf2 = cf3 / user.Confirm3 * 100;
                    var cf2Day = cf2 / 2;
                    var cf1 = cf2 / user.Confirm2 * 100;
                    var cf1Day = cf1 / (days.Count() - 1);
                    var dataQuantity = cf1 / user.Confirm1 * 100;
                    var dataDay = dataQuantity / (days.Count() - 1);
                    int i = 1;
                    foreach (var day in days)
                    {
                        var revenue = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.EventId == ev.Id && (int)a.DayofWeek == day && a.UserId == userId).FirstOrDefault();
                        if (revenue == null)
                        {
                            revenue = new RevenueUser_DayOfWeek
                            {
                                Year = year,
                                Month = month,
                                UserId = userId,
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
                        if (i < days.Count())
                        {
                            revenue.DataQuantity = dataDay;
                            revenue.Confirm1 = cf1Day;
                        }
                        if (i >= days.Count() - 1)
                        {
                            revenue.CI = ciDay;
                            revenue.Confirm2 = cf2Day;
                            if (i == days.Count())
                                revenue.TargetBM = targetBM;
                            revenue.DT = dt;
                        }
                        _unitOfWork.Save();
                        i++;
                    }

                }
                else if (ev == null)
                {

                    var revenue = new RevenueUser_DayOfWeek
                    {
                        TargetBM = targetBM,
                        Active = true,
                        Year = year,
                        Month = month,
                        UserId = userId,
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
                    switch (dayOfWeek)
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
                    _unitOfWork.Save();
                }
                return Json(new { status = true/*, msg = "Cập nhật thành công" */});
            }
            return Json(new { status = false, msg = "Chưa cập nhật các tỉ lệ chuyển đổi cho người dùng này" });



        }

        public PartialViewResult LoadHistoryRevenueUser_Day(int year, int month, int userId, int weekNumber, int dayOfWeek)
        {
            var model = new LoadHistoryRevenueUser_DayViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId && (int)a.WeekNumber == weekNumber && (int)a.DayofWeek == dayOfWeek && a.TargetBM != null, q => q.OrderBy(a => a.CreateDate)),
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
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(year, month);
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
                Users = _unitOfWork.UserRepository.Get(a => a.OfficeId == officeId)
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult AddEvent(AddEventViewModel model)
        {
            if (ModelState.IsValid)
            {
                model.Event.UserIds = "," + model.Event.UserIds;
                if (model.Event.Days.Length == 2)
                    ModelState.AddModelError("", @"Phải có ít nhất 2 ngày triển khai");
                else
                {
                    model.Event.Days = model.Event.Days.TrimEnd(',');
                    _unitOfWork.EventRepository.Insert(model.Event);
                    _unitOfWork.Save();
                    return RedirectToAction("Index", new { result = "add" });
                }

            }
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(model.Event.Year, model.Event.Month);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)model.Event.WeekNumber, model.Event.Year, model.Event.Month);
            model.Users = _unitOfWork.UserRepository.Get(a => a.OfficeId == model.Event.OfficeId);
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
                Event = ev,
                Users = _unitOfWork.UserRepository.Get(a => a.OfficeId == ev.OfficeId)
            };
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(ev.Year, ev.Month);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)ev.WeekNumber, ev.Year, ev.Month);
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateEvent(AddEventViewModel model)
        {
            var ev = _unitOfWork.EventRepository.GetById(model.Event.Id);
            if (ev == null)
                return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                if (model.Event.Days.Length == 2)
                    ModelState.AddModelError("", @"Phải có ít nhất 2 ngày triển khai");
                else
                {
                    ev.TimeFrom = model.Event.TimeFrom;
                    ev.TimeTo = model.Event.TimeTo;
                    ev.Ages = model.Event.Ages;
                    ev.TypeJoin = model.Event.TypeJoin;
                    ev.TypeEvent = model.Event.TypeEvent;
                    ev.LinkUrl = model.Event.LinkUrl;
                    ev.LinkName = model.Event.LinkName;
                    ev.Range = model.Event.Range;
                    ev.Name = model.Event.Name;
                    ev.UserIds = model.Event.UserIds;
                    ev.Days = model.Event.Days.TrimEnd(',');
                    _unitOfWork.Save();
                    return RedirectToAction("Index", new { result = "add" });
                }

            }
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(model.Event.Year, model.Event.Month);
            ViewBag.CurrentWeek = currentWeek;
            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek((int)model.Event.WeekNumber, model.Event.Year, model.Event.Month);
            model.Users = _unitOfWork.UserRepository.Get(a => a.OfficeId == model.Event.OfficeId);
            return View(model);
        }
        #endregion

        #region Công_nợ
        public ActionResult ListDebt(int?ZoneId,int? Month, int? OfficeId, int? Year, int? Week, string Result = "")
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
                OfficeId = OfficeId,
            };
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.Id + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.ZoneId.ToString() + ","));
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
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                    model.Debts = _unitOfWork.DebtRepository.GetQuery(a => a.User.OfficeId == model.OfficeId);
            }
            return View(model);
        }
        public ActionResult UpdateDebt(int id)
        {
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return HttpNotFound();
            var debt = _unitOfWork.DebtRepository.GetById(id);
            if (debt == null)
                return RedirectToAction("Index");
            return View(debt);
        }
        [HttpPost]
        public ActionResult UpdateDebt(Debt model)
        {
            var debt = _unitOfWork.DebtRepository.GetById(model.Id);
            if (debt == null)
                return RedirectToAction("ListDebt");
            if (ModelState.IsValid)
            {
                debt.TypeDebt = model.TypeDebt;
                debt.TypePay = model.TypePay;
                debt.ChannelPay = model.ChannelPay;
                debt.TypeDebt = model.TypeDebt;
                debt.HardContent = model.HardContent;
                debt.ContactStatus = model.ContactStatus;
                debt.HandleWay = model.HandleWay;
                if (model.TypeDebt == TypeDebt.Type1 || model.TypeDebt == TypeDebt.Type2)
                    debt.DownMoney = 0;
                if (model.TypePay == TypePay.NoCard || model.TypePay == TypePay.Card)
                {
                    debt.DebtMoney2 = debt.TotalMoney * 20 / 100;
                }
                else
                {
                    debt.DebtMoney2 = 0;
                }
                debt.RemainMoney = debt.TotalMoney - debt.DebtMoney - debt.DebtMoney2;
                    _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "add" });
            }
            return View(model);
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
                if(debt == null)
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
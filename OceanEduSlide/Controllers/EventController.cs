using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
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
        public ActionResult Index(int? Month, int? OfficeId, int? Year, int? Week, string Result = "")
        {
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.BM)
                return RedirectToAction("Index", "Home");
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
                User = User,
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active, q => q.OrderBy(a => a.Name))
            };
            if (User.TypeUser == TypeUser.BM)
                model.OfficeId = User.OfficeId;
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
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
                return RedirectToAction("Index");
            var model = new UpdatePercentViewModel { UserId = userId, Fullname = user.Fullname ?? user.Username,  CF1 = user.Confirm1, CF2 = user.Confirm2, CF3 = user.Confirm3,CI = user.CI,DT = user.DT,RevenueAverage = user.RevenueAverage.ToString("N0") };
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
        public JsonResult AddOrUpdateRevenueDay(int year, int month, int week, int userId, decimal targetBM, decimal targetBM_DT, int dayOfWeek)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
                return Json(new { status = false });
            var revenue = new RevenueUser_DayOfWeek
            {
                Year = year,
                Month = month,
                UserId = userId,
                TargetBM = targetBM,
                DT = targetBM_DT,
                Active = true,
            };
            if (user.DT > 0 && user.CI > 0 && user.Confirm2 > 0 && user.Confirm1 > 0)
            {
                revenue.CI = targetBM_DT / user.DT * 100;
                revenue.Confirm3 = revenue.CI / user.CI * 100;
                if (user.Confirm3 > 0)
                    revenue.Confirm2 = revenue.Confirm3 / user.Confirm3 * 100;
                else
                    revenue.Confirm2 = revenue.Confirm3;

                revenue.Confirm1 = revenue.Confirm2 / user.Confirm2 * 100;
                revenue.DataQuantity = revenue.Confirm1 / user.Confirm1 * 100;
            }


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
            return Json(new { status = true });
        }

        public PartialViewResult LoadHistoryRevenueUser_Day(int year, int month, int userId, int weekNumber, int dayOfWeek)
        {
            var model = new LoadHistoryRevenueUser_DayViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId && (int)a.WeekNumber == weekNumber && (int)a.DayofWeek == dayOfWeek, q => q.OrderBy(a => a.CreateDate)),
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
        public ActionResult AddEvent(Event model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.EventRepository.Insert(model);
                _unitOfWork.Save();
                return RedirectToAction("Index", new { result = "add" });
            }
            return View(model);
        }
        public ActionResult UpdateEvent(int evId)
        {
            var ev = _unitOfWork.EventRepository.GetById(evId);
            if (ev == null)
                return RedirectToAction("Index");
            return View(ev);
        }
        [HttpPost]
        public ActionResult UpdateEvent(Event model)
        {
            var ev = _unitOfWork.EventRepository.GetById(model.Id);
            if (ev == null)
                return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                ev.TimeFrom = model.TimeFrom;
                ev.TimeTo = model.TimeTo;
                ev.Ages = model.Ages;
                ev.TypeJoin = model.TypeJoin;
                ev.TypeEvent = model.TypeEvent;
                ev.LinkUrl = model.LinkUrl;
                ev.LinkName = model.LinkName;
                ev.Range = model.Range;
                ev.Name = model.Name;
                _unitOfWork.Save();
                return RedirectToAction("Index", new { result = "add" });
            }
            return View(model);
        }
        #endregion

        #region Công_nợ
        public ActionResult ListDebt(int? Month, int? OfficeId, int? Year, int? Week, string Result = "")
        {
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.BM)
                return RedirectToAction("Index", "Home");
            ViewBag.Result = Result;
            var model = new DebtViewModel
            {
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active, q => q.OrderBy(a => a.Name)),
                User = User,
            };
            if (User.TypeUser == TypeUser.BM)
                model.OfficeId = User.OfficeId;
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
                _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "add" });
            }
            return View(model);
        }
        public ActionResult UpdateDownPathway(int debtId)
        {
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
                if (model.Money != null)
                {
                    model.DownPathway.Money = Convert.ToDecimal(model.Money.Replace(",", ""));
                }

                _unitOfWork.DownPathwayRepository.Insert(model.DownPathway);
                _unitOfWork.Save();
                return RedirectToAction("ListDebt", new { result = "add" });
            }

            return View(model);
        }
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
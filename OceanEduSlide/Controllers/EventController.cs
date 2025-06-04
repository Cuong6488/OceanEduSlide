using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Data.Entity;
using System.Linq;
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
                }

            }
            return View(model);
        }
        public ActionResult UpdatePercent(int userId)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
                return RedirectToAction("Index");
            var model = new UpdatePercentViewModel { UserId = userId, Fullname = user.Fullname ?? user.Username };
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
                user.Confirm1 = model.CF1;
                user.Confirm2 = model.CF2;
                user.Confirm3 = model.CF3;
                user.CI = model.CI;
                user.DT = model.DT;
                _unitOfWork.Save();
                return RedirectToAction("Index");
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
    }
}
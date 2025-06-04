using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
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
            ViewBag.DayOfWeeks = DateHelper.GetWorkingDaysInWeek(Week?? currentWeek, Year ?? DateTime.Now.Year, Month ?? DateTime.Now.Month);
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
                        Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(p => p.UserId == a.Id && p.Month == model.Month && p.Year == model.Year && (int)p.WeekNumber == model.Week),
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

    }
}
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
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    public class TuyenSinhController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();

        #region Tuyen_sinh
        public PartialViewResult Header(string name)
        {
            ViewBag.Name = name;
            return PartialView();
        }
        public ActionResult Revenue(int? Month, int? OfficeId, int? Year, string Result = "")
        {
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.BM)
                return RedirectToAction("Index");
            Office office = null;
            var model = new RevenueViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "Id", "Name"),
                Month = Month,
                Year = Year,
                OfficeId = OfficeId,
                User = User,
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active, q => q.OrderBy(a => a.Name))
            };
            if (User.TypeUser == TypeUser.BM)
                model.OfficeId = User.OfficeId;
            ViewBag.Result = Result;
            ViewBag.Year = DateTime.Now.Year;
            ViewBag.WorkingWeeks = 5;
            ViewBag.CurrentWeeks = 0;
            if (Month != null && OfficeId != null && Year != null)
            {
                office = _unitOfWork.OfficeRepository.GetById(OfficeId);
                if (office != null)
                {
                    model.RevenueOffice = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == OfficeId && a.Month == Month && a.Year == Year).FirstOrDefault();
                    model.RevenueOffice_BMs = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == OfficeId && a.Month == Month && a.Year == Year, q => q.OrderByDescending(a => a.CreateDate));
                    var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == OfficeId).ToList();
                    var userItems = users.Select(a => new RevenueViewModel.UserItem
                    {
                        User = a,
                        RevenueUser_Month = _unitOfWork.RevenueUser_MonthRepository.GetQuery(p => p.UserId == a.Id && p.Month == Month && p.Year == Year).FirstOrDefault(),
                        RevenueUser_Month_BMs = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(p => p.UserId == a.Id && p.Month == Month && p.Year == Year, q => q.OrderByDescending(p => p.CreateDate)),
                        RevenueUser_Weeks = _unitOfWork.RevenueUser_WeekRepository.GetQuery(p => p.UserId == a.Id && p.Month == Month && p.Year == Year, q => q.OrderByDescending(p => p.CreateDate)),

                    });
                    model.UserItems = userItems;
                }
                DateTime firstDay = new DateTime(Year ?? 1, Month ?? 1, 1);
                DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
                DateTime today = DateTime.Now;

                int workingWeeks = 1; // Bắt đầu từ tuần 1
                int currentWeek = 0;

                DateTime currentDay = firstDay;

                // Duyệt từng ngày trong tháng
                while (currentDay <= lastDay)
                {
                    // Nếu là thứ Hai và không phải ngày đầu tháng => bắt đầu tuần mới
                    if (currentDay.DayOfWeek == DayOfWeek.Monday && currentDay != firstDay)
                    {
                        workingWeeks++;
                    }

                    // Nếu ngày hiện tại trùng với `today`, cập nhật `currentWeek`
                    if (currentDay.Year == today.Year && currentDay.Month == today.Month && currentDay.Day == today.Day)
                    {
                        currentWeek = workingWeeks;
                    }

                    currentDay = currentDay.AddDays(1);
                }

                // Nếu hôm nay không nằm trong tháng xét, gán `currentWeek = 0`
                if (today.Month != Month || today.Year != Year)
                {
                    currentWeek = 0;
                }

                ViewBag.WorkingWeeks = workingWeeks;
                ViewBag.CurrentWeeks = currentWeek;
            }

            return View(model);
        }
        public ActionResult CalculateWorkWeeks(int year, int month)
        {
            int workingWeeks = CountWorkingWeeks(year, month);
            return Json(new { Year = year, Month = month, WorkWeeks = workingWeeks }, JsonRequestBehavior.AllowGet);
        }

        private int CountWorkingWeeks(int year, int month)
        {
            DateTime firstDay = new DateTime(year, month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);

            int workingWeeks = 0;
            DateTime currentDay = firstDay;

            while (currentDay <= lastDay)
            {
                if (currentDay.DayOfWeek == DayOfWeek.Monday)
                {
                    workingWeeks++;
                }
                currentDay = currentDay.AddDays(1);
            }

            if (firstDay.DayOfWeek != DayOfWeek.Monday)
            {
                workingWeeks++;
            }

            return workingWeeks;
        }
        public ActionResult RevenueOffice()
        {
            if (User.TypeUser != TypeUser.HO)
                return RedirectToAction("Index");
            var model = new RevenueOfficeViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "Id", "Name"),
                RevenueOffice = new RevenueOffice { Active = true },
            };
            ViewBag.Year = DateTime.Now.Year;
            return View(model);
        }
        [HttpPost]
        public ActionResult RevenueOffice(RevenueOfficeViewModel model)
        {
            if (User.TypeUser != TypeUser.HO)
                return RedirectToAction("Index");
            if (ModelState.IsValid)
            {
                _unitOfWork.RevenueOfficeRepository.Insert(model.RevenueOffice);
                _unitOfWork.Save();
                return RedirectToAction("ListRevenueOffice", new { result = "add" });
            }
            ViewBag.Year = DateTime.Now.Year;
            return View(model);
        }

        public ActionResult RevenueOffice_BM(int officeId, int month, int year)
        {
            var office = _unitOfWork.OfficeRepository.GetById(officeId);
            if (office == null || (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.HO))
                return RedirectToAction("Index");
            var model = new RevenueOffice_BMViewModel
            {
                RevenueOffice = new RevenueOffice_BM { Active = true, OfficeId = officeId, Month = month, Year = year },
                OfficeName = office.Name,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult RevenueOffice_BM(RevenueOffice_BMViewModel model)
        {

            model.RevenueOffice.TargetBM_New = Convert.ToDecimal(model.TargetBM_New.Replace(",", ""));
            model.RevenueOffice.TargetBM_HV = Convert.ToDecimal(model.TargetBM_HV.Replace(",", ""));
            model.RevenueOffice.TargetBM_SAB = Convert.ToDecimal(model.TargetBM_SAB.Replace(",", ""));
            model.RevenueOffice.TargetBM_TS = Convert.ToDecimal(model.TargetBM_TS.Replace(",", ""));

            _unitOfWork.RevenueOffice_BMRepository.Insert(model.RevenueOffice);
            _unitOfWork.Save();
            return RedirectToAction("Revenue", new { Result = "add", Month = model.RevenueOffice.Month, Year = model.RevenueOffice.Year, OfficeId = model.RevenueOffice.OfficeId });
        }
        public ActionResult ListRevenueOffice(int? page, int? officeId, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var revenueOffices = _unitOfWork.RevenueOfficeRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month)).AsNoTracking();

            if (officeId > 0)
            {
                revenueOffices = revenueOffices.Where(a => a.OfficeId == officeId);
            }
            var model = new ListRevenueOfficeViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "Name"),
                RevenueOffices = revenueOffices.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
            };
            return View(model);
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueMonth(int year, int month, int userId, decimal targetBM)
        {
            var revenue = new RevenueUser_Month_BM
            {
                Year = year,
                Month = month,
                UserId = userId,
                TargetBM = targetBM,
                Active = true,

            };
            _unitOfWork.RevenueUser_Month_BMRepository.Insert(revenue);
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueWeek(int year, int month, int userId, decimal targetBM, int weekNumber)
        {
            var revenue = new RevenueUser_Week
            {
                Year = year,
                Month = month,
                UserId = userId,
                TargetBM = targetBM,
                Active = true,
            };
            switch (weekNumber)
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
            _unitOfWork.RevenueUser_WeekRepository.Insert(revenue);
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        public PartialViewResult LoadHistoryRevenueUser_Month(int year, int month, int userId)
        {
            var model = new LoadHistoryRevenueUser_MonthViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId, q => q.OrderBy(a => a.CreateDate)),
            };
            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueUser_Week(int year, int month, int userId, int weekNumber)
        {
            var model = new LoadHistoryRevenueUser_WeekViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId && (int)a.WeekNumber == weekNumber, q => q.OrderBy(a => a.CreateDate)),
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
            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueOffice(int year, int month, int officeId)
        {
            var model = new LoadHistoryRevenueOfficeViewModel
            {
                Year = year,
                Month = month,
                Office = _unitOfWork.OfficeRepository.GetById(officeId),
                Revenues = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.Year == year && a.Month == month && a.OfficeId == officeId, q => q.OrderBy(a => a.CreateDate)),
            };

            return PartialView(model);
        }

        #endregion

    }
}
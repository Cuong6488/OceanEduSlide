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
        public ActionResult Revenue(int? Month, int? OfficeId, int? Year)
        {
            //if (User.TypeUser != TypeUser.HO)
            //    return RedirectToAction("Index");
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
            if(Month != null && OfficeId != null && Year != null)
            {
                office = _unitOfWork.OfficeRepository.GetById(OfficeId);
                if(office != null)
                {
                    model.RevenueOffice = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == OfficeId && a.Month == Month && a.Year == Year).FirstOrDefault();
                    model.RevenueOffice_BMs = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == OfficeId && a.Month == Month && a.Year == Year,q => q.OrderByDescending(a => a.CreateDate));
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
            }
            ViewBag.Year = DateTime.Now.Year;
            return View(model);
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
            if(ModelState.IsValid)
            {
                _unitOfWork.RevenueOfficeRepository.Insert(model.RevenueOffice);
                _unitOfWork.Save();
            return RedirectToAction("ListRevenueOffice", new { result = "add" });
            }
            ViewBag.Year = DateTime.Now.Year;
            return View(model);
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
        public PartialViewResult LoadHistoryRevenueUser_Month(int year, int month, int userId)
        {
            var revenues = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId);
            ViewBag.Year = year;
            ViewBag.Month = month;
            return PartialView(revenues);
        }
        #endregion

    }
}
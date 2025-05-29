using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
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


        public ActionResult RevenueOffice(int? Month, int? OfficeId)
        {
            var model = new RevenueViewModel
            {
                 SelectOffices = new SelectList(_unitOfWork.DiscountRepository.Get(a => a.Active), "Id", "Name"),
                 Month = Month,
                 OfficeId = OfficeId
            };
            return View(model);
        }
        public ActionResult Revenue(int? Month, int? OfficeId)
        {
            var model = new RevenueViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.DiscountRepository.Get(a => a.Active), "Id", "Name"),
                Month = Month,
                OfficeId = OfficeId
            };
            return View(model);
        }
        #endregion

    }
}
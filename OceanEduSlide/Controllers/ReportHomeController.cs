using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    public class ReportHomeController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year)
        {

            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
            var model = new ListReportHomeViewModel
            {
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
                ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year), q=> q.OrderBy(a => a.Sort))
                //OfficeId = OfficeId,
            };
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
            }
            else
            {
                model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                    model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                else
                    model.Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Id == User.OfficeId);
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
            }    

            return View(model);
        }

    }
}
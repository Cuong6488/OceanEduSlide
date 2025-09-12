using OceanEduSlide.DAL;
using OceanEduSlide.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.Controllers
{
    public class BaseController : Controller
    {
        public readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public SelectList ZoneSelectList => new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name");
        public SelectList OfficeSelectList(int? zoneId) => new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active && a.ZoneId == zoneId, q => q.OrderBy(a => a.Sort)), "Id", "ShortName");


        public JsonResult GetOffice(int? zoneId)
        {
            var offices = _unitOfWork.OfficeRepository
                .GetQuery(a => a.Active && a.ZoneId == zoneId, q => q.OrderBy(a => a.Name)).Select(a => new { a.Id, a.Name });
            return Json(offices, JsonRequestBehavior.AllowGet);
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
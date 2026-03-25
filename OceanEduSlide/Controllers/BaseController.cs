using OceanEduSlide.DAL;
using System.Linq;
using System.Web.Mvc;
using OceanEduSlide.Utils;
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
        public JsonResult GetUserManagerPresent(int officeId)
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == officeId).Select(a => new { a.Id, a.Fullname, a.MaNhanVien });
            return Json(users, JsonRequestBehavior.AllowGet);
        }
        protected void DebugModelState()
        {
            var errors = ModelState.GetAllErrors();
            foreach (var e in errors)
            {
                System.Diagnostics.Debug.WriteLine($"{e.Field}: {e.Message}");
            }
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
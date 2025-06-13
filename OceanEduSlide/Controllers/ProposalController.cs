using OceanEduSlide.DAL;
using OceanEduSlide.EnumHelpers;
using OceanEduSlide.Filters;
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
    public class ProposalController : Controller
    {
        // GET: Proposal
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();

        public ActionResult Propose()
        {
            if (User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.BM)
                return RedirectToAction("Index", "Home");
            var model = new ProposeViewModel
            {
                Proposal = new Proposal { UserId = User.Id, User = User },
            };
            if (User.TypeUser == TypeUser.ASM)
            {
                var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains("," + User.OfficeId.ToString() + ",")).FirstOrDefault();
                if (zone != null)
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Zone.OfficeIds.Contains("," + a.Id.ToString() + ",")), "Id", "Name");
            }
            else
                model.Proposal.OfficeId = User.OfficeId;
            ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();
            return View(model);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Propose(ProposeViewModel model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.ProposalRepository.Insert(model.Proposal);
                _unitOfWork.Save();

            }
            if (User.TypeUser == TypeUser.ASM)
            {
                var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains(User.OfficeId.ToString())).FirstOrDefault();
                if (zone != null)
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Zone.OfficeIds.Contains(a.Id.ToString())), "Id", "Name");
            }
            ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();
            return RedirectToAction("ListProposal", new {Result = "add"});
        }

        public ActionResult ListProposal(int? officeId, int? Month, int? Year, string Result = "")
        {
            if (User.TypeUser == TypeUser.User)
                return RedirectToAction("Index", "Home");
            ViewBag.Result = Result;
            var model = new ProposalViewModel
            {
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Name)),
                User = User,
            };
            if (User.TypeUser == TypeUser.ASM)
                model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
            else if (User.TypeUser == TypeUser.BM)
                model.OfficeId = User.OfficeId;
            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                {
                    model.Proposals = _unitOfWork.ProposalRepository.GetQuery(a => a.User.OfficeId == model.OfficeId && a.CreateDate.Month == model.Month && a.CreateDate.Year == model.Year);
                    var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains("," + model.OfficeId.ToString() + ",")).FirstOrDefault();
                    if (zone != null)
                    {
                        ViewBag.Zone = zone.Name;
                        var user = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.CV && a.Office.Zone.Id == zone.Id).FirstOrDefault();
                        if (user != null)
                        {
                            ViewBag.CVName = user.Fullname ?? user.Username;
                        }
                    }
                }
            }

            return View(model);
        }
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
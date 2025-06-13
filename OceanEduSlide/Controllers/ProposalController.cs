using OceanEduSlide.DAL;
using OceanEduSlide.EnumHelpers;
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
                //SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content")
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
                if (User.TypeUser == TypeUser.BM)
                    model.Proposal.Active = true;
                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains("," + User.OfficeId + ",")).FirstOrDefault();
                if (z != null)
                {
                    model.Proposal.ZoneId = z.Id;
                }
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
            return RedirectToAction("ListProposal", new { Result = "add" });
        }

        public ActionResult ListProposal(int? zoneId, int? officeId, int? Month, int? Year, string Result = "")
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
                OfficeId = officeId,
                ZoneId = zoneId,
            };
            var proposals = _unitOfWork.ProposalRepository.GetQuery( a=> a.CreateDate.Month == model.Month && a.CreateDate.Year == model.Year);
            if (User.TypeUser == TypeUser.ASM)
            {
                model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                //model.Proposals = _unitOfWork.ProposalRepository.GetQuery(a => a.ZoneId == User.ZoneId && a.CreateDate.Month == model.Month && a.CreateDate.Year == model.Year);
                model.ZoneId = User.ZoneId;
            }

            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.GetQuery(a => User.ZoneIds.Contains("," + a.Id + ","));
                proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.ZoneId + ","));
            }
            else if (User.TypeUser == TypeUser.BM)
                model.OfficeId = User.OfficeId;
            if (model.ZoneId != null)
                proposals = proposals.Where(a => a.ZoneId == model.ZoneId);

            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                {
                    proposals = proposals.Where(a => a.User.OfficeId == model.OfficeId);
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
            model.Proposals = proposals;
            return View(model);
        }
        public ActionResult UpdateProposal(int pId)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null)
                return RedirectToAction("ListProposal");
            var model = new ApproveViewModel
            {
                Proposal = proposal,
                SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content")
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateProposal(ApproveViewModel model)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(model.Proposal.Id);
            if (proposal == null)
                return RedirectToAction("ListProposal");
            if (proposal.CVFeedBack != model.Proposal.CVFeedBack || proposal.TypeApprove != model.Proposal.TypeApprove || proposal.TypeFaultId != model.Proposal.TypeFaultId)
                proposal.NSSeen = false;
            proposal.CVFeedBack = model.Proposal.CVFeedBack;
            proposal.TypeApprove = model.Proposal.TypeApprove;
            proposal.TypeFaultId = model.Proposal.TypeFaultId;
            _unitOfWork.Save();
            return RedirectToAction("ListProposal");
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
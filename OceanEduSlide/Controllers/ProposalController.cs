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
            if (User.TypeUser == null || User.TypeUser == TypeUser.HO || User.TypeUser == TypeUser.CV)
                return HttpNotFound();
            var model = new ProposeViewModel
            {
                Proposal = new Proposal { UserId = User.Id, User = User },
                //SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content")
            };
            if (User.TypeUser == TypeUser.ASM)
            {
                var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == User.ZoneId).FirstOrDefault();
                if (zone != null)
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.ZoneId == zone.Id), "Id", "Name");
            }
            else
                model.Proposal.OfficeId = User.OfficeId;
            //ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();
            return View(model);
        }

        [HttpPost, ValidateInput(false)]
        public ActionResult Propose(ProposeViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (User.TypeUser == TypeUser.BM || User.TypeUser == TypeUser.ASM)
                    model.Proposal.Active = true;
                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == User.ZoneId).FirstOrDefault();
                if (z != null)
                {
                    model.Proposal.ZoneId = z.Id;
                    _unitOfWork.ProposalRepository.Insert(model.Proposal);
                    _unitOfWork.Save();
                    return RedirectToAction("ListProposal", new { Result = "add" });
                }
            }
            //if (User.TypeUser == TypeUser.ASM)
            //{
            //    var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains(User.OfficeId.ToString())).FirstOrDefault();
            //    if (zone != null)
            //        model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Zone.OfficeIds.Contains(a.Id.ToString())), "Id", "Name");
            //}
            //ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();
            return RedirectToAction("ListProposal");
        }

        public ActionResult ListProposal(int? zoneId, int? officeId, int? Month, int? Year, int? Notice, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            ViewBag.Result = Result;
            var model = new ProposalViewModel
            {
                Month = Month,
                Year = Year,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Name)),
                User = User,
                OfficeId = officeId,
                ZoneId = zoneId,
                Notice = Notice,
            };
            var proposals = _unitOfWork.ProposalRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.CreateDate));
            if (Year.HasValue)
            {
                proposals = proposals.Where(a => a.CreateDate.Year == Year);
                if (Month.HasValue)
                    proposals = proposals.Where(a => a.CreateDate.Month == Month);

            }
            if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.Id + ",") && a.Active);
                proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.ZoneId + ",") && a.Active);
                if (Notice == 1)
                    proposals = proposals.Where(a => !a.CVSeen);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneIds.Contains("," + a.ZoneId + ",") && a.Active && !a.CVSeen).Count();
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.ZoneId + ","));

            }
            else if(User.TypeUser == TypeUser.HO)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
                proposals = proposals.Where(a => a.Active);
                if (Notice == 1)
                    proposals = proposals.Where(a => a.TypeApprove == TypeApprove.Type3);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => a.Active && a.TypeApprove == TypeApprove.Type3).Count();
            }
            else
            {
                model.ZoneId = User.ZoneId;
                if (Notice == 2)
                    proposals = proposals.Where(a => a.Active == false);
                else if (Notice == 3)
                {
                    proposals = proposals.Where(a => a.NSSeen == false);
                    //foreach (var item in proposals)
                    //{
                    //    item.NSSeen = true;
                    //}
                    //_unitOfWork.Save();
                }

                if (User.TypeUser != TypeUser.ASM)
                    model.OfficeId = User.OfficeId;
                if (User.TypeUser == TypeUser.ASM)
                    ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneId == a.ZoneId && a.Active && a.NSSeen == false).Count();
                else if (User.TypeUser == TypeUser.BM)
                    ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.OfficeId == a.OfficeId && a.Active && a.NSSeen == false).Count();
            }

            if (model.ZoneId != null)
            {
                proposals = proposals.Where(a => a.ZoneId == model.ZoneId);
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                var user = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.CV && a.ZoneIds.Contains("," + model.ZoneId + ",")).FirstOrDefault();
                if (user != null)
                {
                    ViewBag.CVName = user.Fullname ?? user.Username;
                }
            }
            if (model.OfficeId != null)
            {
                proposals = proposals.Where(a => a.OfficeId == model.OfficeId);
            }
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.CV)
                proposals = proposals.Where(a => a.UserId == User.Id);

            model.Proposals = proposals;
            return View(model);
            //}
            //else
            //{
            //    var model = new ProposalViewModel
            //    {
            //        Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active && User.ZoneIds.Contains("," + a.ZoneId + ","), q => q.OrderBy(a => a.Name)),
            //        User = User,
            //        ZoneId = zoneId,
            //        Zones = _unitOfWork.ZoneRepository.GetQuery(a => User.ZoneIds.Contains("," + a.Id + ",")),
            //    };
            //    //proposals = _unitOfWork.ProposalRepository.GetQuery(a => a.CVSeen == false);
            //    var proposals = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneIds.Contains("," + a.ZoneId + ",") && a.Active && a.CVSeen == false);
            //    model.ProposalItems = proposals.ToList().Select(a => new ProposalViewModel.ProposalItem
            //    {
            //        Proposal = a,
            //        CVName = _unitOfWork.UserRepository.GetQuery(c => c.ZoneId == a.ZoneId && c.TypeUser == TypeUser.CV).FirstOrDefault()?.Fullname
            //    });
            //    return View(model);
            //}
        }
        public ActionResult EditProposal(int pId)
        {
            if (User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.BM)
                return RedirectToAction("ListProposal");
            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null || proposal.CVSeen == true)
                return RedirectToAction("ListProposal");

            var model = new ProposeViewModel
            {
                Proposal = proposal,
                //SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content")
            };
            //ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult EditProposal(ProposeViewModel model)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(model.Proposal.Id);
            if (proposal == null)
                return RedirectToAction("ListProposal");
            proposal.Body = model.Proposal.Body;
            proposal.Url = model.Proposal.Url;
            proposal.Active = model.Proposal.Active;
            _unitOfWork.Save();
            return RedirectToAction("ListProposal", new { Result = "add" });
        }
        public ActionResult UpdateProposal(int pId)
        {
            if (User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.HO)
                return RedirectToAction("ListProposal");
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
            {
                proposal.NSSeen = false;
                proposal.CVSeen = true;
                proposal.CVFeedBack = model.Proposal.CVFeedBack;
                proposal.TypeApprove = model.Proposal.TypeApprove;
                proposal.TypeFaultId = model.Proposal.TypeFaultId;
                _unitOfWork.Save();
            }
            return RedirectToAction("ListProposal", new { Result = "add" });
        }
        [HttpPost]
        public JsonResult UpdateSeen(int id)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(id);
            if (proposal == null)
                return Json(new {status = false});
            proposal.NSSeen = true;
            _unitOfWork.Save();
            return Json(new { status = true });
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
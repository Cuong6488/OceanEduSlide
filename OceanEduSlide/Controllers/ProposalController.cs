using OceanEduSlide.DAL;
using OceanEduSlide.EnumHelpers;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    [ForcePasswordChangeFilter]
    public class ProposalController : Controller
    {
        // GET: Proposal
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();

        public ActionResult Propose()
        {
            if (User.TypeUser != TypeUser.BM/* && User.TypeUser != TypeUser.ASM*/)
                return HttpNotFound();
            var model = new ProposeViewModel
            {
                Proposal = new Proposal { UserId = User.Id, User = User },
                SelectProposalTypes = new SelectList(_unitOfWork.ProposalTypeRepository.Get(a => a.Active), "Id", "Content"),

            };
            //if (User.TypeUser == TypeUser.ASM)
            //{
            //    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
            //    {
            //        var zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
            //        var offices = new List<Office>();
            //        foreach (var zone in zones)
            //        {
            //            var officeAdd = _unitOfWork.OfficeRepository.GetQuery(a => a.ZoneId == zone.Id);
            //            offices.AddRange(officeAdd);
            //        }
            //        model.SelectOffices = new SelectList(offices, "Id", "ShortName");
            //    }
            //    else
            //    {
            //        var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == User.ZoneId).FirstOrDefault();
            //        if (zone != null)
            //            model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.ZoneId == zone.Id), "Id", "ShortName");
            //    }

            //}
            //else
            //{
            if (string.IsNullOrEmpty(User.OfficeIds))
            {
                model.Proposal.OfficeId = (int)User.OfficeId;
                model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId == model.Proposal.OfficeId)
                    .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien }), "Id", "FullNameWithCode");
            }
            else
            {
                var selectOffices = _unitOfWork.OfficeRepository.Get(a => User.OfficeIds.Contains("," + a.Id + ","));
                var newOfficeIds = User.OfficeIds.Trim(',');
                model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && User.OfficeIds.Contains("," + a.OfficeId + ","))
                        .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien }), "Id", "FullNameWithCode");
                if (selectOffices.Count() == 1)
                {
                    model.Proposal.OfficeId = selectOffices.First().Id;
                }
                else
                {
                    model.SelectOffices = new SelectList(selectOffices, "Id", "ShortName");
                }
            }
            //}
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
                var office = _unitOfWork.OfficeRepository.GetById(model.Proposal.OfficeId);
                if (office != null)
                    model.Proposal.MaDeXuat = DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year.ToString() + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + office.ShortCode;

                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.OfficeIds.Contains("," + model.Proposal.OfficeId + ",")).FirstOrDefault();
                if (z == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy vùng");
                    return View(model);
                }
                model.Proposal.ZoneId = z.Id;
                var cvs = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.ZoneIds.Contains(z.ShortCode));
                model.Proposal.CVName = "";
                foreach (var item in cvs)
                {
                    model.Proposal.CVName += item.Fullname + ", ";
                }
                model.Proposal.CVName = model.Proposal.CVName.Trim().Trim(',');
                var idUser2 = model.Proposal.UserId2 ?? model.Proposal.UserId;
                model.Proposal.User2 = _unitOfWork.UserRepository.GetById(idUser2);
                _unitOfWork.ProposalRepository.Insert(model.Proposal);
                _unitOfWork.Save();
                return RedirectToAction("ListProposal", new { Result = "add" });

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

        public ActionResult ListProposal(int? zoneId, int? officeId, string startDay, string endDay, int? Notice, string MaDeXuat, string Type, string Fault, string Result = "")
        {
            if (User.TypeUser == null || User.TypeUser == TypeUser.SAB || User.TypeUser == TypeUser.EC || User.TypeUser == TypeUser.CM || User.TypeUser == TypeUser.TTL || User.TypeUser == TypeUser.ALT)
                return HttpNotFound();
            ViewBag.Result = Result;
            //var pageSize = 10;
            //ViewBag.PageSize = pageSize;
            var types = _unitOfWork.ProposalTypeRepository.GetQuery().Select(a => a.Content).Distinct().ToList();
            var faults = _unitOfWork.TypeFaultRepository.GetQuery().Select(a => a.Content).Distinct().ToList();
            if (string.IsNullOrEmpty(startDay))
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                startDay = startDate.ToString("dd/MM/yyyy");

            }
            if (string.IsNullOrEmpty(endDay))
                endDay = DateTime.Now.ToString("dd/MM/yyyy");
            DateTime StartDate = new DateTime();
            DateTime EndDate = new DateTime();
            if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
            {
                StartDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
            }
            if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
            {
                EndDate = new DateTime(crd.Year, crd.Month, crd.Day, 0, 0, 0);
            }
            var model = new ProposalViewModel
            {

                StartDay = startDay,
                EndDay = endDay,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Name)),
                User = User,
                OfficeId = officeId,
                ZoneId = zoneId,
                Notice = Notice,
                MaDeXuat = MaDeXuat,
                ProposalTypes = types,
                Faults = faults,
                Type = Type,
                Fault = Fault,
            };
            var historyQuery = _unitOfWork.HistoryOfficeRepository.GetQuery(a => a.Year == model.Year);
            //var historyOffices = historyQuery.Select(h => new
            //{
            //    h.OfficeId,
            //    ZoneShortCode = h.Zone.ShortCode,
            //    h.ZoneId
            //});
            var proposals = _unitOfWork.ProposalRepository.GetQuery(a => DbFunctions.TruncateTime(a.CreateDate) >= DbFunctions.TruncateTime(StartDate) && DbFunctions.TruncateTime(a.CreateDate) <= DbFunctions.TruncateTime(EndDate), 
                q => q.OrderByDescending(a => a.CreateDate));
            if (!string.IsNullOrEmpty(MaDeXuat))
                proposals = proposals.Where(a => a.MaDeXuat.Contains(MaDeXuat));
            //if (Year.HasValue)
            //{
            //    proposals = proposals.Where(a => a.CreateDate.Year == Year);
            //    if (Month.HasValue)
            //    {
            //        historyQuery = historyQuery.Where(a => a.Month == Month);       
            //        proposals = proposals.Where(a => a.CreateDate.Month == Month);
            //    }
            //}
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month >= StartDate.Month && h.Month <= EndDate.Month && h.Year == StartDate.Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            if (!string.IsNullOrEmpty(Type))
            {
                proposals = proposals.Where(a => a.ProposalTypeId != null && a.ProposalType.Content == Type);
            }
            if (!string.IsNullOrEmpty(Fault))
            {
                proposals = proposals.Where(a => a.TypeFaultId != null && a.TypeFault.Content == Fault);
            }
            if (Notice == 1)
                proposals = proposals.Where(a => !a.CVSeen);
            if (Notice == 4)
                proposals = proposals.Where(a => a.TypeApprove == TypeApprove.Type3);
            if (Notice == 5)
                proposals = proposals.Where(a => a.TypeApprove == TypeApprove.Type2);
            if (Notice == 6)
                proposals = proposals.Where(a => a.TypeApprove == TypeApprove.Type1);
            if (User.TypeUser == TypeUser.CV)
            {
                var k = User.ZoneIds;
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") && a.Active);
                }
                if (Notice == 2)
                    proposals = proposals.Where(a => a.CVSeen);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") && a.Active && !a.CVSeen).Count();
            }
            else if (User.TypeUser == TypeUser.HO || User.TypeUser == TypeUser.PKT)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
                proposals = proposals.Where(a => a.Active);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => a.Active && a.TypeApprove == TypeApprove.Type3).Count();
            }
            else
            {
                //model.ZoneId = User.ZoneId;
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

                //if (User.TypeUser != TypeUser.ASM)
                //    model.OfficeId = User.OfficeId;
                //if (User.TypeUser == TypeUser.ASM)
                //    ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneId == a.ZoneId && a.Active && a.NSSeen == false).Count();
                //else if (User.TypeUser == TypeUser.BM)
                //    ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.OfficeId == a.OfficeId && a.Active && a.NSSeen == false).Count();
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                        ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") && a.Active && a.NSSeen == false).Count();
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            if (model.OfficeId != null)
                                proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
                        }
                    }
                    else
                    {
                        model.ZoneId = User.ZoneId;
                        ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneId == a.ZoneId && a.Active && a.NSSeen == false).Count();
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                    {
                        model.OfficeId = User.OfficeId;
                        if (User.TypeUser == TypeUser.BM)
                            ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.OfficeId == a.OfficeId && a.Active && a.NSSeen == false).Count();
                    }
                    else
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                        if (model.OfficeId == null)
                            proposals = proposals.Where(a => User.OfficeIds.Contains("," + a.OfficeId + ","));
                        if (User.TypeUser == TypeUser.BM)
                            ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.OfficeIds.Contains("," + a.OfficeId.ToString() + ",") && a.Active && a.NSSeen == false).Count();
                    }
                }
            }

            if (model.ZoneId != null)
            {
                proposals = proposals.Where(a => a.ZoneId == model.ZoneId);
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
            }
            if (model.OfficeId != null)
            {
                proposals = proposals.Where(a => a.OfficeId == model.OfficeId);
            }
            if (User.TypeUser != TypeUser.HO && User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM && User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.PKT)
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
            if (/*User.TypeUser != TypeUser.ASM &&*/ User.TypeUser != TypeUser.BM)
                return RedirectToAction("ListProposal");

            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null || (proposal.CVSeen && !proposal.BMEdit))
                return RedirectToAction("ListProposal");

            var model = new ProposeViewModel
            {
                Proposal = proposal,
                //SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content")
                SelectProposalTypes = new SelectList(_unitOfWork.ProposalTypeRepository.Get(a => a.Active), "Id", "Content"),

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
            proposal.ProposalTypeId = model.Proposal.ProposalTypeId;
            proposal.Active = true;
            //if (string.IsNullOrEmpty(proposal.CVName))
            //{
            //    proposal.CVName = _unitOfWork.UserRepository.GetQuery(a => a.ZoneIds.Contains("," + proposal.Zone.ShortCode + ",")).FirstOrDefault()?.Fullname;
            //}
            _unitOfWork.Save();
            return RedirectToAction("ListProposal", new { Result = "add" });
        }
        public ActionResult UpdateProposal(int pId, int? notice)
        {
            if (User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.HO)
                return RedirectToAction("ListProposal");
            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null)
                return RedirectToAction("ListProposal");
            var model = new ApproveViewModel
            {
                Proposal = proposal,
                SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content"),
                Notice = notice,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateProposal(ApproveViewModel model)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(model.Proposal.Id);
            if (proposal == null)
                return RedirectToAction("ListProposal");

            proposal.BMEdit = model.Proposal.BMEdit;
            if (proposal.CVFeedBack != model.Proposal.CVFeedBack || proposal.TypeApprove != model.Proposal.TypeApprove || proposal.TypeFaultId != model.Proposal.TypeFaultId)
            {
                proposal.NSSeen = false;
                proposal.CVSeen = true;
                proposal.CVFeedBack = model.Proposal.CVFeedBack;
                proposal.TypeApprove = model.Proposal.TypeApprove;
                proposal.TypeFaultId = model.Proposal.TypeFaultId;
                proposal.CVFbName = User.Fullname;
                _unitOfWork.Save();
            }
            return RedirectToAction("ListProposal", new { Result = "add", notice = model.Notice });
        }
        [HttpPost]
        public JsonResult UpdateSeen(int id)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(id);
            if (proposal == null)
                return Json(new { status = false });
            proposal.NSSeen = true;
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        [HttpPost]
        public JsonResult UpdateBMEdit(int id, bool isChecked)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(id);
            if (proposal == null)
                return Json(new { status = false });
            proposal.BMEdit = isChecked;
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
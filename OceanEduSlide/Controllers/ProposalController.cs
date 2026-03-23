using OceanEduSlide.DAL;
using OceanEduSlide.EnumHelpers;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Helpers;
using OceanEduSlide.Migrations;

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
            if ((User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM) || User.CDCM == "BAM")
                return HttpNotFound();
            var model = new ProposeViewModel
            {
                Proposal = new Proposal { UserId = User.Id, User = User },
                SelectProposalTypes = new SelectList(_unitOfWork.ProposalTypeRepository.Get(a => a.Active), "Id", "Content"),

            };
            if (User.TypeUser == TypeUser.ASM)
            {
                if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                {
                    var zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                    var offices = new List<Office>();
                    foreach (var zone in zones)
                    {
                        var officeAdd = _unitOfWork.OfficeRepository.GetQuery(a => a.ZoneId == zone.Id);
                        offices.AddRange(officeAdd);
                    }
                    model.SelectOffices = new SelectList(offices, "Id", "ShortName");
                    model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId != null && a.Office.ZoneId != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ","))
                        .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien + " - " + u.Office?.ShortName }), "Id", "FullNameWithCode");
                }
                else
                {
                    var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == User.ZoneId).FirstOrDefault();
                    if (zone != null)
                        model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.ZoneId == zone.Id), "Id", "ShortName");
                    model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId != null && a.Office.ZoneId != null && User.ZoneId == a.Office.ZoneId)
                       .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien + " - " + u.Office?.ShortName }), "Id", "FullNameWithCode");
                }
            }
            else
            {
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
            }
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
                var isPost = true;
                var office = _unitOfWork.OfficeRepository.GetById(model.Proposal.OfficeId);
                if (office == null)
                {
                    ModelState.AddModelError("", "Không tìm thấy chi nhánh, hãy liên hệ quản trị viên");
                    isPost = false;
                }

                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == office.ZoneId).FirstOrDefault();
                if (z == null)
                {
                    ModelState.AddModelError("", "Chi nhánh chưa có vùng, hãy liên hệ quản trị viên");
                    isPost = false;
                }
                if (isPost)
                {
                    model.Proposal.MaDeXuat = DateTime.Now.Day.ToString("00") + DateTime.Now.Month.ToString("00") + DateTime.Now.Year.ToString() + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + office.ShortCode;
                    model.Proposal.ZoneId = z.Id;
                    var idUser2 = model.Proposal.UserId2 ?? model.Proposal.UserId;
                    model.Proposal.User2 = _unitOfWork.UserRepository.GetById(idUser2);
                    _unitOfWork.ProposalRepository.Insert(model.Proposal);
                    _unitOfWork.Save();
                    return RedirectToAction("ListProposal", new { Result = "add" });

                }
                model.SelectProposalTypes = new SelectList(_unitOfWork.ProposalTypeRepository.Get(a => a.Active), "Id", "Content");

                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        var zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                        var offices = new List<Office>();
                        foreach (var zone in zones)
                        {
                            var officeAdd = _unitOfWork.OfficeRepository.GetQuery(a => a.ZoneId == zone.Id);
                            offices.AddRange(officeAdd);
                        }
                        model.SelectOffices = new SelectList(offices, "Id", "ShortName");
                        model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId != null && a.Office.ZoneId != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ","))
                            .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien + " - " + u.Office?.ShortName }), "Id", "FullNameWithCode");
                    }
                    else
                    {
                        var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Id == User.ZoneId).FirstOrDefault();
                        if (zone != null)
                            model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.ZoneId == zone.Id), "Id", "ShortName");
                        model.SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.Active && a.OfficeId != null && a.Office.ZoneId != null && User.ZoneId == a.Office.ZoneId)
                           .Select(u => new { Id = u.Id, FullNameWithCode = u.Fullname + " - " + u.MaNhanVien + " - " + u.Office?.ShortName }), "Id", "FullNameWithCode");
                    }
                }
                else
                {
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
                }
                return View(model);
                //var cvs = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.TypeUser == TypeUser.CV && a.ZoneIds.Contains(z.ShortCode));
                //model.Proposal.CVName = "";
                //foreach (var item in cvs)
                //{
                //    model.Proposal.CVName += item.Fullname + ", ";
                //}
                //model.Proposal.CVName = model.Proposal.CVName.Trim().Trim(',');
            }
            //ViewBag.TypeProposalList = Enum.GetValues(typeof(TypeProposal)).Cast<TypeProposal>().Select(d => new SelectListItem { Value = ((int)d).ToString(), Text = d.GetDisplayName() }).ToList();

            return RedirectToAction("ListProposal");
        }

        public ActionResult ListProposal(int? zoneId, int? officeId, string startDay, string endDay, int? Notice, string MaDeXuat, string Type, string CVName, string Fault, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            ViewBag.Result = Result;
            //var pageSize = 10;
            //ViewBag.PageSize = pageSize;
            var types = _unitOfWork.ProposalTypeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)).Select(a => a.Content).ToList();
            var faults = _unitOfWork.TypeFaultRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)).Select(a => a.Content).ToList();
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
            var proposals = _unitOfWork.ProposalRepository.GetQuery(a => DbFunctions.TruncateTime(a.CreateDate) >= DbFunctions.TruncateTime(StartDate) && DbFunctions.TruncateTime(a.CreateDate) <= DbFunctions.TruncateTime(EndDate)
            , q => q.OrderByDescending(a => a.CreateDate));

            if (!string.IsNullOrEmpty(MaDeXuat))
                proposals = proposals.Where(a => a.MaDeXuat.Contains(MaDeXuat));
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
                if (Notice == 2)
                    proposals = proposals.Where(a => a.CVSeen);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneIds != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ",") && a.Active && !a.CVSeen).Count();
            }
            else if (User.TypeUser == TypeUser.HO || User.TypeUser == TypeUser.PKT)
            {
                proposals = proposals.Where(a => a.Active);
                ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => a.Active && a.TypeApprove == TypeApprove.Type3).Count();
            }
            else
            {
                if (Notice == 2)
                    proposals = proposals.Where(a => a.Active == false);
                else if (Notice == 3)
                {
                    proposals = proposals.Where(a => a.NSSeen == false);
                }
                if (User.TypeUser == TypeUser.ASM)
                {
                    ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => a.Active && a.NSSeen == false && ((User.ZoneIds != null && User.ZoneIds.Contains("," + a.Office.Zone.ShortCode + ",")) || (a.Office.ZoneId != null && User.ZoneId == a.Office.ZoneId))).Count();
                }
                else
                {
                    if (User.TypeUser == TypeUser.BM)
                        ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => a.Active && a.NSSeen == false && (User.OfficeId == a.OfficeId || (User.OfficeIds != null && User.OfficeIds.Contains("," + a.OfficeId + ",")))).Count();
                }
            }
            if (User.TypeUser.HasValue && PermisstionHelper.ListTypeUserNhanVien_CN.Contains(User.TypeUser.Value))
            {
                proposals = proposals.Where(a => a.UserId2 == User.Id);
            }
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => (h.Year > StartDate.Year || (h.Year == StartDate.Year && h.Month >= StartDate.Month)) && (h.Year < EndDate.Year || (h.Year == EndDate.Year && h.Month <= EndDate.Month))).AsNoTracking();

            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == User.Id && (a.Year > StartDate.Year || (a.Year == StartDate.Year && a.Month >= StartDate.Month)) && (a.Year < EndDate.Year || (a.Year == EndDate.Year && a.Month <= EndDate.Month))).AsNoTracking();

            var zones = PermisstionHelper.GetZoneManagerPeriod(_unitOfWork, User, historyUsers);
            if (zones.Count() == 1)
            {
                zoneId = zones.First().Id;
            }

            var offices = PermisstionHelper.GetOfficeManagerPeriod(_unitOfWork, User, historyUsers, historyOffices, zoneId);
            var listOfficeId = offices.Select(a => a.Id).ToHashSet();
            if (officeId.HasValue && !listOfficeId.Contains(officeId.Value))
            {
                officeId = null;
            }
            if (offices.Count() == 1)
            {
                officeId = offices.First().Id;
            }
            if (officeId.HasValue)
                listOfficeId = listOfficeId.Where(a => a == officeId).ToHashSet();
            proposals = proposals.Where(a => listOfficeId.Contains(a.OfficeId));

            var listCV = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.TypeUser == TypeUser.CV && a.ZoneIds != null).AsNoTracking().ToList();
            var proposalItems = proposals.ToList().Select(x => new ProposalViewModel.ProposalItem
            {
                ListCVPhuTrach = listCV.Where(a => a.ZoneIds.Contains("," + x.Zone.ShortCode + ",")).Select(a => a.Fullname).ToList(),
                Proposal = x,
            });

            if (!string.IsNullOrEmpty(CVName))
            {
                proposalItems = proposalItems.Where(a => a.ListCVPhuTrach != null && a.ListCVPhuTrach.Contains(CVName));
            }

            var model = new ProposalViewModel
            {
                StartDay = startDay,
                EndDay = endDay,
                Zones = zones,
                Offices = offices,
                User = User,
                OfficeId = officeId,
                ZoneId = zoneId,
                Notice = Notice,
                MaDeXuat = MaDeXuat,
                ProposalTypes = types,
                Faults = faults,
                Type = Type,
                Fault = Fault,
                CVName = CVName,
                ProposalItems = proposalItems,
                SelectCVName = new SelectList(_unitOfWork.UserRepository.Get(a => a.Fullname != null && a.TypeUser == TypeUser.CV).Select(a => a.Fullname).Distinct())
            };
            return View(model);
        }
        public void ExportProposal(int? ZoneId, int? OfficeId, string startDay, string endDay, int? Notice, string MaDeXuat, string Type, string Fault)
        {
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
            var proposals = _unitOfWork.ProposalRepository.GetQuery(a => DbFunctions.TruncateTime(a.CreateDate) >= DbFunctions.TruncateTime(StartDate) && DbFunctions.TruncateTime(a.CreateDate) <= DbFunctions.TruncateTime(EndDate),
                q => q.OrderByDescending(a => a.CreateDate));
            if (!string.IsNullOrEmpty(MaDeXuat))
                proposals = proposals.Where(a => a.MaDeXuat.Contains(MaDeXuat));
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
                if (ZoneId == null)
                {
                    proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ",") && a.Active);
                }
                if (Notice == 2)
                    proposals = proposals.Where(a => a.CVSeen);
            }
            else if (User.TypeUser == TypeUser.HO || User.TypeUser == TypeUser.PKT)
            {
                proposals = proposals.Where(a => a.Active);
            }
            else
            {
                if (Notice == 2)
                    proposals = proposals.Where(a => a.Active == false);
                else if (Notice == 3)
                {
                    proposals = proposals.Where(a => a.NSSeen == false);
                }
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        if (ZoneId == null)
                        {
                            if (OfficeId == null)
                                proposals = proposals.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
                        }
                    }
                    else
                    {
                        ZoneId = User.ZoneId;
                        ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.ZoneId == a.ZoneId && a.Active && a.NSSeen == false).Count();
                    }
                }
                else if (User.TypeUser == TypeUser.BM)
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                    {
                        OfficeId = User.OfficeId;
                        if (User.TypeUser == TypeUser.BM)
                            ViewBag.NoticeCount = _unitOfWork.ProposalRepository.GetQuery(a => User.OfficeId == a.OfficeId && a.Active && a.NSSeen == false).Count();
                    }
                    else
                    {
                        if (OfficeId == null)
                            proposals = proposals.Where(a => User.OfficeIds.Contains("," + a.OfficeId + ","));
                    }
                }
                else
                {
                    OfficeId = User.OfficeId;
                    proposals = proposals.Where(a => a.UserId2 == User.Id);
                }
            }
            if (ZoneId != null)
            {
                proposals = proposals.Where(a => a.ZoneId == ZoneId);
            }
            if (OfficeId != null)
            {
                proposals = proposals.Where(a => a.OfficeId == OfficeId);
            }
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Mã đề xuất");
            dt.Columns.Add("CV PTS phụ trách");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Loại đề xuất");
            dt.Columns.Add("Nhân sự đề xuất");
            dt.Columns.Add("Nhân sự theo dõi");
            dt.Columns.Add("Ngày đề xuất");
            dt.Columns.Add("Nội dung và lý do đề xuất");
            dt.Columns.Add("Hồ sơ, tài liệu minh chứng kèm theo");
            dt.Columns.Add("Phản hồi của phòng tuyển sinh");
            dt.Columns.Add("Kết luận");
            dt.Columns.Add("Tổng hợp lỗi");
            dt.Columns.Add("Note");
            int stt = 1;
            foreach (var item in proposals)
            {
                dt.Rows.Add(stt, item.MaDeXuat, item.CVName, item.Office.ShortName, item.Zone.Name, item.ProposalType?.Content, item.User.Fullname, item.User2?.Fullname, item.CreateDate.ToString("dd/MM/yyyy"),
                   HtmlHelpers.RemoveHtml(null, item.Body), item.Url, HtmlHelpers.RemoveHtml(null, item.CVFeedBack), EnumExtensions.GetDisplayName(item.TypeApprove), item.TypeFault?.Content, item.Note);
                stt++;
            }
            var filename = $"danh-sach-de-xuat.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách đề xuất");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public ActionResult EditProposal(int pId)
        {
            if ((User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM) || User.CDCM == "BAM")
                return RedirectToAction("ListProposal");

            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null || proposal.UserId != User.Id || (proposal.CVSeen && !proposal.BMEdit))
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
            if (!proposal.CVSeen)
            {
                proposal.ProposalTypeId = model.Proposal.ProposalTypeId;
                proposal.Body = model.Proposal.Body;
            }
            proposal.Url = model.Proposal.Url;
            proposal.Active = true;
            //if (string.IsNullOrEmpty(proposal.CVName))
            //{
            //    proposal.CVName = _unitOfWork.UserRepository.GetQuery(a => a.ZoneIds.Contains("," + proposal.Zone.ShortCode + ",")).FirstOrDefault()?.Fullname;
            //}
            _unitOfWork.Save();
            return RedirectToAction("ListProposal", new { Result = "add" });
        }
        public ActionResult UpdateProposal(int pId, int Page, int? ZoneId, int? OfficeId, string StartDay, string EndDay, int? Notice, string MaDeXuat, string Type, string Fault, string CVName)
        {
            if (User.TypeUser != TypeUser.CV && User.TypeUser != TypeUser.HO)
                return RedirectToAction("ListProposal");
            var proposal = _unitOfWork.ProposalRepository.GetById(pId);
            if (proposal == null)
                return RedirectToAction("ListProposal");
            if (User.TypeUser == TypeUser.CV)
            {
                if (!User.ZoneIds.Contains("," + proposal.Zone.ShortCode + ","))
                    return RedirectToAction("ListProposal");
            }
            var model = new ApproveViewModel
            {
                Proposal = proposal,
                SelectFault = new SelectList(_unitOfWork.TypeFaultRepository.Get(a => a.Active), "Id", "Content"),
                Notice = Notice,
                Page = Page,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
                StartDay = StartDay,
                EndDay = EndDay,
                Fault = Fault,
                Type = Type,
                CVName = CVName

            };
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult UpdateProposal(ApproveViewModel model)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(model.Proposal.Id);
            if (proposal == null)
                return RedirectToAction("ListProposal");

            proposal.BMEdit = model.Proposal.BMEdit;
            proposal.Note = model.Proposal.Note;
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
            return RedirectToAction("ListProposal", new
            {
                Result = "add",
                Notice = model.Notice,
                Page = model.Page,
                ZoneId = model.ZoneId,
                OfficeId = model.OfficeId,
                StartDay = model.StartDay,
                EndDay = model.EndDay,
                Fault = model.Fault,
                Type = model.Type,
                CVName = model.CVName,
            });
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
        [HttpPost]
        public bool DeleteProposal(int proposalId = 0)
        {
            var proposal = _unitOfWork.ProposalRepository.GetById(proposalId);
            if (proposal == null)
            {
                return false;
            }
            _unitOfWork.ProposalRepository.Delete(proposal);
            _unitOfWork.Save();
            return true;
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
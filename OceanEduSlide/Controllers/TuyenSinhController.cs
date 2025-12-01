using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.OEDongBo;
using OceanEduSlide.ViewModels;
using OfficeOpenXml;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OceanEduSlide.EnumHelpers;
namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    [ForcePasswordChangeFilter]
    public class TuyenSinhController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private DongBoTuyenSinhEntities db = new DongBoTuyenSinhEntities();

        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();

        #region Kinh_Doanh
        public PartialViewResult Header()
        {
            var model = new HeaderViewModel
            {
                User = User,
                Categories1 = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type1),
                Categories2 = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type2),
                Categories3 = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type3),
            };
            //if (User.TypeUser == TypeUser.BM || User.TypeUser == TypeUser.EC || User.TypeUser == TypeUser.ALT || User.TypeUser == TypeUser.CM || User.TypeUser == TypeUser.SAB || User.TypeUser == TypeUser.TTL)
            //    model.Categories3 = model.Categories3.Where(a => ("," + a.Offices + ",").Contains("," + OfficeCode + ","));
            return PartialView(model);
        }
        public PartialViewResult GetCatgory(string MucLuc, int? Month)
        {
            var indexs = _unitOfWork.CategoryRepository
                .GetQuery(a => a.TypeCategory == TypeCategory.Type3)
                .Select(a => a.Index) // Chọn trường bạn cần, có thể thay 'Index' bằng tên khác
                .Distinct()
                .ToList();
            var catgories = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type3, q => q.OrderByDescending(a => a.Month));
            if (User.TypeUser == TypeUser.BM || User.TypeUser == TypeUser.EC || User.TypeUser == TypeUser.ALT || User.TypeUser == TypeUser.CM || User.TypeUser == TypeUser.SAB || User.TypeUser == TypeUser.TTL)
            {
                if (string.IsNullOrEmpty(User.OfficeIds))
                    catgories = catgories.Where(a => ("," + a.Offices + ",").Contains("," + OfficeCode + ","));

                else
                {
                    var listCode = User.OfficeNames.Split(',');
                    catgories = catgories.Where(a => listCode.Any(l => ("," + a.Offices + ",").Contains("," + l + ",")));
                }
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                {
                    var officeShortCodesAll = new List<string>();
                    var listZoneShortCode = User.ZoneIds.Trim(',').Split(',');
                    foreach (var shortCode in listZoneShortCode)
                    {
                        var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == shortCode).FirstOrDefault();
                        if (zone != null)
                        {
                            var officeShortCodes = _unitOfWork.OfficeRepository.GetQuery(o => o.ZoneId == zone.Id).Select(o => o.ShortCode).ToList();
                            officeShortCodesAll.AddRange(officeShortCodes);
                        }
                    }
                    catgories = catgories.Where(cat => officeShortCodesAll.Any(code => ("," + cat.Offices + ",").Contains("," + code + ",")));

                }
                else
                {
                    var zoneId = User.ZoneId;
                    var officeShortCodes = _unitOfWork.OfficeRepository.GetQuery(o => o.ZoneId == zoneId).Select(o => o.ShortCode).ToList();

                    // Lọc các Category có chứa ít nhất một ShortCode trong Offices
                    catgories = catgories.Where(cat => officeShortCodes.Any(code => ("," + cat.Offices + ",").Contains("," + code + ",")));
                }

            }
            if (Month != null)
            {
                catgories = catgories.Where(a => a.Month == Month);
            }
            if (!string.IsNullOrEmpty(MucLuc))
            {
                catgories = catgories.Where(a => a.Index == MucLuc);
            }
            var model = new CategoryViewModel
            {
                Categories = catgories,
                Month = Month,
                MucLuc = MucLuc,
                Indexs = indexs
            };

            return PartialView(model);
        }
        public ActionResult Revenue(int? page, int? ZoneId, int? Month, int? OfficeId, int? Year, int? UserType, int? TypeView, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            var pageNumber = page ?? 1;
            var model = new RevenueViewModel
            {
                //SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "Id", "ShortName"),
                Month = Month ?? DateTime.Now.Month,
                Year = Year ?? DateTime.Now.Year,
                OfficeId = OfficeId,
                ZoneId = ZoneId,
                User = User,
                UserType = UserType,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId))
            };
            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == model.Year && a.Month == model.Month
            && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.AEC
            && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == model.Month) || a.DayEnd.Value.Month != model.Month))),
            q => q.OrderBy(a => a.OfficeId == null ? int.MinValue : a.Office.ZoneId).ThenBy(a => a.OfficeId).ThenBy(a => a.Sort));
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == model.Month && h.Year == model.Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                if (model.ZoneId == null)
                {
                    model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                    if (model.OfficeId == null)
                        historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                }
            }
            else
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                        //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                        if (model.ZoneId == null)
                        {
                            model.Offices = model.Offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                            if (model.OfficeId == null)
                                historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                        }
                    }
                    else
                    {
                        model.ZoneId = User.ZoneId;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                        model.OfficeId = User.OfficeId;
                    else
                    {
                        model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId.ToString() + ",")));
                        if (model.OfficeId == null)
                        {
                            historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.OfficeIds.Contains("," + h.OfficeId + ",")));
                        }
                    }
                }
            }
            ViewBag.Result = Result;
            ViewBag.Year = DateTime.Now.Year;
            if (UserType != null)
            {
                historyUsers = historyUsers.Where(a => (int)a.TypeUser == UserType);
            }
            //else
            //{
            //    historyUsers = historyUsers.Where(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM);
            //}
            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == model.ZoneId));
                historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == model.ZoneId));
            }
            var (workingWeeks, currentWeek) = CalculateWeeks(model.Year ?? DateTime.Now.Year, model.Month ?? DateTime.Now.Month);

            ViewBag.WorkingWeeks = workingWeeks;
            ViewBag.CurrentWeek = currentWeek;

            if (model.OfficeId != null)
            {
                var office = _unitOfWork.OfficeRepository.GetById(model.OfficeId);
                if (office != null)
                {
                    model.RevenueOffice = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == model.OfficeId && a.Month == model.Month && a.Year == model.Year).FirstOrDefault();
                    model.RevenueOffice_BMs = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == model.OfficeId && a.Month == model.Month && a.Year == model.Year, q => q.OrderByDescending(a => a.CreateDate));
                    //var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == model.OfficeId && (a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM)).ToList();
                    //var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.OfficeId == model.OfficeId);
                    historyUsers = historyUsers.Where(a => a.OfficeId == model.OfficeId);
                    var userItems = historyUsers.ToList().Select(a => new RevenueViewModel.UserItem
                    {
                        HistoryUser = a,
                        //User = a.User,
                        RevenueUser_Month = _unitOfWork.RevenueUser_MonthRepository.GetQuery(p => p.HistoryUserId == a.Id && p.Month == model.Month && p.Year == model.Year).FirstOrDefault(),
                        RevenueUser_Month_BMs = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(p => p.HistoryUserId == a.Id && p.Month == model.Month && p.Year == model.Year, q => q.OrderByDescending(p => p.CreateDate)),
                        RevenueUser_Month_BM_real = _unitOfWork.RevenueUser_Month_BM_realRepository.GetQuery(p => p.HistoryUserId == a.Id && p.Month == model.Month && p.Year == model.Year, q => q.OrderByDescending(p => p.CreateDate)).FirstOrDefault(),
                        RevenueUser_Weeks = _unitOfWork.RevenueUser_WeekRepository.GetQuery(p => p.HistoryUserId == a.Id && p.Month == model.Month && p.Year == model.Year, q => q.OrderByDescending(p => p.CreateDate)),
                        RevenueUser_Week_Reals = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(p => p.HistoryUserId == a.Id && p.Month == model.Month && p.Year == model.Year, q => q.OrderByDescending(p => p.CreateDate)),
                        Debt = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.UserId == a.UserId && (q.Year < model.Year || (q.Year == model.Year && q.Month < model.Month))
                        && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).GroupBy(q => q.DebtId ?? q.Id)
                        .Select(g => g.OrderByDescending(q => q.CreateDate).FirstOrDefault()).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0
                    });
                    model.UserItems = userItems.ToPagedList(pageNumber, 20);
                }
                return View(model);
            }
            if (TypeView == null)
                TypeView = 1;
            model.TypeView = TypeView;
            //model.RevenueOffices = revenueOffices;
            var officeItems = model.Offices.ToList().Select(a => new RevenueViewModel.OfficeItem
            {
                Office = a,
                RevenueOffice = _unitOfWork.RevenueOfficeRepository.GetQuery(p => p.OfficeId == a.Id && p.Month == model.Month && p.Year == model.Year).FirstOrDefault(),
                RevenueOffice_BMs = _unitOfWork.RevenueOffice_BMRepository.GetQuery(p => p.OfficeId == a.Id && p.Month == model.Month && p.Year == model.Year, q => q.OrderByDescending(p => p.CreateDate)),
            });
            if (TypeView == 2)
            {
                // Bước 1: Xác định phân trang
                int pageSize = 20;

                // Bước 2: Truy vấn danh sách người dùng đầy đủ
                var listHistoryUsers = historyUsers
                    .ToList();
                //var allUserIds = listHistoryUsers.Select(u => u.UserId).Distinct().ToList();

                // Bước 3: Phân trang danh sách người dùng
                var pagedHistoryUsers = historyUsers
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Bước 4: Lấy danh sách userId cần xử lý
                var userIds = pagedHistoryUsers.Select(u => u.Id).ToList();
                var pagedUserIds = pagedHistoryUsers.Select(u => u.UserId).Distinct().ToList();


                // Bước 5: Truy vấn dữ liệu liên quan theo userIds
                var revenueMonthsList = _unitOfWork.RevenueUser_MonthRepository
                    .GetQuery(p => userIds.Contains(p.HistoryUserId ?? 0) && p.Month == model.Month && p.Year == model.Year)
                    .AsNoTracking()
                    .ToList();

                var revenueMonthDict = revenueMonthsList
                    .Where(p => p.HistoryUserId.HasValue)
                    .GroupBy(p => p.HistoryUserId.Value)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault());

                var revenueMonthBMsDict = _unitOfWork.RevenueUser_Month_BMRepository
                    .GetQuery(p => userIds.Contains(p.HistoryUserId ?? 0) && p.Month == model.Month && p.Year == model.Year)
                    .AsNoTracking()
                    .GroupBy(p => p.HistoryUserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreateDate).ToList());

                var revenueMonthBMRealsDict = _unitOfWork.RevenueUser_Month_BM_realRepository
                    .GetQuery(p => userIds.Contains(p.HistoryUserId ?? 0) && p.Month == model.Month && p.Year == model.Year)
                    .AsNoTracking()
                    .GroupBy(p => p.HistoryUserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreateDate).FirstOrDefault());

                var revenueWeeksDict = _unitOfWork.RevenueUser_WeekRepository
                    .GetQuery(p => userIds.Contains(p.HistoryUserId ?? 0) && p.Month == model.Month && p.Year == model.Year)
                    .AsNoTracking()
                    .GroupBy(p => p.HistoryUserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreateDate).ToList());

                var revenueWeekRealsDict = _unitOfWork.RevenueUser_Week_RealRepository
                    .GetQuery(p => userIds.Contains(p.HistoryUserId ?? 0) && p.Month == model.Month && p.Year == model.Year)
                    .AsNoTracking()
                    .GroupBy(p => p.HistoryUserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreateDate).ToList());

                //var debtsList = _unitOfWork.DebtRepository
                //    .GetQuery(q => q.Active && userIds.Contains(q.UserId) &&
                //        (q.Year < model.Year || (q.Year == model.Year && q.Month < model.Month)) &&
                //        (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3))
                //    .AsNoTracking()
                //    .GroupBy(q => new { q.UserId, DebtKey = q.DebtId ?? q.Id })
                //    .Select(g => g.OrderByDescending(q => q.CreateDate).FirstOrDefault())
                //    .ToList();
                //var debtDict = debtsList
                //    .GroupBy(q => q.UserId)
                //    .ToDictionary(g => g.Key, g => g.Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0);
                var debtsList = _unitOfWork.DebtRepository
    .GetQuery(q => q.Active && pagedUserIds.Contains(q.UserId) &&
        (q.Year < model.Year || (q.Year == model.Year && q.Month < model.Month)) &&
        (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3))
    .AsNoTracking()
    .GroupBy(q => new { q.UserId, DebtKey = q.DebtId ?? q.Id })
    .Select(g => g.OrderByDescending(q => q.CreateDate).FirstOrDefault())
    .ToList();

                var debtDict = debtsList
                    .GroupBy(q => q.UserId)
                    .ToDictionary(g => g.Key, g => g.Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0);

                // Bước 6: Tạo danh sách kết quả
                var userItems = new List<RevenueViewModel.UserItem>();

                foreach (var a in pagedHistoryUsers)
                {
                    revenueMonthDict.TryGetValue(a.Id, out var month);
                    revenueMonthBMsDict.TryGetValue(a.Id, out var bmList);
                    revenueMonthBMRealsDict.TryGetValue(a.Id, out var bmReal);
                    revenueWeeksDict.TryGetValue(a.Id, out var weekList);
                    revenueWeekRealsDict.TryGetValue(a.Id, out var weekRealList);
                    debtDict.TryGetValue(a.UserId, out var debt);

                    userItems.Add(new RevenueViewModel.UserItem
                    {
                        HistoryUser = a,
                        RevenueUser_Month = month,
                        RevenueUser_Month_BMs = bmList ?? new List<RevenueUser_Month_BM>(),
                        RevenueUser_Month_BM_real = bmReal,
                        RevenueUser_Weeks = weekList ?? new List<RevenueUser_Week>(),
                        RevenueUser_Week_Reals = weekRealList ?? new List<RevenueUser_Week_Real>(),
                        Debt = debt
                    });
                }

                // Bước 7: Gán vào model với phân trang
                model.UserItems = new StaticPagedList<RevenueViewModel.UserItem>(
                    userItems, pageNumber, pageSize, listHistoryUsers.Count
                );
            }

            model.OfficeItems = officeItems;
            return View("RevenueManager", model);
        }
        public void ExportRevenueCN(int Year, int Month, int? ZoneId)
        {
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == Month && h.Year == Year).Select(h => new
            {
                h.OfficeId,
                ZoneShortCode = h.Zone.ShortCode,
                h.ZoneId
            });

            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId));
            if (User.TypeUser == TypeUser.CV)
            {
                if (ZoneId == null)
                {
                    offices = offices.Where(o => o.ZoneId != null && User.ZoneIds.Contains("," + o.Zone.ShortCode + ","));
                }
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        if (ZoneId == null)
                        {
                            //offices = offices.Where(o => o.ZoneId != null && User.ZoneIds.Contains("," + o.Zone.ShortCode + ","));
                            offices = offices.Where(o => historyOffices.Any(h => h.OfficeId == o.Id && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
                        }
                    }
                    else
                    {
                        ZoneId = User.ZoneId;
                    }
                }
                else if (User.OfficeIds != null)
                {
                    //offices = offices.Where(a => User.OfficeIds.Contains("," + a.Id + ","));
                    offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && User.OfficeIds.Contains("," + h.OfficeId + ",")));
                }
            }
            if (ZoneId != null)
            {
                //offices = offices.Where(a => a.ZoneId == ZoneId);
                offices = offices.Where(a => historyOffices.Any(h => h.OfficeId == a.Id && h.ZoneId == ZoneId));
            }
            var dt = new DataTable();
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chỉ tiêu doanh số tuyển sinh");
            dt.Columns.Add("Chỉ tiêu doanh số học vụ");
            dt.Columns.Add("Chỉ tiêu doanh số kế toán");
            dt.Columns.Add("Cam kết hoàn thành doanh số");
            dt.Columns.Add("Phân bổ DS theo ghi danh mới");
            dt.Columns.Add("Phân bổ DS theo tái phí");
            dt.Columns.Add("Phân bổ DS theo SAB");
            foreach (var office in offices)
            {
                var revenueHO = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == Month && a.Year == Year).FirstOrDefault();
                var revenueOffice = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == Month && a.Year == Year).FirstOrDefault();
                dt.Rows.Add(office.Zone?.Name, office.ShortName, Month, revenueHO?.Target_TS, revenueHO?.Target_HV, revenueHO?.Target_SAB, revenueOffice?.TargetBM_TS, revenueOffice?.TargetBM_New, revenueOffice?.TargetBM_HV, revenueOffice?.TargetBM_SAB);
            }
            var filename = $"phan-bo-DS-tong-quan.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách phân bổ DS tổng quan");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportRevenueNV(int Year, int Month, int? ZoneId, int? OfficeId, int? UserType)
        {
            var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == Month && h.Year == Year)
                .AsNoTracking()
                .Select(h => new
                {
                    h.OfficeId,
                    h.ZoneId,
                    ZoneShortCode = h.Zone.ShortCode,
                    ZoneName = h.Zone.Name,
                    OfficeName = h.Office.ShortName
                }).ToList();

            var officeIdToZone = historyOffices.ToDictionary(x => x.OfficeId);

            var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a =>
                a.Active &&
                a.Year == Year &&
                a.Month == Month &&
                a.TypeUser != TypeUser.ASM &&
                a.TypeUser != TypeUser.HO &&
                a.TypeUser != TypeUser.CV &&
                a.TypeUser != TypeUser.PKT &&
                a.TypeUser != TypeUser.AEC &&
                (a.DayEnd == null || (a.DayEnd.Value.Month != Month || (a.DayEnd.Value.Day != 1))),
                q => q.OrderBy(a => a.OfficeId == null ? int.MinValue : a.Office.ZoneId).ThenBy(a => a.OfficeId).ThenBy(a => a.Sort)
            ).AsNoTracking().ToList();

            // Filter theo quyền người dùng
            if (User.TypeUser == TypeUser.CV)
            {
                if (ZoneId == null && OfficeId == null)
                {
                    historyUsers = historyUsers
                        .Where(a => a.OfficeId != null &&
                                    officeIdToZone.TryGetValue(a.OfficeId.Value, out var h) &&
                                    User.ZoneIds.Contains("," + h.ZoneShortCode + ","))
                        .ToList();
                }
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                if (User.TypeUser == TypeUser.ASM)
                {
                    if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
                    {
                        if (ZoneId == null && OfficeId == null)
                        {
                            historyUsers = historyUsers
                                .Where(a => a.OfficeId != null &&
                                            officeIdToZone.TryGetValue(a.OfficeId.Value, out var h) &&
                                            User.ZoneIds.Contains("," + h.ZoneShortCode + ","))
                                .ToList();
                        }
                    }
                    else
                    {
                        ZoneId = User.ZoneId;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(User.OfficeIds))
                        OfficeId = User.OfficeId;
                    else if (OfficeId == null)
                    {
                        historyUsers = historyUsers
                            .Where(a => a.OfficeId != null &&
                                        User.OfficeIds.Contains("," + a.OfficeId + ","))
                            .ToList();
                    }
                }
            }

            if (UserType != null)
            {
                historyUsers = historyUsers.Where(a => (int)a.TypeUser == UserType).ToList();
            }

            if (ZoneId != null)
            {
                historyUsers = historyUsers
                    .Where(a => a.OfficeId != null &&
                                officeIdToZone.TryGetValue(a.OfficeId.Value, out var h) &&
                                h.ZoneId == ZoneId)
                    .ToList();
            }

            if (OfficeId != null)
            {
                historyUsers = historyUsers
                    .Where(a => a.OfficeId == OfficeId)
                    .ToList();
            }

            var userIds = historyUsers.Select(u => u.Id).ToList();
            var actualUserIds = historyUsers.Select(u => u.UserId).ToList();

            // Load dữ liệu batch
            var targetMonths = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.HistoryUserId != null &&
                userIds.Contains(a.HistoryUserId.Value) && a.Month == Month && a.Year == Year)
                .AsNoTracking().ToDictionary(a => a.HistoryUserId);

            var camKetMonths = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.HistoryUserId != null &&
                userIds.Contains(a.HistoryUserId.Value) && a.Month == Month && a.Year == Year)
                .AsNoTracking()
                .GroupBy(a => a.HistoryUserId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreateDate).First());

            var allDebts = _unitOfWork.DebtRepository.GetQuery(q =>
                q.Active &&
                actualUserIds.Contains(q.UserId) &&
                (q.Year < Year || (q.Year == Year && q.Month < Month)) &&
                (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3))
                .AsNoTracking()
                .ToList();

            var debts = allDebts
                .GroupBy(q => new { q.UserId, DebtId = q.DebtId ?? q.Id })
                .Select(g => g.OrderByDescending(x => x.CreateDate).First())
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalMoney - x.DownMoney));

            var revenueWeeks = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.HistoryUserId != null &&
                userIds.Contains(a.HistoryUserId.Value) && a.Month == Month && a.Year == Year)
                .AsNoTracking().ToList();

            var revenueWeekReals = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.HistoryUserId != null &&
                userIds.Contains(a.HistoryUserId.Value) && a.Month == Month && a.Year == Year)
                .AsNoTracking().ToList();

            var dt = new DataTable();
            var (workingWeeks, currentWeek) = CalculateWeeks(Year, Month);

            string[] fixedHeaders = new[] {
                "Vùng", "Chi nhánh", "Tháng", "Họ tên nhân sự", "Mã nhân viên", "CDCM",
                "Ngày vào làm", "Ngày nghỉ/điều chuyển", "Trạng thái", "Chỉ tiêu doanh số",
                "Cam kết HT doanh số (gồm dự thu cũ)", "Dự thu tháng trước", "Số tiền thực chạy"
            };
            string[] subHeaders = new[] { "Dự kiến", "Thực tế" };

            foreach (var header in fixedHeaders) dt.Columns.Add(header);
            for (int week = 1; week <= workingWeeks; week++)
                foreach (var sub in subHeaders)
                    dt.Columns.Add($"Tuần {week} - {sub}");

            foreach (var item in historyUsers)
            {
                var row = dt.NewRow();

                var zoneInfo = item.OfficeId.HasValue && officeIdToZone.TryGetValue(item.OfficeId.Value, out var h) ? h : null;
                row["Vùng"] = zoneInfo?.ZoneName;
                row["Chi nhánh"] = zoneInfo?.OfficeName;
                row["Tháng"] = Month;
                row["Họ tên nhân sự"] = item.User.Fullname;
                row["Mã nhân viên"] = item.User.MaNhanVien;
                row["CDCM"] = item.CDCM;
                row["Ngày vào làm"] = item.DayStart.ToString("dd/MM/yyyy");
                row["Ngày nghỉ/điều chuyển"] = item.DayEnd?.ToString("dd/MM/yyyy");
                row["Trạng thái"] = EnumExtensions.GetDisplayName(item.Status);

                //var targetMonth = targetMonths.GetValueOrDefault(item.Id);
                //var camKetMonth = camKetMonths.GetValueOrDefault(item.Id);
                //var debt = debts.GetValueOrDefault(item.UserId);
                var targetMonth = targetMonths.ContainsKey(item.Id) ? targetMonths[item.Id] : null;
                var camKetMonth = camKetMonths.ContainsKey(item.Id) ? camKetMonths[item.Id] : null;
                var userId = item.UserId;
                var debt = debts.ContainsKey(userId) ? debts[userId] : 0;

                decimal soTienThucChay = camKetMonth?.TargetBM - debt ?? 0;

                row["Chỉ tiêu doanh số"] = targetMonth?.Target;
                row["Cam kết HT doanh số (gồm dự thu cũ)"] = camKetMonth?.TargetBM;
                row["Dự thu tháng trước"] = debt;
                row["Số tiền thực chạy"] = soTienThucChay;

                for (int week = 1; week <= workingWeeks; week++)
                {
                    var targetWeek = revenueWeeks.FirstOrDefault(a => a.HistoryUserId == item.Id && (int)a.WeekNumber == week);
                    var realWeek = revenueWeekReals.FirstOrDefault(a => a.HistoryUserId == item.Id && (int)a.WeekNumber == week);

                    row[$"Tuần {week} - Dự kiến"] = targetWeek?.TargetBM;
                    row[$"Tuần {week} - Thực tế"] = realWeek?.TargetBM;
                }

                dt.Rows.Add(row);
            }

            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Danh sách phân bổ DS chi tiết");

                int row1 = 1, row2 = 2, col = 1;
                foreach (var header in fixedHeaders)
                {
                    ws.Cells[row1, col, row2, col].Merge = true;
                    ws.Cells[row1, col].Value = header;
                    col++;
                }

                for (int week = 1; week <= workingWeeks; week++)
                {
                    int startCol = col;
                    foreach (var sub in subHeaders)
                        ws.Cells[row2, col++].Value = sub;

                    ws.Cells[row1, startCol, row1, col - 1].Merge = true;
                    ws.Cells[row1, startCol].Value = $"Tuần {week}";
                }

                ws.Cells[3, 1].LoadFromDataTable(dt, false);
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename=phan-bo-DS-chi-tiet.xlsx");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        //public void ExportRevenueNV(int Year, int Month, int? ZoneId, int? OfficeId, int? UserType)
        //{
        //    var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery(h => h.Month == Month && h.Year == Year).Select(h => new
        //    {
        //        h.OfficeId,
        //        ZoneShortCode = h.Zone.ShortCode,
        //        h.ZoneId
        //    });
        //    var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery(a => historyOffices.Any(h => h.OfficeId == a.OfficeId) && a.Active && a.Year == Year && a.Month == Month
        //    && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.AEC
        //    && (a.DayEnd == null || (a.DayEnd != null && ((a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == Month) || a.DayEnd.Value.Month != Month))),
        //    q => q.OrderBy(a => a.OfficeId == null ? int.MinValue : a.Office.ZoneId).ThenBy(a => a.OfficeId).ThenBy(a => a.Sort));
        //    if (User.TypeUser == TypeUser.CV)
        //    {
        //        if (ZoneId == null && OfficeId == null)
        //            historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
        //    }
        //    else if (User.TypeUser != TypeUser.HO)
        //    {
        //        if (User.TypeUser == TypeUser.ASM)
        //        {
        //            if (!string.IsNullOrEmpty(User.ZoneIds) && User.ZoneIds.Length > 2)
        //            {
        //                if (ZoneId == null && OfficeId == null)
        //                    historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.ZoneIds.Contains("," + h.ZoneShortCode + ",")));
        //                var hs = historyUsers.Count();
        //            }
        //            else
        //            {
        //                ZoneId = User.ZoneId;
        //            }
        //        }

        //        else
        //        {
        //            if (string.IsNullOrEmpty(User.OfficeIds))
        //                OfficeId = User.OfficeId;
        //            else if (OfficeId == null)
        //            {
        //                historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && User.OfficeIds.Contains("," + h.OfficeId + ",")));
        //            }
        //        }
        //    }
        //    if (UserType != null)
        //    {
        //        historyUsers = historyUsers.Where(a => (int)a.TypeUser == UserType);
        //    }
        //    //else
        //    //{
        //    //    historyUsers = historyUsers.Where(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.BM);
        //    //}
        //    if (ZoneId != null)
        //    {
        //        historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && h.ZoneId == ZoneId));
        //    }
        //    if (OfficeId != null)
        //    {
        //        historyUsers = historyUsers.Where(a => historyOffices.Any(h => h.OfficeId == a.OfficeId && h.OfficeId == OfficeId));
        //    }
        //    var dt = new DataTable();
        //    var (workingWeeks, currentWeek) = CalculateWeeks(Year, Month);

        //    // Các cột cố định
        //    string[] fixedHeaders = new[] { "Vùng", "Chi nhánh", "Tháng", "Họ tên nhân sự","Mã nhân viên", "CDCM", "Ngày vào làm", "Ngày nghỉ/điều chuyển", "Trạng thái", "Chỉ tiêu doanh số", "Cam kết HT doanh số (gồm dự thu cũ)",
        //    "Dự thu tháng trước","Số tiền thực chạy"};

        //    // Các loại dữ liệu trong mỗi tuần
        //    string[] subHeaders = new[] { "Dự kiến", "Thực tế" };
        //    foreach (var header in fixedHeaders)
        //        dt.Columns.Add(header);

        //    // 2. Thêm cột động theo tuần
        //    for (int week = 1; week <= workingWeeks; week++)
        //    {
        //        foreach (var sub in subHeaders)
        //        {
        //            dt.Columns.Add($"Tuần {week} - {sub}");
        //        }
        //    }
        //    //foreach (var item in historyUsers)
        //    //{
        //    //    var revenueHO = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == Month && a.Year == Year).FirstOrDefault();
        //    //    var revenueOffice = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == Month && a.Year == Year).FirstOrDefault();
        //    //    dt.Rows.Add(office.Zone?.Name, office.ShortName, Month, revenueHO?.Target_TS, revenueHO?.Target_HV, revenueHO?.Target_SAB, revenueOffice?.TargetBM_TS, revenueOffice?.TargetBM_New, revenueOffice?.TargetBM_HV, revenueOffice?.TargetBM_SAB);
        //    //}
        //    var filename = $"phan-bo-DS-chi-tiet.xlsx";
        //    using (var pck = new ExcelPackage())
        //    {
        //        //Create the worksheet
        //        var ws = pck.Workbook.Worksheets.Add("Danh sách phân bổ DS chi tiết");
        //        int row1 = 1, row2 = 2, col = 1;
        //        foreach (var header in fixedHeaders)
        //        {
        //            ws.Cells[row1, col, row2, col].Merge = true;
        //            ws.Cells[row1, col].Value = header;
        //            col++;
        //        }
        //        for (int week = 1; week <= workingWeeks; week++)
        //        {
        //            int startCol = col;

        //            foreach (var sub in subHeaders)
        //            {
        //                ws.Cells[row2, col].Value = sub;
        //                col++;
        //            }

        //            // Merge dòng 1 cho tuần
        //            ws.Cells[row1, startCol, row1, col - 1].Merge = true;
        //            ws.Cells[row1, startCol].Value = $"Tuần {week}";
        //        }
        //        foreach (var item in historyUsers)
        //        {
        //            var targetMonth = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == Month && a.Year == Year).FirstOrDefault();
        //            var camKetMonth = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == Month && a.Year == Year, q => q.OrderByDescending(a => a.CreateDate)).FirstOrDefault();
        //            var debt = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.UserId == item.UserId && (q.Year < Year || (q.Year == Year && q.Month < Month))
        //                && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).GroupBy(q => q.DebtId ?? q.Id)
        //                .Select(g => g.OrderByDescending(q => q.CreateDate).FirstOrDefault()).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0;
        //            decimal soTienThucChay = 0;

        //            if (camKetMonth != null)
        //            {
        //                soTienThucChay = camKetMonth.TargetBM - debt;
        //            }
        //            //dt.Rows.Add(item.Zone?.Name, item.Office?.ShortName, Month,item.User.Fullname,item.User.MaNhanVien,item.DayStart.ToString("dd/MM/yyyy"), item.DayEnd?.ToString("dd/MM/yyyy"),
        //            //    EnumExtensions.GetDisplayName(item.Status), targetMonth?.Target, camKetMonth?.TargetBM, debt, soTienThucChay);
        //            var row = dt.NewRow();
        //            row["Vùng"] = item.Zone?.Name;
        //            row["Chi nhánh"] = item.Office?.ShortName;
        //            row["Tháng"] = Month;
        //            row["Họ tên nhân sự"] = item.User.Fullname;
        //            row["Mã nhân viên"] = item.User.MaNhanVien;
        //            row["CDCM"] = item.CDCM;
        //            row["Ngày vào làm"] = item.DayStart.ToString("dd/MM/yyyy");
        //            row["Ngày nghỉ/điều chuyển"] = item.DayEnd?.ToString("dd/MM/yyyy");
        //            row["Trạng thái"] = EnumExtensions.GetDisplayName(item.Status);
        //            row["Chỉ tiêu doanh số"] = targetMonth?.Target;
        //            row["Cam kết HT doanh số (gồm dự thu cũ)"] = camKetMonth?.TargetBM;
        //            row["Dự thu tháng trước"] = debt;
        //            row["Số tiền thực chạy"] = soTienThucChay;
        //            for (int week = 1; week <= workingWeeks; week++)
        //            {
        //                var targetWeek = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.Month == Month && a.Year == Year && (int)a.WeekNumber == week && a.HistoryUserId == item.Id).FirstOrDefault();
        //                var revenueWeekReal = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.Month == Month && a.Year == Year && (int)a.WeekNumber == week && a.HistoryUserId == item.Id).FirstOrDefault();
        //                row[$"Tuần {week} - Dự kiến"] = targetWeek?.TargetBM;
        //                row[$"Tuần {week} - Thực tế"] = revenueWeekReal?.TargetBM;
        //            }
        //            dt.Rows.Add(row);
        //        }
        //        ws.Cells[3, 1].LoadFromDataTable(dt, false);
        //        //Write it back to the client
        //        Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        //        Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
        //        Response.BinaryWrite(pck.GetAsByteArray());
        //    }
        //}

        public ActionResult ChangeDataRevenueMonth(int month)
        {
            var revenues = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in revenues)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueMonth");
        }
        public ActionResult ChangeDataRevenueMonth_BM(int month)
        {
            var revenues = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in revenues)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueMonth_BM");
        }
        public ActionResult ChangeDataRevenueWeek(int month)
        {
            var revenues = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in revenues)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueWeek");
        }
        public ActionResult ChangeDataRevenueWeekReal(int month)
        {
            var revenues = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in revenues)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueWeekReal");
        }
        public ActionResult ChangeDataRevenueDay(int month)
        {
            var revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in revenues)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueDay");
        }

        public ActionResult ChangeDataRevenueReportData(int month)
        {
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Month == month && a.Year == 2025 && a.UserId != null);
            var histories = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.Year == 2025);
            foreach (var r in reportDatas)
            {
                var history = histories.FirstOrDefault(a => a.UserId == r.UserId && a.TypeUser == r.User.TypeUser && a.OfficeId == r.User.OfficeId);
                if (history != null)
                    r.HistoryUserId = history.Id;
            }
            _unitOfWork.Save();
            return Content("Thành công - ChangeDataRevenueReportData");
        }
        public static (int, int) CalculateWeeks(int year, int month)
        {
            DateTime firstDay = new DateTime(year, month, 1);
            DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
            DateTime today = DateTime.Now;

            int workingWeeks = 1; // Bắt đầu từ tuần 1
            int currentWeek = 0;

            DateTime currentDay = firstDay;

            // Duyệt từng ngày trong tháng
            while (currentDay <= lastDay)
            {
                // Nếu là thứ Hai và không phải ngày đầu tháng => bắt đầu tuần mới
                if (currentDay.DayOfWeek == DayOfWeek.Monday && currentDay != firstDay)
                {
                    workingWeeks++;
                }

                // Nếu ngày hiện tại trùng với `today`, cập nhật `currentWeek`
                if (currentDay.Year == today.Year && currentDay.Month == today.Month && currentDay.Day == today.Day)
                {
                    currentWeek = workingWeeks;
                }

                currentDay = currentDay.AddDays(1);
            }

            // Nếu hôm nay không nằm trong tháng xét, gán `currentWeek = 0`
            if (today.Month != month || today.Year != year)
            {
                currentWeek = 0;
            }

            return (workingWeeks, currentWeek);
        }
        //public ActionResult RevenueOffice()
        //{
        //    if (User.TypeUser != TypeUser.HO)
        //        return RedirectToAction("Index","Home");
        //    var model = new RevenueOfficeViewModel
        //    {
        //        SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "Id", "ShortName"),
        //        RevenueOffice = new RevenueOffice { Active = true },
        //    };
        //    ViewBag.Year = DateTime.Now.Year;
        //    return View(model);
        //}
        //[HttpPost]
        //public ActionResult RevenueOffice(RevenueOfficeViewModel model)
        //{
        //    if (User.TypeUser != TypeUser.HO)
        //        return RedirectToAction("Index","Home");
        //    if (ModelState.IsValid)
        //    {
        //        _unitOfWork.RevenueOfficeRepository.Insert(model.RevenueOffice);
        //        _unitOfWork.Save();
        //        return RedirectToAction("ListRevenueOffice", new { result = "add" });
        //    }
        //    ViewBag.Year = DateTime.Now.Year;
        //    return View(model);
        //}

        public ActionResult RevenueOffice_BM(int officeId, int month, int year)
        {
            var office = _unitOfWork.OfficeRepository.GetById(officeId);
            if (office == null || (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM))
                return HttpNotFound();
            var model = new RevenueOffice_BMViewModel
            {
                RevenueOffice = new RevenueOffice_BM { Active = true, OfficeId = officeId, Month = month, Year = year },
                OfficeName = office.Name,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult RevenueOffice_BM(RevenueOffice_BMViewModel model)
        {
            model.RevenueOffice.TargetBM_New = Convert.ToDecimal(model.TargetBM_New.Replace(",", ""));
            model.RevenueOffice.TargetBM_HV = Convert.ToDecimal(model.TargetBM_HV.Replace(",", ""));
            model.RevenueOffice.TargetBM_SAB = Convert.ToDecimal(model.TargetBM_SAB.Replace(",", ""));
            model.RevenueOffice.TargetBM_TS = Convert.ToDecimal(model.TargetBM_TS.Replace(",", ""));

            _unitOfWork.RevenueOffice_BMRepository.Insert(model.RevenueOffice);
            _unitOfWork.Save();
            return RedirectToAction("Revenue", new { Result = "add", Month = model.RevenueOffice.Month, Year = model.RevenueOffice.Year, OfficeId = model.RevenueOffice.OfficeId });
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "ShortName"),
                RevenueOffices = revenueOffices.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
            };
            return View(model);
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueMonth(int year, int month, int historyUserId, decimal targetBM)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            if (historyUser == null)
                return Json(new { status = false });
            if (historyUser.Status != StatusUser.Active)
                return Json(new { status = false, msg = "Người dùng này đã được điều chuyển/ nghỉ việc" });
            var revenue = new RevenueUser_Month_BM
            {
                Year = year,
                Month = month,
                UserId = historyUser.UserId,
                HistoryUserId = historyUserId,
                TargetBM = targetBM,
                Active = true,
            };
            _unitOfWork.RevenueUser_Month_BMRepository.Insert(revenue);
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueMonthReal(int year, int month, int userId, decimal targetBM)
        {
            var revenue = new Models.RevenueUser_Month_BM_real
            {
                Year = year,
                Month = month,
                UserId = userId,
                TargetBM = targetBM,
                Active = true,

            };
            _unitOfWork.RevenueUser_Month_BM_realRepository.Insert(revenue);
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        [HttpPost]
        public JsonResult AddOrUpdateRevenueWeek(int year, int month, int historyUserId, decimal? targetBM, int weekNumber)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            if (historyUser == null)
                return Json(new { status = false });
            if (historyUser.Status != StatusUser.Active)
                return Json(new { status = false, msg = "Người dùng này đã được điều chuyển/ nghỉ việc" });
            var revenue = new RevenueUser_Week
            {
                Year = year,
                Month = month,
                UserId = historyUser.UserId,
                HistoryUserId = historyUserId,
                TargetBM = targetBM ?? 0,
                Active = true,
            };
            switch (weekNumber)
            {
                case 1:
                    revenue.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    revenue.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    revenue.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    revenue.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    revenue.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    revenue.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            _unitOfWork.RevenueUser_WeekRepository.Insert(revenue);
            _unitOfWork.Save();
            return Json(new { status = true });
        }
        public PartialViewResult LoadHistoryRevenueUser_Month(int year, int month, int historyUserId)
        {
            var historyUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId);
            var model = new LoadHistoryRevenueUser_MonthViewModel
            {
                Year = year,
                Month = month,
                User = historyUser.User,
                HistoryUser = _unitOfWork.HistoryUserRepository.GetById(historyUserId),
                Revenues = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Year == year && a.Month == month && a.HistoryUserId == historyUserId, q => q.OrderBy(a => a.CreateDate)),
            };
            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueUser_Week(int year, int month, int userId, int weekNumber)
        {
            var model = new LoadHistoryRevenueUser_WeekViewModel
            {
                Year = year,
                Month = month,
                User = _unitOfWork.UserRepository.GetById(userId),
                Revenues = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.Year == year && a.Month == month && a.UserId == userId && (int)a.WeekNumber == weekNumber, q => q.OrderBy(a => a.CreateDate)),
            };
            switch (weekNumber)
            {
                case 1:
                    model.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    model.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    model.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    model.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    model.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    model.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            return PartialView(model);
        }
        public PartialViewResult LoadHistoryRevenueOffice(int year, int month, int officeId)
        {
            var model = new LoadHistoryRevenueOfficeViewModel
            {
                Year = year,
                Month = month,
                Office = _unitOfWork.OfficeRepository.GetById(officeId),
                Revenues = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.Year == year && a.Month == month && a.OfficeId == officeId, q => q.OrderBy(a => a.CreateDate)),
            };

            return PartialView(model);
        }

        #endregion

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
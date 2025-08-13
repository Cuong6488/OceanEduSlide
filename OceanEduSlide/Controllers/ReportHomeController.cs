using Fido2NetLib;
using Newtonsoft.Json;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using Org.BouncyCastle.Asn1.X509;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Z.EntityFramework.Plus;
using NLog;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    [ForcePasswordChangeFilter]
    public class ReportHomeController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        private static Logger logger = LogManager.GetCurrentClassLogger();


        public ActionResult Index()
        {
            return View();
        }
        //public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year)
        //{
        //    if (User.TypeUser == null)
        //        return HttpNotFound();
        //    var pageNumber = page ?? 1;
        //    var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year)
        //                && a.ReportCategory.TypeCat == TypeCat.Type1
        //                && a.ReportCategoryId == 35, q => q.OrderBy(a => a.Sort)
        //                );

        //    var offices = _unitOfWork.OfficeRepository.GetQuery(
        //        a => a.Active,
        //        q => q.OrderBy(a => a.ZoneId)
        //    ).ToList();

        //    var officeDataDict = reportDatas
        //        .GroupBy(r => r.OfficeId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.Sum(r =>
        //            {
        //                int val;
        //                var cleanedData = r.Data?.Replace(".", "").Replace(",", "");
        //                return int.TryParse(cleanedData, out val) ? val : 0;
        //            })
        //        );

        //    // Sắp xếp lại danh sách offices trong bộ nhớ
        //    var sortedOffices = offices
        //        .OrderByDescending(o => officeDataDict.ContainsKey(o.Id) ? officeDataDict[o.Id] : 0);
        //    IEnumerable<Office> filteredOffices = sortedOffices;

        //    var model = new ListReportHomeViewModel
        //    {
        //        Month = Month ?? DateTime.Now.Month,
        //        Year = Year ?? DateTime.Now.Year,
        //        //Offices = sortedOffices,
        //        User = User,
        //        ZoneId = ZoneId,
        //        ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
        //        ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year) && a.ReportCategory.TypeCat == TypeCat.Type1, q => q.OrderBy(a => a.Sort))
        //        //OfficeId = OfficeId,
        //    };
        //    if (User.TypeUser == TypeUser.HO)
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
        //    else if (User.TypeUser == TypeUser.CV)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
        //        //model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
        //        filteredOffices = filteredOffices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
        //    }
        //    else
        //    {
        //        model.ZoneId = User.ZoneId;
        //        if (User.TypeUser == TypeUser.ASM)
        //            //model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
        //            filteredOffices = filteredOffices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));


        //        else
        //            //model.Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Id == User.OfficeId);
        //            filteredOffices = filteredOffices.Where(a => a.Id == User.OfficeId);

        //    }

        //    if (model.ZoneId != null)
        //    {
        //        filteredOffices = filteredOffices.Where(a => a.ZoneId == model.ZoneId);

        //        //model.Offices = sortedOffices.ToPagedList(20, pageNumber);
        //        model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
        //    }
        //    //var finalOffices = filteredOffices.OrderByDescending(o => officeDataDict.ContainsKey(o.Id) ? officeDataDict[o.Id] : 0).ToList();

        //    model.Offices = filteredOffices.ToPagedList(pageNumber, 15);
        //    ViewBag.OfficeIds = ",";
        //    foreach (var item in model.ReportDatas)
        //    {
        //        ViewBag.OfficeIds += item.OfficeId + ",";
        //    }
        //    return View(model);
        //}
        public ActionResult ReportKDCN(int? page, int? ZoneId, int? Month, int? Year)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            int currentMonth = Month ?? DateTime.Now.Month;
            int currentYear = Year ?? DateTime.Now.Year;
            int pageNumber = page ?? 1;

            // Truy vấn ReportData: chỉ lấy các field cần thiết để tính toán
            var reportDataRaw = _unitOfWork.ReportDataRepository
                .GetQuery(a =>
                    a.Active &&
                    a.Month == currentMonth &&
                    a.Year == currentYear &&
                    a.ReportCategory.TypeCat == TypeCat.Type1 &&
                    a.ReportCategoryId == 35)
                .Select(a => new { a.OfficeId, a.Data }) // giảm payload rất nhiều
                .AsNoTracking()
                .ToList();

            // Tính tổng theo OfficeId
            var officeDataDict = reportDataRaw
                .GroupBy(r => r.OfficeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r =>
                    {
                        int val;
                        var cleaned = r.Data?.Replace(".", "").Replace(",", "") ?? "0";
                        return int.TryParse(cleaned, out val) ? val : 0;
                    })
                );

            // Truy vấn Office kèm theo điều kiện lọc
            IQueryable<Office> officeQuery = _unitOfWork.OfficeRepository
                .GetQuery(a => a.Active)
                .AsNoTracking(); // nhẹ hơn

            // Lọc theo quyền user
            if (User.TypeUser == TypeUser.CV)
            {
                officeQuery = officeQuery.Where(a => User.ZoneIds.Contains("," + a.Zone.ShortCode + ","));
            }
            else if (User.TypeUser == TypeUser.ASM)
            {
                officeQuery = officeQuery.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
            }
            else if (User.TypeUser != TypeUser.HO)
            {
                officeQuery = officeQuery.Where(a => a.Id == User.OfficeId);
            }

            if (ZoneId.HasValue)
            {
                officeQuery = officeQuery.Where(a => a.ZoneId == ZoneId.Value);
            }

            // Lấy danh sách offices đã lọc và sắp xếp
            var filteredOffices = officeQuery
                .ToList() // chỉ ToList khi đã có filter
                .OrderByDescending(o => officeDataDict.ContainsKey(o.Id) ? officeDataDict[o.Id] : 0)
                .ToPagedList(pageNumber, 15);

            // Lấy danh sách ReportData (full) cho hiển thị
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a =>
                a.Active &&
                a.Month == currentMonth &&
                a.Year == currentYear &&
                a.ReportCategory.TypeCat == TypeCat.Type1);

            if (ZoneId.HasValue)
            {
                reportDatas = reportDatas.Where(a => a.Office.ZoneId == ZoneId.Value);
            }

            var model = new ListReportHomeViewModel
            {
                Month = currentMonth,
                Year = currentYear,
                User = User,
                ZoneId = ZoneId,
                Offices = filteredOffices,
                ReportCategories = _unitOfWork.ReportCategoryRepository
                    .GetQuery(a => a.Active && a.TypeCat == TypeCat.Type1,
                              q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort))
                    .AsNoTracking(),
                ReportDatas = reportDatas.AsNoTracking()
            };

            // Zones theo quyền
            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository
                    .Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
            }
            else
            {
                model.ZoneId = User.ZoneId;
            }

            // Gán danh sách OfficeId cho ViewBag
            ViewBag.OfficeIds = "," + string.Join(",", model.ReportDatas.Select(d => d.OfficeId).Distinct()) + ",";

            return View(model);
        }
        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        {
            if (User.TypeUser == null)
                return HttpNotFound();

            var pageNumber = page ?? 1;
            var selectedMonth = Month ?? DateTime.Now.Month;
            var selectedYear = Year ?? DateTime.Now.Year;

            // Lấy dữ liệu báo cáo theo điều kiện
            var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active
                    && a.Month == selectedMonth
                    && a.Year == selectedYear
                    && a.ReportCategory.TypeCat == TypeCat.Type2
                    && a.ReportCategoryId == 88,
                q => q.OrderBy(a => a.Sort))
                .ToList();

            // Lấy danh sách user có OfficeId và TypeUser
            var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.TypeUser != null && a.OfficeId != null,
                    q => q.OrderBy(a => a.OfficeId))
                .ToList();

            // Tính tổng giá trị reportData cho từng user (đã parse số liệu)
            var userDataDict = reportDatas
                .GroupBy(r => r.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(r =>
                    {
                        int val;
                        var cleanedData = r.Data?.Replace(",", "");
                        return int.TryParse(cleanedData, out val) ? val : 0;
                    })
                );

            // Sắp xếp user theo tổng giá trị data giảm dần rồi theo OfficeId
            var sortedUsers = users
                .OrderByDescending(o => userDataDict.ContainsKey(o.Id) ? userDataDict[o.Id] : 0)
                .ThenBy(a => a.OfficeId);

            IEnumerable<User> filteredUsers = sortedUsers;

            // Tạo model view
            var model = new ListReportNVHomeViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
                ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == selectedMonth && a.Year == selectedYear && a.ReportCategory.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Sort)),
                OfficeId = OfficeId,
            };

            // Phân quyền theo TypeUser để lọc vùng, chi nhánh, nhân viên
            if (User.TypeUser == TypeUser.HO)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            }
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
                filteredUsers = filteredUsers.Where(a => User.ZoneIds.Contains("," + a.Office.Zone?.ShortCode + ","));
            }
            else
            {
                model.ZoneId = User.ZoneId;

                if (User.TypeUser == TypeUser.ASM)
                {
                    model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                    filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
                }
                else
                {
                    model.OfficeId = User.OfficeId;
                    filteredUsers = filteredUsers.Where(a => a.OfficeId == User.OfficeId);
                }
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);
            }

            if (model.OfficeId != null)
            {
                model.ReportDatas = model.ReportDatas.Where(a => a.User?.OfficeId == model.OfficeId);
                filteredUsers = filteredUsers.Where(a => a.OfficeId == model.OfficeId);
                ViewBag.OfficeIds = "," + string.Join(",", model.ReportDatas.Select(d => d.OfficeId)) + ",";
            }

            // Phân trang cho danh sách user
            model.Users = filteredUsers.ToPagedList(pageNumber, 15);

            // Tạo chuỗi MaNhanViens dùng để phân biệt user có dữ liệu báo cáo
            string manhanviens = ",";
            foreach (var item in model.ReportDatas)
            {
                if (!manhanviens.Contains("," + item.User?.MaNhanVien + ","))
                    manhanviens += item.User?.MaNhanVien + ",";
            }

            ViewBag.MaNhanViens = manhanviens;

            return View(model);
        }

        //public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
        //{

        //    if (User.TypeUser == null)
        //        return HttpNotFound();
        //    var pageNumber = page ?? 1;
        //    var reportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year)
        //                && a.ReportCategory.TypeCat == TypeCat.Type2
        //                && a.ReportCategoryId == 88, q => q.OrderBy(a => a.Sort)
        //                ).ToList();
        //    var users = _unitOfWork.UserRepository.GetQuery(
        //       a => a.Active && a.TypeUser != null && a.OfficeId != null,
        //       q => q.OrderBy(a => a.OfficeId)
        //   ).ToList();

        //    var userDataDict = reportDatas
        //        .GroupBy(r => r.UserId)
        //        .ToDictionary(
        //            g => g.Key,
        //            g => g.Sum(r =>
        //            {
        //                int val;
        //                var cleanedData = r.Data?.Replace(".", "");
        //                return int.TryParse(cleanedData, out val) ? val : 0;
        //            })
        //        );
        //    var sortedUsers = users
        //        .OrderByDescending(o => userDataDict.ContainsKey(o.Id) ? userDataDict[o.Id] : 0).ThenBy(a => a.OfficeId);
        //    IEnumerable<User> filteredUsers = sortedUsers;
        //    var model = new ListReportNVHomeViewModel
        //    {
        //        Month = Month ?? DateTime.Now.Month,
        //        Year = Year ?? DateTime.Now.Year,
        //        Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
        //        User = User,
        //        ZoneId = ZoneId,
        //        ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
        //        ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year) && a.ReportCategory.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Sort)),
        //        OfficeId = OfficeId,
        //    };
        //    if (User.TypeUser == TypeUser.HO)
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
        //    else if (User.TypeUser == TypeUser.CV)
        //    {
        //        model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
        //        model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
        //        filteredUsers = filteredUsers.Where(a => User.ZoneIds.Contains("," + a.Office.Zone?.ShortCode + ","));

        //    }
        //    else
        //    {
        //        model.ZoneId = User.ZoneId;
        //        if (User.TypeUser == TypeUser.ASM)
        //        {
        //            model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
        //            filteredUsers = filteredUsers.Where(a => User.Zone.OfficeIds.Contains("," + a.Office.Id.ToString() + ","));
        //        }


        //        else
        //        {
        //            model.OfficeId = User.OfficeId;
        //            filteredUsers = filteredUsers.Where(a => a.OfficeId == User.OfficeId);
        //        }


        //    }

        //    if (model.ZoneId != null)
        //    {
        //        model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
        //        //model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
        //        filteredUsers = filteredUsers.Where(a => a.Office.ZoneId == model.ZoneId);

        //    }
        //    if (model.OfficeId != null)
        //    {
        //        model.ReportDatas = model.ReportDatas.Where(a => a.User?.OfficeId == model.OfficeId);
        //        //model.Users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeId == model.OfficeId && a.TypeUser != null);
        //        filteredUsers = filteredUsers.Where(a => a.OfficeId == model.OfficeId);
        //        // Tạo danh sách OfficeIds cho ViewBag
        //        ViewBag.OfficeIds = "," + string.Join(",", model.ReportDatas.Select(d => d.OfficeId)) + ",";

        //    }
        //    model.Users = filteredUsers.ToPagedList(pageNumber, 15);
        //    string manhanviens = ",";
        //    foreach (var item in model.ReportDatas)
        //    {
        //        if (!(manhanviens + ",").Contains("," + item.User?.MaNhanVien + ","))
        //            manhanviens += item.User?.MaNhanVien + ",";
        //    }
        //    ViewBag.MaNhanViens = manhanviens;
        //    return View(model);
        //}
        public ActionResult ListCall()
        {
            int pageSize = 20;
            int pageIndex = 1; // Ví dụ, bạn muốn lấy trang 1

            var logs = _unitOfWork.CallLogRepository
                .GetQuery()
                .Take(pageSize);

            return View(logs);
        }
        #region CallLogs
        public async Task<ActionResult> Sync()
        {
            await SyncCallLogsAsync();
            return Content("Đã đồng bộ xong các cuộc gọi");
        }

        private async Task SyncCallLogsAsync()
        {
            int daysToCheck = 3;
            DateTime today = DateTime.Today;

            for (int i = 1; i <= daysToCheck; i++)
            {
                DateTime day = today.AddDays(-i);

                bool hasData = _unitOfWork.CallLogRepository
                    .GetQuery(x => DbFunctions.TruncateTime(x.CallDate) == day)
                    .Any();

                if (!hasData)
                {
                    //logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " khong co du lieu");
                    await FetchAndSaveLogsAsync(day);
                }
                else
                {
                    logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " da co du lieu");
                }
            }
        }

        private async Task FetchAndSaveLogsAsync(DateTime day)
        {
            string user = "lvd";
            string pass = "qazplm123`$%^";
            string baseUrl = "https://voip.ocean.edu.vn/api/report.php";

            string tbegin = day.ToString("yyyy/MM/dd");
            string tend = day.AddDays(1).ToString("yyyy/MM/dd");

            string url = $"{baseUrl}?user={user}&pass={Uri.EscapeDataString(pass)}&tbegin={tbegin}&tend={tend}&type=1";
            logger.Info(url);
            using (var http = new HttpClient())
            {
                try
                {
                    var json = await http.GetStringAsync(url);
                    if (string.IsNullOrEmpty(json) || json?.Length < 10)
                    {
                        logger.Info(json);
                    }
                    var allLogs = JsonConvert.DeserializeObject<List<CallLog>>(json);
                    if (allLogs == null || allLogs.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"No logs found for {day:yyyy-MM-dd}");
                        logger.Info("Khong co ban ghi nao ngay " + day.ToString("dd/MM/yyyy"));
                        return;
                    }

                    var logs = allLogs.ToList();
                    var uniqueIds = logs.Select(l => l.UniqueId).Distinct().ToList();

                    // ✅ Chia nhỏ truy vấn để tránh lỗi biểu thức dài
                    var existingUniqueIds = new HashSet<string>();
                    int batchSize = 500;
                    for (int i = 0; i < uniqueIds.Count; i += batchSize)
                    {
                        var batch = uniqueIds.Skip(i).Take(batchSize).ToList();
                        var batchIds = _unitOfWork.CallLogRepository
                            .GetQuery(c => batch.Contains(c.UniqueId))
                            .Select(c => c.UniqueId)
                            .ToList();
                        foreach (var id in batchIds)
                            existingUniqueIds.Add(id);
                    }

                    var newLogs = logs.Where(log => !existingUniqueIds.Contains(log.UniqueId)).ToList();

                    if (!newLogs.Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ No new logs to sync for {day:yyyy-MM-dd}");
                        logger.Info("Khong co ban ghi moi nao ngay " + day.ToString("dd/MM/yyyy"));

                        return;
                    }

                    // ✅ Tải toàn bộ người dùng 1 lần duy nhất
                    var userDict = _unitOfWork.UserRepository.GetQuery(u => !string.IsNullOrEmpty(u.MaNhanVien) && u.Active).ToList()
                                    .GroupBy(u => u.MaNhanVien).Select(g => g.First()).ToDictionary(u => u.MaNhanVien, u => u.Id);

                    // ✅ Lọc các logs không tìm thấy user
                    newLogs = newLogs
                        .Where(log =>
                        {
                            if (userDict.TryGetValue(log.Exten, out var userId))
                            {
                                log.UserId = userId;
                                //log.CallDateString = log.CallDate.ToString("dd/MM/yyyy"); // nếu cần
                                return true;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No user found for Exten {log.Exten}. Skipping log.");
                                return false;
                            }
                        })
                        .ToList();

                    if (newLogs.Any())
                    {
                        _unitOfWork.CallLogRepository.InsertRange(newLogs);
                        _unitOfWork.Save();
                        System.Diagnostics.Debug.WriteLine($"✓ Synced {newLogs.Count} new call logs for {day:yyyy-MM-dd}");
                        logger.Info("Cap nhat du lieu thanh cong ngay " + day.ToString("dd/MM/yyyy"));
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"✓ No matching users for new logs on {day:yyyy-MM-dd}");
                        logger.Info("Khong co user nao trung khop - ngay " + day.ToString("dd/MM/yyyy"));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error syncing {day:yyyy-MM-dd}: {ex.Message}");
                    logger.Error("Loi xay ra: " + ex.Message + day.ToString("dd/MM/yyyy"));
                }
            }
        }
        public ActionResult ReportCall(int? page, int? ZoneId, int? OfficeId, string startDay, string endDay)
        {

            if (User.TypeUser == null)
                return HttpNotFound();
            var pageNumber = page ?? 1;

            if (string.IsNullOrEmpty(startDay))
                startDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            if (string.IsNullOrEmpty(endDay))
                endDay = DateTime.Now.AddDays(-1).ToString("dd/MM/yyyy");
            var model = new ListCallViewModel
            {
                Offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.Sort)),
                User = User,
                ZoneId = ZoneId,
                OfficeId = OfficeId,
                StartDay = startDay,
                EndDay = endDay
            };
            if (User.TypeUser == TypeUser.HO)
                model.Zones = _unitOfWork.ZoneRepository.Get(a => a.Active);
            else if (User.TypeUser == TypeUser.CV)
            {
                model.Zones = _unitOfWork.ZoneRepository.Get(a => User.ZoneIds.Contains("," + a.ShortCode + ",") && a.Active);
                model.Offices = model.Offices.Where(a => User.ZoneIds.Contains("," + a.Zone?.ShortCode + ","));
            }
            else
            {
                model.ZoneId = User.ZoneId;
                if (User.TypeUser == TypeUser.ASM)
                    model.Offices = model.Offices.Where(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                else
                    model.OfficeId = User.OfficeId;
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                //model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
            }
            if (model.OfficeId != null && !string.IsNullOrEmpty(startDay) && !string.IsNullOrEmpty(endDay))
            {
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
                var startDate = StartDate.Date;
                var endDate = EndDate.Date.AddDays(1);
                var callData = _unitOfWork.CallLogRepository.GetQuery(p => p.CallDate >= startDate && p.CallDate < endDate && p.User.OfficeId == model.OfficeId);
                var aggregated = callData.GroupBy(p => p.UserId).Select(g => new
                {
                    UserId = g.Key,
                    Over120s = g.Count(x => x.BillSec > 120),
                    Over90s = g.Count(x => x.BillSec > 90 && x.BillSec <= 120),
                    Over60s = g.Count(x => x.BillSec >= 60 && x.BillSec <= 90),
                    Under60s = g.Count(x => x.BillSec >= 30 && x.BillSec < 60),
                    Under30s = g.Count(x => x.BillSec < 30 && x.Disposition == "ANSWERED"),
                    NoAns = g.Count(x => x.Disposition == "NO ANSWER"),
                    Busy = g.Count(x => x.Disposition == "BUSY"),
                    Failed = g.Count(x => x.Disposition == "FAILED"),
                    TotalOver60s = g.Count(x => x.BillSec >= 60),
                    TotalOver30s = g.Count(x => x.BillSec >= 30),
                    Total = g.Count(),


                }).ToList();
                var users = _unitOfWork.UserRepository.Get(a => a.TypeUser != null && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.ASM && a.OfficeId == model.OfficeId);
                var userItems = users.Select(u =>
                {
                    var match = aggregated.FirstOrDefault(x => x.UserId == u.Id);

                    return new ListCallViewModel.UserItem
                    {
                        User = u,
                        Over120s = match?.Over120s ?? 0,
                        Over90s = match?.Over90s ?? 0,
                        Over60s = match?.Over60s ?? 0,
                        Under60s = match?.Under60s ?? 0,
                        Under30s = match?.Under30s ?? 0,
                        NoAns = match?.NoAns ?? 0,
                        Busy = match?.Busy ?? 0,
                        Failed = match?.Failed ?? 0,
                        Total = match?.Total ?? 0,
                        TotalOver30s = match?.TotalOver30s ?? 0,
                        TotalOver60s = match?.TotalOver60s ?? 0,
                    };
                }).ToList();
                model.TotalOver120s = userItems.Sum(a => a.Over120s);
                model.TotalOver90s = userItems.Sum(a => a.Over90s);
                model.TotalOver60s = userItems.Sum(a => a.Over60s);
                model.TotalUnder60s = userItems.Sum(a => a.Under60s);
                model.TotalUnder30s = userItems.Sum(a => a.Under30s);
                model.UserItems = userItems;
            }
            return View(model);

        }
        public PartialViewResult LoadListCall(int userId, int type, string startDay, string endDay)
        {
            var model = new LoadListCallViewModel
            {
                StartDay = startDay,
                EndDay = endDay,
                User = _unitOfWork.UserRepository.GetById(userId),
            };

            DateTime startDate = new DateTime();
            DateTime endDate = new DateTime();

            if (DateTime.TryParse(startDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                startDate = cd.Date;

            if (DateTime.TryParse(endDay, new CultureInfo("vi-VN"), DateTimeStyles.None, out var crd))
                endDate = crd.Date.AddDays(1);

            switch (type)
            {
                case 120:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 120);
                    ViewBag.Type = "trên 2 phút";
                    break;
                case 90:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec > 90 && p.BillSec <= 120);
                    ViewBag.Type = "trên 1,5 phút";
                    break;
                case 60:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 60 && p.BillSec <= 90);
                    ViewBag.Type = "trên 1 phút";
                    break;
                case 59:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec >= 30 && p.BillSec < 60);
                    ViewBag.Type = "dưới 1 phút";
                    break;
                case 30:
                    model.CallLogs = _unitOfWork.CallLogRepository.GetQuery(p => p.UserId == userId && p.CallDate >= startDate && p.CallDate < endDate && p.BillSec < 30 && p.Disposition == "ANSWERED");
                    ViewBag.Type = "Dưới 30s";
                    break;
                default:
                    ViewBag.Type = "";
                    break;
            }

            return PartialView(model);
        }
        public ActionResult ClearCallLogs()
        {
            var calllogs = _unitOfWork.CallLogRepository.GetQuery();
            calllogs.Delete();
            return RedirectToAction("ReportCall");
        }

        #endregion
    }
}
using Newtonsoft.Json;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
                ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year) && a.ReportCategory.TypeCat == TypeCat.Type1, q => q.OrderBy(a => a.Sort))
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
            ViewBag.OfficeIds = ",";
            foreach (var item in model.ReportDatas)
            {
                ViewBag.OfficeIds += item.OfficeId + ",";
            }
            return View(model);
        }
        public ActionResult ReportKDNV(int? page, int? ZoneId, int? OfficeId, int? Month, int? Year)
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
                ReportCategories = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Group).ThenBy(a => a.Sort)),
                ReportDatas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == (Month ?? DateTime.Now.Month) && a.Year == (Year ?? DateTime.Now.Year) && a.ReportCategory.TypeCat == TypeCat.Type2, q => q.OrderBy(a => a.Sort)),
                OfficeId = OfficeId,
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
                    model.OfficeId = User.OfficeId;
            }

            if (model.ZoneId != null)
            {
                model.Offices = model.Offices.Where(a => a.ZoneId == model.ZoneId);
                //model.ReportDatas = model.ReportDatas.Where(a => a.Office.ZoneId == model.ZoneId);
            }
            if (model.OfficeId != null)
            {
                model.ReportDatas = model.ReportDatas.Where(a => a.User?.OfficeId == model.OfficeId);
                model.Users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeId == model.OfficeId && a.TypeUser != null);
            }
            string manhanviens = ",";
            foreach (var item in model.ReportDatas)
            {
                if (!(manhanviens + ",").Contains("," + item.User?.MaNhanVien + ","))
                    manhanviens += item.User?.MaNhanVien + ",";
            }
            ViewBag.MaNhanViens = manhanviens;
            return View(model);
        }
        public async Task<ActionResult> Sync()
        {
            await SyncCallLogsAsync();
            return Content("Đã đồng bộ xong các cuộc gọi đã trả lời (ANSWERED).");
        }
        public ActionResult ListCall()
        {
            int pageSize = 20;
            int pageIndex = 1; // Ví dụ, bạn muốn lấy trang 1

            var logs = _unitOfWork.CallLogRepository
                .GetQuery(c => c.Disposition == "ANSWERED")
                .Take(pageSize);

            return View(logs);
        }

        private async Task SyncCallLogsAsync()
        {
            int daysToCheck = 7;
            DateTime today = DateTime.Today;

            for (int i = 1; i <= daysToCheck; i++)
            {
                DateTime day = today.AddDays(-i);

                bool hasData = _unitOfWork.CallLogRepository
                    .GetQuery(x => DbFunctions.TruncateTime(x.CallDate) == day)
                    .Any();

                if (!hasData)
                {
                    await FetchAndSaveLogsAsync(day);
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

            using (var http = new HttpClient())
            {
                try
                {
                    var json = await http.GetStringAsync(url);
                    var allLogs = JsonConvert.DeserializeObject<List<CallLog>>(json);

                    var logs = allLogs
                                .Where(l => l.Disposition == "ANSWERED")
                                .ToList();
                    var uniqueIds = logs.Select(l => l.UniqueId).ToList();
                    var existingUniqueIds = _unitOfWork.CallLogRepository
            .GetQuery(c => uniqueIds.Contains(c.UniqueId)) // Truy vấn dựa trên danh sách UniqueId
            .Select(c => c.UniqueId)
            .ToList();
                    var newLogs = logs
            .Where(log => !existingUniqueIds.Contains(log.UniqueId))
            .ToList();
                    //foreach (var log in newLogs)
                    //{
                    //    log.CallDateString = log.CallDate.ToString("dd/MM/yyyy");
                    //    var u = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == log.Exten).FirstOrDefault();
                    //    if (u != null)
                    //    {
                    //        log.UserId = u.Id;
                    //    }
                    //    else
                    //    {
                    //        // Nếu không tìm thấy người dùng, xóa bản ghi khỏi newLogs
                    //        System.Diagnostics.Debug.WriteLine($"No user found for Exten {log.Exten}. Removing log.");
                    //        newLogs.Remove(log);  // Loại bỏ log khỏi newLogs
                    //        continue;  // Bỏ qua bản ghi này và chuyển sang bản ghi tiếp theo
                    //    }
                    //}
                    newLogs.RemoveAll(log =>
                    {
                        var u = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == log.Exten).FirstOrDefault();
                        if (u != null)
                        {
                            log.UserId = u.Id;  // Gán UserId cho log
                            return false;  // Nếu tìm thấy người dùng, không xóa bản ghi này
                        }
                        else
                        {
                            // Nếu không tìm thấy người dùng, xóa log khỏi newLogs
                            System.Diagnostics.Debug.WriteLine($"No user found for Exten {log.Exten}. Removing log.");
                            return true;  // Xóa log này khỏi newLogs
                        }
                    });
                    if (newLogs.Any())
                    {
                        _unitOfWork.CallLogRepository.InsertRange(newLogs);
                        _unitOfWork.Save();
                    }
                    //_unitOfWork.Save();
                    //System.Diagnostics.Debug.WriteLine($"✓ Synced {logs.Count()} calls for {day:yyyy-MM-dd}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"✗ Error syncing {day:yyyy-MM-dd}: {ex.Message}");
                }
            }
        }

    }
}
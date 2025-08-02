using Newtonsoft.Json;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using NLog;
using System.Data.Entity;

namespace OceanEduSlide.DAL
{
    public class CallLogService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        #region CallLogs
        //public async Task<ActionResult> Sync()
        //{
        //    await SyncCallLogsAsync();
        //    return Content("Đã đồng bộ xong các cuộc gọi");
        //}

        //private async Task SyncCallLogsAsync()
        //{
        //    int daysToCheck = 3;
        //    DateTime today = DateTime.Today;

        //    for (int i = 1; i <= daysToCheck; i++)
        //    {
        //        DateTime day = today.AddDays(-i);

        //        bool hasData = _unitOfWork.CallLogRepository
        //            .GetQuery(x => DbFunctions.TruncateTime(x.CallDate) == day)
        //            .Any();

        //        if (!hasData)
        //        {
        //            //logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " khong co du lieu");
        //            await FetchAndSaveLogsAsync(day);
        //        }
        //        else
        //        {
        //            logger.Info("Ngay " + day.ToString("dd/MM/yyyy") + " da co du lieu");
        //        }
        //    }
        //}
        public async Task SyncYesterdayAsync()
        {
            DateTime yesterday = DateTime.Today.AddDays(-1);
            await FetchAndSaveLogsAsync(yesterday);
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

        #endregion
    }
}
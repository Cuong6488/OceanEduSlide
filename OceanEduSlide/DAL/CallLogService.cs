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
using FluentScheduler;
using Newtonsoft.Json.Linq;
using System.IO;
using OceanEduSlide.Migrations;

namespace OceanEduSlide.DAL
{
    public class CallLogService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();

        #region CallLogs
        public async Task SyncCusTom(int month, int day)
        {
            await Sync3DayAsync(month, day);
        }
        public async Task Sync3DayAsync(int month, int day)
        {
            for (int i = 1; i <= 7; i++)
            {
                DateTime daycheck = new DateTime(2025, month, day).AddDays(-i);
                await FetchAndSaveLogsAsync(daycheck);
            }
        }
        public async Task SyncRecentlyAsync()
        {
            for (int i = 1; i <= 7; i++)
            {
                DateTime day = DateTime.Today.AddDays(-i);
                await FetchAndSaveLogsAsync(day);
            }
        }
        public async Task SyncTodayAsync()
        {
            DateTime day = DateTime.Today;
            await FetchAndSaveLogsAsync(day);
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
                                var historyUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.UserId == userId && a.Month == day.Month && a.Year == day.Year && a.DayStart <= day && (a.DayEnd == null
                                || (a.DayEnd != null && a.DayEnd.Value > day)), q => q.OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null)).FirstOrDefault();

                                if (historyUser != null)
                                {

                                    log.UserId = userId;
                                    log.HistoryUserId = historyUser.Id;
                                    //log.CallDateString = log.CallDate.ToString("dd/MM/yyyy"); // nếu cần
                                    return true;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"Không có bản ghi user theo tháng nào của nhân sự {log.Exten}");
                                    return false;
                                }
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
        public async Task SyncDuplicateAsync()
        {
            await Task.Run(() =>
            {
                FetchAndSaveLogsDuplicateAsync();
            });
        }
        public void FetchAndSaveLogsDuplicateAsync()
        {
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            var duplicateUserIds = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == thisMonth && a.Year == thisYear).GroupBy(a => a.UserId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            var listHistoryUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == thisMonth && a.Year == thisYear && duplicateUserIds.Contains(a.UserId))
                .Select(a => new HistoryUserDto
                {
                    Id = a.Id,
                    DayStart = a.DayStart,
                    DayEnd = a.DayEnd,
                    UserId = a.UserId,
                    OfficeId = a.OfficeId,
                    Status = a.Status,
                }).ToList();
            // lấy ngày làm việc thực tế từ vị trí cũ (điều chuyển)
            foreach (var item in listHistoryUser)
            {
                if (item.Status == StatusUser.Active)
                {
                    var oldPosittion = listHistoryUser.Where(a => a.DayEnd != null && a.Status == StatusUser.Transfer && a.UserId == item.UserId).OrderByDescending(a => a.DayEnd).FirstOrDefault();
                    if (oldPosittion != null)
                    {
                        item.DayStart = (DateTime)oldPosittion.DayEnd;
                    }
                }
            }
            // lấy ra những cuộc gọi có calldate lệch với ngày vào làm/ nghỉ việc của nhân viên
            //var listDuplicateCallLog = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId != null && (DbFunctions.TruncateTime(a.HistoryUser.DayStart) > DbFunctions.TruncateTime(a.CallDate)
            //|| (a.HistoryUser.DayEnd != null && (DbFunctions.TruncateTime(a.HistoryUser.DayEnd) <= DbFunctions.TruncateTime(a.CallDate)))) && a.CallDate.Month == thisMonth && a.CallDate.Year == thisYear).ToList();

            var callLogs = _unitOfWork.CallLogRepository
                .GetQuery(a => a.HistoryUserId != null
                    && duplicateUserIds.Contains(a.HistoryUser.UserId)
                    && a.CallDate.Month == thisMonth
                    && a.CallDate.Year == thisYear)
                .ToList();

            // lọc các calllog bị lệch theo dữ liệu đã hiệu chỉnh
            var listDuplicateCallLog = callLogs
                .Where(a =>
                {
                    var history = listHistoryUser.FirstOrDefault(h => h.Id == a.HistoryUserId);
                    if (history == null) return false;

                    return history.DayStart.Date > a.CallDate.Date
                        || (history.DayEnd != null && history.DayEnd.Value.Date <= a.CallDate.Date);
                })
                .ToList();

            foreach (var callLog in listDuplicateCallLog)
            {
                // select historyuser chuẩn cho cuộc gọi
                var historyUser = listHistoryUser.Where(a => a.UserId == callLog.HistoryUser.UserId && a.DayStart.Date <= callLog.CallDate.Date && (a.DayEnd == null
                                || (a.DayEnd != null && a.DayEnd.Value > callLog.CallDate.Date))).OrderBy(a => a.DayEnd == null).ThenBy(a => a.DayEnd).ThenBy(a => a.OfficeId == null).FirstOrDefault();
                if (historyUser != null)
                {
                    callLog.HistoryUserId = historyUser.Id;
                }
            }
            _unitOfWork.Save();
        }
        #endregion
    }
}
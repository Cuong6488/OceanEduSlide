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
using OceanEduSlide.OEDongBo;
using Z.EntityFramework.Plus;
using OceanEduSlide.Migrations;
using Helpers;
using System.Web.UI.WebControls;
using ImageResizer.ExtensionMethods;
using Microsoft.IdentityModel.Tokens;
using System.Web.Services.Description;
using System.Text;
using System.Xml.Linq;
using OceanEduSlide.Utils;
namespace OceanEduSlide.DAL
{
    public class UserService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();
        private readonly UserTypeService _userTypeService = new UserTypeService();

        // Lấy tháng trước - năm của tháng trước
        public static (int lastMonth, int yearLastMonth) CalcLastMonth(int currentMonth, int currentYear)
        {
            if (currentMonth == 1)
            {
                return (12, currentYear - 1);
            }
            else
            {
                return (currentMonth - 1, currentYear);
            }
        }

        // Lấy ngày công tối đa của tháng
        public static int? CalcWorkingDayFull(int currentMonth, int currentYear, WorkingDay workingDay)
        {
            int workingDayFull = 1;

            switch (currentMonth)
            {
                case 1:
                    workingDayFull = workingDay.WorkingDayMonth1;
                    break;
                case 2:
                    workingDayFull = workingDay.WorkingDayMonth2;
                    break;
                case 3:
                    workingDayFull = workingDay.WorkingDayMonth3;
                    break;
                case 4:
                    workingDayFull = workingDay.WorkingDayMonth4;
                    break;
                case 5:
                    workingDayFull = workingDay.WorkingDayMonth5;
                    break;
                case 6:
                    workingDayFull = workingDay.WorkingDayMonth6;
                    break;
                case 7:
                    workingDayFull = workingDay.WorkingDayMonth7;
                    break;
                case 8:
                    workingDayFull = workingDay.WorkingDayMonth8;
                    break;
                case 9:
                    workingDayFull = workingDay.WorkingDayMonth9;
                    break;
                case 10:
                    workingDayFull = workingDay.WorkingDayMonth10;
                    break;
                case 11:
                    workingDayFull = workingDay.WorkingDayMonth11;
                    break;
                case 12:
                    workingDayFull = workingDay.WorkingDayMonth12;
                    break;
                default:
                    break;
            }
            return workingDayFull;
        }

        public static int CalcWorkingDayBase(int currentMonth, int currentYear, HistoryUser historyUser, int workingDayFull)
        {
            HistoryUser oldPosittion = null;

            var startDateReal = historyUser.DayStart;
            // Nếu trạng thái là Đang làm việc
            //if (historyUser.Status == StatusUser.Active)
            //{
            //    //Tìm vị trí cũ
            //    oldPosittion = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == historyUser.UserId && a.Month == monthInt && a.Year == yearInt && a.Status == StatusUser.Transfer, q => q.OrderByDescending(a => a.DayEnd)).FirstOrDefault();
            //    if (oldPosittion != null)
            //    {
            //        if (oldPosittion.DayEnd == null)
            //        {
            //            ModelState.AddModelError("", @"Nhân sự điều chuyển " + oldPosittion.User.MaNhanVien + " không có ngày điều chuyển");
            //            return View();
            //        }
            //        // Gán biến theo ngày điều chuyển để tính ngày bắt đầu làm việc ở vị trí hiện tại
            //        startDateReal = oldPosittion.DayEnd.Value;
            //    }
            //}
            int workingDayBase = 0;
            if ((startDateReal.Year < currentYear || (startDateReal.Year == currentYear && startDateReal.Month < currentMonth)) && (historyUser.DayEnd == null || (historyUser.DayEnd != null && historyUser.DayEnd.Value.Month > currentMonth)))
            {
                workingDayBase = workingDayFull;
            }
            else
            {
                DateTime ngayBatDau = historyUser.DayStart.Year < currentYear || (historyUser.DayStart.Year == currentYear && historyUser.DayStart.Month < currentMonth) ? new DateTime(currentYear, currentMonth, 1) : historyUser.DayStart;
                if (oldPosittion != null)
                {
                    var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                    var oldDayWork = oldDayFull - (oldDayFull / 6);
                    workingDayBase = Math.Max(workingDayFull - oldDayWork, 0);
                }
                else
                {
                    DateTime ngayKetThuc = historyUser.DayEnd != null ? historyUser.DayEnd.Value : new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));
                    int soNgayLamViec = (ngayKetThuc - ngayBatDau).Days + 1;
                    if (soNgayLamViec < 0)
                    {
                        logger.Error("Nhan vien " + historyUser.User?.MaNhanVien + " co ngay vao lam > ngay nghi viec");
                        soNgayLamViec = 0;
                    }
                    int soNgayNghi = soNgayLamViec / 6;
                    workingDayBase = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                }
            }
            if (historyUser.DayReduce > 0)
            {
                workingDayBase -= historyUser.DayReduce ?? 0;
            }
            return workingDayBase;
        }

        // Tính số ngày công thực tế
        public static int CalcWorkingDay(int currentMonth, int currentYear, HistoryUser historyUser, int workingDayFull)
        {
            var workingDayTT = CalcWorkingDayBase(currentMonth, currentYear, historyUser, workingDayFull);
            if (historyUser.DayEnd.HasValue && historyUser.DayStart.Month == historyUser.DayEnd.Value.Month && historyUser.DayStart.Year == historyUser.DayEnd.Value.Year)
            {
                if (workingDayTT < 6)
                {
                    workingDayTT = 0;
                }
            }
            workingDayTT = Math.Max(workingDayTT, 0);
            return workingDayTT;
        }

        // Tính số ngày tính chỉ tiêu cuộc gọi
        public static int CalcWorkingDayCG(int currentMonth, int currentYear, HistoryUser historyUser, int workingDayFull)
        {
            var workingDayCG = CalcWorkingDayBase(currentMonth, currentYear, historyUser, workingDayFull);

            if (historyUser.DayReduceCG > 0)
            {
                return workingDayCG - historyUser.DayReduceCG.Value;
            }
            workingDayCG = Math.Max(workingDayCG, 0);

            return workingDayCG;
        }
        public static StatusUser? GetStatusUser(string status)
        {
            switch (status)
            {
                case "Đang làm việc":
                    return StatusUser.Active;
                case "Nghỉ thai sản":
                    return StatusUser.InActive;
                case "Đã nghỉ":
                    return StatusUser.InActive;
                case "Nghỉ việc":
                    return StatusUser.InActive;
                case "Miễn nhiệm":
                    return StatusUser.InActive;
                case "Điều chuyển":
                    return StatusUser.Transfer;
                case "Bổ nhiệm":
                    return StatusUser.Transfer;
                default:
                    return null;
            }
        }
        public static User UpdateUserInfo(User user, string maNhanSu, string tenNhanSu, string maChucDanh, string password, Office office, Zone zone, TypeUser type, List<User> users, StatusUser status, bool isToday, UnitOfWork unitOfWork, bool isAuto)
        {
            if (user == null)
            {
                var newUser = new User
                {
                    Username = maNhanSu,
                    MaNhanVien = maNhanSu,
                    Password = password,
                    Active = (status == StatusUser.InActive ? false : true),
                    OfficeId = office?.Id,
                    ZoneId = zone?.Id,
                    Fullname = tenNhanSu,
                    SaleKit = true,
                    TypeUser = type,
                    CDCM = maChucDanh,
                };
                unitOfWork.UserRepository.Insert(newUser);
                unitOfWork.Save();
                users.Add(newUser);
                user = newUser;
            }
            else
            {
                if (isToday)
                {
                    user.CDCM = maChucDanh;
                    user.SaleKit = true;
                    user.Active = (status == StatusUser.InActive ? false : true);
                    if (status == StatusUser.Active || status == StatusUser.InActive)
                    {
                        user.OfficeId = office?.Id;
                        user.ZoneId = zone?.Id;
                        user.TypeUser = type;
                    }
                }

            }
            if (isToday && isAuto)
            {
                if (status == StatusUser.Active || status == StatusUser.InActive)
                {
                    if (string.IsNullOrEmpty(user.OfficeIds) || user.OfficeIds.Trim(',').Split(',').Length == 1)
                    {
                        if (office != null)
                        {
                            user.OfficeIds = "," + office.Id + ",";
                            user.OfficeNames = office.ShortCode;
                        }
                        else
                        {
                            user.OfficeIds = null;
                            user.OfficeNames = null;
                        }
                    }
                    if (string.IsNullOrEmpty(user.ZoneIds) || user.ZoneIds.Trim(',').Split(',').Length == 1)
                    {
                        if (zone != null)
                        {
                            user.ZoneIds = "," + zone.ShortCode + ",";
                        }
                        else
                        {
                            user.ZoneIds = null;
                        }
                    }
                }
            }
            return user;
        }

        public static HistoryUser UpdateHistoryUserInfo(User user, string maChucDanh, Office office, Zone zone, TypeUser type, DateTime dayStart, DateTime? dayEnd, int sort, IEnumerable<HistoryUser> listHistoryUser, StatusUser status, int currentMonth, int currentYear, List<HistoryUser> historyUserList, bool isAuto)
        {
            var historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.DayStart.Date == dayStart.Date && a.TypeUser == type && ((office != null && a.OfficeId == office.Id) || (office == null && a.OfficeId == null)));
            if (historyUser != null)
            {
                historyUser.Status = status;
                historyUser.ZoneId = zone?.Id;
                historyUser.CDCM = maChucDanh;
                historyUser.DayEnd = dayEnd;
                historyUser.Sort = sort;
                if (isAuto)
                {
                    if (office != null && (string.IsNullOrEmpty(historyUser.OfficeIds) || historyUser.OfficeIds.Trim(',').Split(',').Length == 1))
                        historyUser.OfficeIds = "," + office.Id + ",";
                    if (zone != null && (string.IsNullOrEmpty(historyUser.ZoneIds) || historyUser.ZoneIds.Trim(',').Split(',').Length == 1))
                        historyUser.ZoneIds = "," + zone.ShortCode + ",";
                }
            }
            else
            {
                var newhistoryUser = new HistoryUser
                {
                    UserId = user.Id,
                    Month = currentMonth,
                    Year = currentYear,
                    TypeUser = type,
                    OfficeId = office?.Id,
                    ZoneId = zone?.Id,
                    Status = status,
                    DayStart = dayStart,
                    CDCM = maChucDanh,
                    DayEnd = dayEnd,
                    Sort = sort,
                    Active = true
                };
                historyUserList.Add(newhistoryUser);
                historyUser = newhistoryUser;
                if (isAuto)
                {
                    if (office != null)
                        newhistoryUser.OfficeIds = "," + office.Id + ",";
                    if (zone != null)
                        newhistoryUser.ZoneIds = "," + zone.ShortCode + ",";
                }

            }
            return historyUser;
        }
        public static void SyncBaoCaoCuocGoi(ConfigSite config, int workingDayFull, IEnumerable<ReportData> reportDatas, List<ReportData> reportDataList, IEnumerable<HistoryUser> listNewHistoryUser, int currentYear, int currentMonth, int lastMonth, DateTime endDayLastMonth, UnitOfWork unitOfWork)
        {
            if (CheckAutoRevenue(config))
            {
                foreach (var item in reportDatas.Where(a => a.ReportCategoryId == 26 || a.ReportCategoryId == 27))
                {
                    item.Data = "0";
                    item.DataReal = 0;
                }
                foreach (var historyUser in listNewHistoryUser)
                {
                    // cuộc gọi thực đạt
                    var countTD = unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.CallDate.Year == currentYear && a.CallDate.Month == currentMonth && a.BillSec >= 60).Count();
                    // Thêm hoặc update thực đạt CG cho NV
                    if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC || countTD > 0)
                    {
                        var reportDataCallTD = reportDatas.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.ReportCategoryId == 100);
                        if (reportDataCallTD == null)
                            reportDataCallTD = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 100);
                        if (reportDataCallTD == null)
                        {
                            reportDataCallTD = new ReportData()
                            {
                                Data = countTD.ToString("N0"),
                                DataReal = countTD,
                                UserId = historyUser.UserId,
                                HistoryUserId = historyUser.Id,
                                Month = currentMonth,
                                Year = currentYear,
                                ReportCategoryId = 100,
                                OfficeId = historyUser.OfficeId,
                                ZoneId = historyUser.ZoneId,
                                Sort = 19,
                            };
                            reportDataList.Add(reportDataCallTD);
                        }
                        else
                        {
                            reportDataCallTD.Data = countTD.ToString("N0");
                            reportDataCallTD.DataReal = countTD;
                        }
                        // Nếu không phải là NVKD: Chỉ tiêu CG trống
                        if ((historyUser.TypeUser != TypeUser.EC && historyUser.TypeUser != TypeUser.ALT && historyUser.TypeUser != TypeUser.AEC) || (historyUser.TypeUser == TypeUser.AEC && historyUser.CDCM == "BDO"))
                        {
                            var targetCallEmpty = reportDatas.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.ReportCategoryId == 99);
                            if (targetCallEmpty == null)
                                targetCallEmpty = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 99);
                            if (targetCallEmpty == null)
                            {
                                targetCallEmpty = new ReportData()
                                {
                                    Data = "",
                                    UserId = historyUser.UserId,
                                    HistoryUserId = historyUser.Id,
                                    Month = currentMonth,
                                    Year = currentYear,
                                    ReportCategoryId = 99,
                                    OfficeId = historyUser.OfficeId,
                                    Sort = 18,
                                };
                                reportDataList.Add(targetCallEmpty);
                            }
                            else
                            {
                                targetCallEmpty.Data = "";
                                targetCallEmpty.Data = null;
                            }
                        }
                    }
                    if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC)
                    {
                        // Tính ngày công làm việc Thực tế
                        var workingDayCG = CalcWorkingDayCG(currentMonth, currentYear, historyUser, workingDayFull);

                        int callTarget = 0;
                        if (historyUser.DayStart.Month == currentMonth || historyUser.DayStart.Month == lastMonth)
                        {
                            //Số ngày làm việc tháng trước
                            var dayFree = (endDayLastMonth - historyUser.DayStart).Days + 1;
                            if (dayFree < 0)
                                dayFree = 0;
                            if (dayFree <= 5)
                            {
                                workingDayCG = Math.Max(0, workingDayCG - (5 - dayFree));
                            }
                        }
                        callTarget = 12 * workingDayCG;
                        // Chỉ tiêu báo cáo cuộc gọi nhân sự
                        if (historyUser.CDCM != "BDO")
                        {
                            var reportDataCall = reportDatas.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.ReportCategoryId == 99);
                            if (reportDataCall == null)
                                reportDataCall = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 99);
                            if (reportDataCall == null)
                            {
                                reportDataCall = new ReportData()
                                {
                                    Data = callTarget.ToString("N0"),
                                    DataReal = callTarget,
                                    UserId = historyUser.UserId,
                                    HistoryUserId = historyUser.Id,
                                    Month = currentMonth,
                                    Year = currentYear,
                                    ReportCategoryId = 99,
                                    OfficeId = historyUser.OfficeId,
                                    ZoneId = historyUser.ZoneId,
                                    Sort = 18,
                                };
                                reportDataList.Add(reportDataCall);

                            }
                            else
                            {
                                reportDataCall.Data = callTarget.ToString("N0");
                                reportDataCall.DataReal = callTarget;
                            }
                            // % Hoàn thành CG Nhân sự
                            if (callTarget > 0)
                            {
                                var ht = (decimal)countTD / callTarget;
                                var reportDataCallHT = reportDatas.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.ReportCategoryId == 101);
                                if (reportDataCallHT == null)
                                    reportDataCallHT = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 101);
                                if (reportDataCallHT == null)
                                {
                                    reportDataCallHT = new ReportData()
                                    {
                                        Data = (ht * 100).ToString("F2") + "%",
                                        DataReal = ht,
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = currentMonth,
                                        Year = currentYear,
                                        ReportCategoryId = 101,
                                        OfficeId = historyUser.OfficeId,
                                        ZoneId = historyUser.ZoneId,
                                        Sort = 20,
                                    };
                                    reportDataList.Add(reportDataCallHT);
                                }
                                else
                                {
                                    reportDataCallHT.Data = (ht * 100).ToString("F2") + "%";
                                    reportDataCallHT.DataReal = ht;
                                }
                            }
                        }


                        //Chỉ tiêu - thực đạt cuộc gọi chi nhánh
                        if (historyUser.OfficeId != null)
                        {
                            //Chỉ tiêu
                            if (historyUser.CDCM != "BDO")
                            {
                                var reportCallOfficeTarget = reportDatas.FirstOrDefault(a => a.OfficeId == historyUser.OfficeId && a.ReportCategoryId == 26);
                                if (reportCallOfficeTarget == null)
                                    reportCallOfficeTarget = reportDataList.FirstOrDefault(a => a.OfficeId == historyUser.OfficeId && a.ReportCategoryId == 26);

                                if (reportCallOfficeTarget == null)
                                {
                                    reportCallOfficeTarget = new ReportData()
                                    {
                                        Data = callTarget.ToString("N0"),
                                        DataReal = callTarget,
                                        Month = currentMonth,
                                        Year = currentYear,
                                        ReportCategoryId = 26,
                                        OfficeId = historyUser.OfficeId,
                                        Sort = 7,
                                    };
                                    reportDataList.Add(reportCallOfficeTarget);

                                }
                                else
                                {
                                    if (reportCallOfficeTarget.DataReal == null)
                                        reportCallOfficeTarget.DataReal = 0;
                                    reportCallOfficeTarget.DataReal += callTarget;
                                    reportCallOfficeTarget.Data = (reportCallOfficeTarget.DataReal ?? 0).ToString("N0");
                                }
                            }


                            // Thực đạt
                            var reportCallOfficeTD = reportDatas.FirstOrDefault(a => a.OfficeId == historyUser.OfficeId && a.ReportCategoryId == 27);
                            if (reportCallOfficeTD == null)
                                reportCallOfficeTD = reportDataList.FirstOrDefault(a => a.OfficeId == historyUser.OfficeId && a.ReportCategoryId == 27);

                            if (reportCallOfficeTD == null)
                            {
                                reportCallOfficeTD = new ReportData()
                                {
                                    Data = countTD.ToString("N0"),
                                    DataReal = countTD,
                                    Month = currentMonth,
                                    Year = currentYear,
                                    ReportCategoryId = 27,
                                    OfficeId = historyUser.OfficeId,
                                    Sort = 8,
                                };
                                reportDataList.Add(reportCallOfficeTD);
                            }
                            else
                            {
                                if (reportCallOfficeTD.DataReal == null)
                                    reportCallOfficeTD.DataReal = 0;
                                reportCallOfficeTD.DataReal += countTD;
                                reportCallOfficeTD.Data = (reportCallOfficeTD.DataReal ?? 0).ToString("N0");
                            }

                        }
                        if (historyUser.TypeUser == TypeUser.AEC)
                        {
                            var zone = unitOfWork.ZoneRepository.GetById(historyUser.ZoneId);
                            if (zone != null)
                            {
                                var office = unitOfWork.OfficeRepository.GetQuery(a => a.Name == zone.Name).FirstOrDefault();
                                if (office == null)
                                {
                                    office = new Office()
                                    {
                                        Name = zone.Name,
                                        ZoneId = zone.Id,
                                        ShortCode = zone.ShortCode,
                                        ShortName = zone.Name
                                    };
                                    unitOfWork.OfficeRepository.Insert(office);
                                    unitOfWork.Save();
                                }
                                else
                                {
                                    office.ZoneId = zone.Id;
                                    office.ShortCode = zone.ShortCode;
                                    office.ShortName = zone.Name;
                                }
                                var historyOffice = unitOfWork.HistoryOfficeRepository.GetQuery(a => a.Active && a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear).FirstOrDefault();
                                if (historyOffice == null)
                                {
                                    historyOffice = new HistoryOffice()
                                    {
                                        OfficeId = office.Id,
                                        ZoneId = zone.Id,
                                        Year = currentYear,
                                        Month = currentMonth,
                                        DBATL = 0,
                                        DBEC = 1,
                                        BaseTarget = 1,
                                        GroupOffice = GroupOffice.E,
                                        QD156 = false,
                                    };
                                    unitOfWork.HistoryOfficeRepository.Insert(historyOffice);
                                    //_unitOfWork.Save();
                                }
                                else
                                {
                                    historyOffice.ZoneId = zone.Id;
                                    historyOffice.DBEC = 1;
                                }
                                historyUser.OfficeId = office.Id;
                                unitOfWork.Save();
                            }
                        }
                    }
                }
            }
        }
        public static void SyncChitieuDS(int currentMonth, int currentYear, int workingDayFull, int lastMonth, int yearLastMonth, DateTime endDayLastMonth, Office office, HistoryOffice historyOffice, WorkingDay workingDay, TargetGroup targetGroup, WorkingDay workingDayLastYear,
            List<RevenueOffice> newRevenueList, List<RevenueUser_Month> newRevenueList2, List<ReportData> reportDataListAdd,
            IEnumerable<HistoryUser> historyUserMonthList, IEnumerable<RevenueOffice> revenueOffices, IEnumerable<RevenueUser_Month> listRevenueUserDataBases, IEnumerable<RevenueUser_Month> revenueUsers, IEnumerable<HistoryUser> NVKDLastMonths,
            IEnumerable<ReportData> reportDatactHVs, IEnumerable<ReportData> reportDatas87, IEnumerable<ReportData> reportTDDSNVs, IEnumerable<ReportData> datahtDSNVs, IEnumerable<ReportData> reportDataCNs, IEnumerable<ReportData> reportTDDSCNs, IEnumerable<ReportData> datahtDSCNs, IEnumerable<ReportData> reportDataHVCNs)
        {
            #region chỉ tiêu doanh số


            var targetBase = historyOffice.BaseTarget;
            var historyUserMonths = historyUserMonthList.Where(a => a.OfficeId == office.Id);

            var revenueOffice = revenueOffices.FirstOrDefault(a => a.OfficeId == office.Id);
            if (revenueOffice == null)
            {
                revenueOffice = newRevenueList.FirstOrDefault(a => a.Month == currentMonth && a.Year == currentYear && a.OfficeId == office.Id);
            }
            // Reset chỉ tiêu CN
            if (revenueOffice == null)
            {
                revenueOffice = new RevenueOffice
                {
                    Target_TS = 0,
                    Target_HV = 0,
                    Target_SAB = 0,
                    Month = currentMonth,
                    Year = currentYear,
                    OfficeId = office.Id,
                };
                newRevenueList.Add(revenueOffice);
            }
            else
            {
                revenueOffice.Target_TS = 0;
                revenueOffice.Target_SAB = 0;
                revenueOffice.Target_HV = 0;
            }
            // Khởi tạo chỉ tiêu HV CN
            decimal chitieuHVCN = 0;
            var DBKD = historyOffice.DBEC + historyOffice.DBATL;
            foreach (var item in historyUserMonths)
            {
                // Khởi tạo chỉ tiêu DS nhân viên
                decimal targetNS = 0;
                if (item.TypeUser == TypeUser.EC || item.TypeUser == TypeUser.ALT)
                {
                    var workingDayTT = CalcWorkingDay(currentMonth, currentYear, item, workingDayFull);
                    bool nsFullTarget = true;

                    decimal targetDBCS = targetBase / DBKD;
                    targetNS = targetDBCS * ((decimal)workingDayTT / workingDayFull);
                    if (item.DayStart.Month == currentMonth && item.DayStart.Year == currentYear)
                    {
                        nsFullTarget = false;
                        targetNS = targetNS / 2;
                    }
                    else if (item.DayStart.Month == lastMonth && item.DayStart.Year == yearLastMonth)
                    {
                        var dayLastMonth = 0;
                        var day50Total = 0;
                        switch (currentMonth)
                        {
                            case 1:
                                day50Total = workingDayLastYear.WorkingDayMonth12;
                                break;
                            case 2:
                                day50Total = workingDay.WorkingDayMonth1;
                                break;
                            case 3:
                                day50Total = workingDay.WorkingDayMonth2;
                                break;
                            case 4:
                                day50Total = workingDay.WorkingDayMonth3;
                                break;
                            case 5:
                                day50Total = workingDay.WorkingDayMonth4;
                                break;
                            case 6:
                                day50Total = workingDay.WorkingDayMonth5;
                                break;
                            case 7:
                                day50Total = workingDay.WorkingDayMonth6;
                                break;
                            case 8:
                                day50Total = workingDay.WorkingDayMonth7;
                                break;
                            case 9:
                                day50Total = workingDay.WorkingDayMonth8;
                                break;
                            case 10:
                                day50Total = workingDay.WorkingDayMonth9;
                                break;
                            case 11:
                                day50Total = workingDay.WorkingDayMonth10;
                                break;
                            case 12:
                                day50Total = workingDay.WorkingDayMonth11;
                                break;
                            default:
                                break;
                        }

                        //Số ngày từ ngày vào làm tới cuối tháng trước
                        var dayTotal = (endDayLastMonth - item.DayStart).Days + 1;
                        //Số ngày nghỉ tháng trước
                        var dayFree = dayTotal / 6;
                        //Số ngày làm việc tháng trước
                        dayLastMonth = Math.Min(dayTotal - dayFree, day50Total);
                        //Số ngày làm việc tính 50% chỉ tiêu tháng này
                        var dayThisMonth50 = Math.Min(day50Total - dayLastMonth, workingDayTT);
                        if (dayThisMonth50 > 0)
                        {
                            nsFullTarget = false;
                        }
                        //Số ngày làm việc tính 100% chỉ tiêu tháng này
                        var dayThisMonthFull = Math.Max(workingDayTT - dayThisMonth50, 0);
                        //if (dayThisMonthFull < workingDayFull)
                        //    nsFullTarget = false;
                        decimal targetNS50 = 0;
                        decimal targetNSFull = 0;
                        targetNS50 = targetDBCS * dayThisMonth50 / workingDayFull / 2;
                        targetNSFull = targetDBCS * dayThisMonthFull / workingDayFull;
                        targetNS = targetNS50 + targetNSFull;
                    }
                    if (historyOffice.QD156 && nsFullTarget)
                    {
                        var countNVKDLastMonth = NVKDLastMonths.Count(a => a.OfficeId == office.Id);
                        decimal hesoEC = 1;
                        decimal hesoATL = 1;
                        int chenhLech = DBKD - countNVKDLastMonth;
                        if (chenhLech == 1)
                        {
                            hesoEC = (decimal)1.1;
                            hesoATL = (decimal)1.1;
                        }
                        else if (chenhLech == 2)
                        {
                            hesoEC = (decimal)1.15;
                            hesoATL = (decimal)1.2;
                        }
                        else if (chenhLech == 3)
                        {
                            hesoEC = (decimal)1.2;
                            hesoATL = (decimal)1.3;
                        }
                        else if (chenhLech == 4)
                        {
                            hesoEC = (decimal)1.25;
                            hesoATL = (decimal)1.4;
                        }
                        else if (chenhLech >= 5)
                        {
                            hesoEC = (decimal)1.3;
                            hesoATL = (decimal)1.5;
                        }
                        if (hesoEC > 1)
                        {
                            if (item.TypeUser == TypeUser.EC)
                                targetNS = targetNS * hesoEC;
                            else
                                targetNS = targetNS * hesoATL;
                        }
                    }
                    revenueOffice.Target_TS += targetNS;


                    // Báo cáo chỉ tiêu học viên NV

                    var reportDatactHV = reportDatactHVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                    if (reportDatactHV == null)
                        reportDatactHV = reportDataListAdd.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 95);

                    // Tính số ngày từ khi khai trương
                    if (!office.OpenDate.HasValue)
                    {
                        continue;
                    }
                    int totalMonths = (currentYear - office.OpenDate.Value.Year) * 12 + (currentMonth - office.OpenDate.Value.Month);
                    var STHBQ = totalMonths > 6 ? 20 : 12;
                    decimal ctHV = 0;
                    ctHV = targetNS / (2989000 * ((decimal)50 / 100) * STHBQ);
                    // Cộng dồn chỉ tiêu HV CN
                    chitieuHVCN += ctHV;
                    if (reportDatactHV == null)
                    {
                        reportDatactHV = new ReportData()
                        {
                            // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                            //Data = ctHV.ToString("N2"),
                            Data = "",
                            UserId = item.UserId,
                            HistoryUserId = item.Id,
                            Month = currentMonth,
                            Year = currentYear,
                            ReportCategoryId = 95,
                            OfficeId = office.Id,
                            Sort = 15,
                            // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                            //DataReal = ctHV
                            DataReal = null
                        };
                        reportDataListAdd.Add(reportDatactHV);
                    }
                    else
                    {
                        // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                        //reportDatactHV.Data = ctHV.ToString("N2");
                        //reportDatactHV.DataReal = ctHV;
                        reportDatactHV.Data = "";
                        reportDatactHV.DataReal = null;
                    }
                    // %ht báo cáo  HV NV

                    // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                    //if (reportDatactHV.DataReal > 0)
                    //{
                    //    // thực đạt HV NV
                    //    var reportTDHVNV = reportTDHVNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                    //    if (reportTDHVNV != null)
                    //    {
                    //        var TDHVNV = reportTDHVNV.DataReal ?? 0;
                    //        var htHVNV = TDHVNV / reportDatactHV.DataReal * 100;
                    //        var datahtHVNV = datahtHVNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                    //        if (datahtHVNV == null)
                    //            datahtHVNV = reportDataListAdd.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 97);
                    //        if (datahtHVNV == null)
                    //        {
                    //            datahtHVNV = new ReportData()
                    //            {
                    //                Data = (htHVNV ?? 0).ToString("F2") + "%",
                    //                DataReal = htHVNV,
                    //                Month = currentMonth,
                    //                Year = currentYear,
                    //                ReportCategoryId = 97,
                    //                OfficeId = office.Id,
                    //                Sort = 17,
                    //            };
                    //            reportDataListAdd.Add(datahtHVNV);
                    //        }
                    //        else
                    //        {
                    //            datahtHVNV.Data = (htHVNV ?? 0).ToString("F2") + "%";
                    //            datahtHVNV.DataReal = htHVNV;
                    //        }
                    //    }
                    //}

                }
                else if (item.TypeUser == TypeUser.CM || item.TypeUser == TypeUser.TTL)
                {
                    switch (historyOffice.GroupOffice)
                    {
                        case GroupOffice.A:
                            targetNS = targetGroup.Target_A;
                            break;
                        case GroupOffice.B:
                            targetNS = targetGroup.Target_B;
                            break;
                        case GroupOffice.C:
                            targetNS = targetGroup.Target_C;
                            break;
                        case GroupOffice.D:
                            targetNS = targetGroup.Target_D;
                            break;
                        case GroupOffice.E:
                            targetNS = targetGroup.Target_E;
                            break;
                        default:
                            break;
                    }
                    revenueOffice.Target_HV += targetNS;
                }
                else if (item.TypeUser == TypeUser.SAB)
                {
                    targetNS = targetBase / (historyOffice.DBEC + historyOffice.DBATL) * 65 / 100;
                    revenueOffice.Target_SAB += targetNS;
                }
                // Báo cáo chỉ tiêu doanh số nhân sự
                var reportData = reportDatas87.FirstOrDefault(a => a.HistoryUserId == item.Id);
                if (reportData == null)
                    reportData = reportDataListAdd.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 87);
                if (reportData == null)
                {
                    reportData = new ReportData()
                    {
                        Data = targetNS.ToString("N0"),
                        DataReal = targetNS,
                        UserId = item.UserId,
                        HistoryUserId = item.Id,
                        Month = currentMonth,
                        Year = currentYear,
                        ReportCategoryId = 87,
                        OfficeId = office.Id,
                        Sort = 10,
                    };
                    reportDataListAdd.Add(reportData);
                }
                else
                {
                    reportData.Data = targetNS.ToString("N0");
                    reportData.DataReal = targetNS;
                }
                // Báo cáo %ht DS nhân sự
                if (targetNS > 0)
                {
                    // thực đạt doanh số NV
                    var reportTDDSNV = reportTDDSNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                    if (reportTDDSNV != null)
                    {
                        var TDDSNV = reportTDDSNV.DataReal ?? 0;
                        var htDSNV = TDDSNV / targetNS * 100;
                        var datahtDSNV = datahtDSNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                        if (datahtDSNV == null)
                            datahtDSNV = reportDataListAdd.FirstOrDefault(a => a.ReportCategoryId == 89 && a.Month == currentMonth && a.Year == currentYear && a.HistoryUserId == item.Id);
                        if (datahtDSNV == null)
                        {
                            datahtDSNV = new ReportData()
                            {
                                Data = htDSNV.ToString("F2") + "%",
                                DataReal = htDSNV / 100,
                                UserId = item.UserId,
                                HistoryUserId = item.Id,
                                Month = currentMonth,
                                Year = currentYear,
                                ReportCategoryId = 89,
                                OfficeId = office.Id,
                                Sort = 12,
                            };
                            reportDataListAdd.Add(datahtDSNV);
                        }
                        else
                        {
                            datahtDSNV.Data = htDSNV.ToString("F2") + "%";
                            datahtDSNV.DataReal = htDSNV / 100;
                        }
                    }
                }

                // Chỉ tiêu DS nhân sự - phân bổ ds
                var r = revenueUsers.FirstOrDefault(a => a.HistoryUserId == item.Id);
                if (r == null)
                    r = newRevenueList2.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == currentMonth && a.Year == currentYear);
                if (r != null)
                {
                    r.Target = targetNS;
                }
                else
                {
                    var rnew = new RevenueUser_Month
                    {
                        Target = targetNS,
                        Month = currentMonth,
                        Year = currentYear,
                        UserId = item.UserId,
                        HistoryUserId = item.Id,

                    };
                    newRevenueList2.Add(rnew);
                }
            }
            if (historyOffice.TargetReduce > 0)
            {
                revenueOffice.Target_TS -= (historyOffice.TargetReduce ?? 0);
            }
            if (historyOffice.NVKDOver > 0)
            {
                var nvkdOver = historyOffice.NVKDOver ?? 0;

                var listRevenueUserDataBase = listRevenueUserDataBases.Where(a => a.HistoryUser.OfficeId == historyOffice.OfficeId);

                var listRevenueUserNew = newRevenueList2.Where(a => a.HistoryUser != null && a.HistoryUser.OfficeId == historyOffice.OfficeId
                && (a.HistoryUser.TypeUser == TypeUser.EC || a.HistoryUser.TypeUser == TypeUser.ALT) && a.Month == currentMonth && a.Year == currentYear);

                var mergedList = listRevenueUserDataBase.Concat(listRevenueUserNew).OrderByDescending(a => a.Target).ToList();

                var countNVKDCN = historyUserMonths.Count(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT);

                var countRevenueUser = mergedList.Count();
                var skipNVKD = countNVKDCN - nvkdOver;
                if (skipNVKD < DBKD)
                {
                    logger.Error("Chi nhánh " + office.ShortName + " có định biên NVKD là " + DBKD + ", mà hiện tại đang có " + countNVKDCN + " NVKD, không thể giảm trừ chỉ tiêu " + historyOffice.NVKDOver + " NVKD");
                    return;
                }
                var sumTargetDown = mergedList.Skip(skipNVKD).Take(nvkdOver).Sum(a => a.Target);
                revenueOffice.Target_TS -= sumTargetDown;
            }
            revenueOffice.Target_TS = Math.Max(targetBase, revenueOffice.Target_TS);
            //Chỉ tiêu báo cáo DS chi nhánh
            var reportDataCN = reportDataCNs.FirstOrDefault(a => a.OfficeId == office.Id);
            if (reportDataCN == null)
                reportDataCN = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 34);
            if (reportDataCN == null)
            {
                reportDataCN = new ReportData()
                {
                    Data = revenueOffice.Target_TS.ToString("N0"),
                    DataReal = revenueOffice.Target_TS,
                    Month = currentMonth,
                    Year = currentYear,
                    ReportCategoryId = 34,
                    OfficeId = office.Id,
                    Sort = 13,
                };
                reportDataListAdd.Add(reportDataCN);
            }
            else
            {
                reportDataCN.Data = revenueOffice.Target_TS.ToString("N0");
                reportDataCN.DataReal = revenueOffice.Target_TS;
            }

            // % HT báo cáo doanh số chi nhánh
            if (reportDataCN.DataReal > 0)
            {
                // thực đạt doanh số CN
                var reportTDDSCN = reportTDDSCNs.FirstOrDefault(a => a.OfficeId == office.Id);
                if (reportTDDSCN != null)
                {
                    var TDDSCN = reportTDDSCN.DataReal ?? 0;
                    var htDSCN = TDDSCN / reportDataCN.DataReal * 100;
                    var datahtDSCN = datahtDSCNs.FirstOrDefault(a => a.OfficeId == office.Id);
                    if (datahtDSCN == null)
                        datahtDSCN = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 36);
                    if (datahtDSCN == null)
                    {
                        datahtDSCN = new ReportData()
                        {
                            Data = (htDSCN ?? 0).ToString("F2") + "%",
                            DataReal = htDSCN,
                            Month = currentMonth,
                            Year = currentYear,
                            ReportCategoryId = 36,
                            OfficeId = office.Id,
                            Sort = 15,
                        };
                        reportDataListAdd.Add(datahtDSCN);
                    }
                    else
                    {
                        datahtDSCN.Data = (htDSCN ?? 0).ToString("F2") + "%";
                        datahtDSCN.DataReal = htDSCN;
                    }
                }
            }

            // Chỉ tiêu HV CN
            var reportDataHVCN = reportDataHVCNs.FirstOrDefault(a => a.OfficeId == office.Id);
            if (reportDataHVCN == null)
                reportDataHVCN = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 30);
            if (reportDataHVCN == null)
            {
                reportDataHVCN = new ReportData()
                {
                    // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                    //Data = chitieuHVCN.ToString("N2"),
                    Data = "",
                    Month = currentMonth,
                    Year = currentYear,
                    ReportCategoryId = 30,
                    OfficeId = office.Id,
                    Sort = 10,
                    // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                    //DataReal = chitieuHVCN,
                    DataReal = null
                };
                reportDataListAdd.Add(reportDataHVCN);
            }
            else
            {
                // OE báo bỏ chỉ tiêu HV, tạm thời để trống
                //reportDataHVCN.Data = chitieuHVCN.ToString("N2");
                //reportDataHVCN.DataReal = chitieuHVCN;
                reportDataHVCN.Data = "";
                reportDataHVCN.DataReal = null;
            }
            // % ht báo cáo HV CN

            // OE báo bỏ chỉ tiêu HV, tạm thời để trống
            //if (reportDataHVCN.DataReal > 0)
            //{
            //    // thực đạt HV CN
            //    var reportTDHVCN = reportTDHVCNs.FirstOrDefault(a => a.OfficeId == office.Id);
            //    if (reportTDHVCN != null)
            //    {
            //        var TDHVCN = reportTDHVCN.DataReal ?? 0;
            //        var htHVCN = TDHVCN / reportDataHVCN.DataReal * 100;
            //        var datahtHVCN = datahtHVCNs.FirstOrDefault(a => a.OfficeId == office.Id);
            //        if (datahtHVCN == null)
            //            datahtHVCN = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 32);
            //        if (datahtHVCN == null)
            //        {
            //            datahtHVCN = new ReportData()
            //            {
            //                Data = (htHVCN ?? 0).ToString("F2") + "%",
            //                DataReal = htHVCN,
            //                Month = currentMonth,
            //                Year = currentYear,
            //                ReportCategoryId = 32,
            //                OfficeId = office.Id,
            //                Sort = 12,
            //            };
            //            reportDataListAdd.Add(datahtHVCN);
            //        }
            //        else
            //        {
            //            datahtHVCN.Data = (htHVCN ?? 0).ToString("F2") + "%";
            //            datahtHVCN.DataReal = htHVCN;
            //        }
            //    }
            //}


            #endregion
        }

        public bool CheckAutoUser(ConfigSite config)
        {
            if (config == null || !config.AutoUser)
            {
                return false;
            }
            return true;
        }
        public static bool CheckAutoRevenue(ConfigSite config)
        {
            if (config == null || !config.AutoRevenue)
            {
                return false;
            }
            return true;
        }
        public void SyncUser(DateTime date)
        {
            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (!CheckAutoUser(config))
            {
                return;
            }
            var password = HtmlHelpers.ComputeHash(config.Password ?? "AUG2025@#", "SHA256", null);
            date = date.Date;
            var today = DateTime.Now.Date;
            bool isToday = true;
            //Nếu đang sync tháng trước
            if (today != date)
                isToday = false;

            var currentMonth = date.Month;
            var currentYear = date.Year;

            // Lấy tháng trước - năm của tháng trước
            var (lastMonth, yearLastMonth) = CalcLastMonth(currentMonth, currentYear);

            // Lấy ngày cuối của tháng trước
            DateTime endDayLastMonth = new DateTime(yearLastMonth, lastMonth, DateTime.DaysInMonth(yearLastMonth, lastMonth));

            var workingDay = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == currentYear).FirstOrDefault();
            if (workingDay == null)
            {
                logger.Error("Chưa có dữ liệu bảng số ngày công năm " + currentYear);
                return;
            }

            var workingDayLastYear = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == currentYear - 1).FirstOrDefault();
            if (workingDayLastYear == null)
            {
                logger.Error("Chưa có dữ liệu bảng số ngày công năm " + (currentYear - 1));
                return;
            }
            // Lấy ngày công tối đa của tháng này
            var workingDayFull = CalcWorkingDayFull(currentMonth, currentYear, workingDay);

            var ThaiSans = _dongBoTuyenSinh.ThaiSans.Where(t => date >= t.NgayBatDauNghiThaiSan && date <= t.NgayKetthucNghiThaiSan).ToList();
            var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.Where(a => DbFunctions.TruncateTime(a.NgayApDung) <= date || a.Loai == "VaoLamlai").OrderByDescending(a => a.NgayApDung).ToList();

            var DSNhanSuNguons = new List<DSNhanSuNguon>();
            var listNSStop_danghi = new List<DSNhanSuNguon>();
            var listNSStop_danglamviec = new List<DSNhanSuNguon>();
            var DSNhanSuDelete = new List<DSNhanSuNguon>();
            if (isToday)
            {
                DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE" || (a.NgayNghiViec.HasValue &&
                          DbFunctions.TruncateTime(a.NgayNghiViec.Value) > DbFunctions.TruncateTime(endDayLastMonth))).ToList();

                // Trạng thái Stop - đã nghỉ
                listNSStop_danghi = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == currentMonth && a.NgayNghiViec.Value.Year == currentYear && a.TrangThai == "E_STOP" && date > a.NgayNghiViec).ToList();

                // Trạng thái Stop - vẫn đang làm việc
                listNSStop_danglamviec = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.TrangThai == "E_STOP" && date <= a.NgayNghiViec && !ThaiSans.Any(t => a.IDNhanSuHRM == t.IDNhanSuHRM)).ToList();

                //Danh sách nhân sự đã nghỉ việc từ tháng trước 
                DSNhanSuDelete = _dongBoTuyenSinh.DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == lastMonth && a.NgayNghiViec.Value.Year == yearLastMonth).ToList();
            }
            else
            {
                DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.Where(a => a.NgayVaoLam.HasValue && DbFunctions.TruncateTime(a.NgayVaoLam.Value) <= DbFunctions.TruncateTime(date)
                        && (a.TrangThai == "E_HIRE" || (a.NgayNghiViec.HasValue && DbFunctions.TruncateTime(a.NgayNghiViec.Value) > DbFunctions.TruncateTime(endDayLastMonth)))).ToList();

                listNSStop_danghi = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == currentMonth && a.NgayNghiViec.Value.Year == currentYear && a.TrangThai == "E_STOP" && today >= a.NgayNghiViec).ToList();

                listNSStop_danglamviec = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.TrangThai == "E_STOP" && today < a.NgayNghiViec && !ThaiSans.Any(t => a.IDNhanSuHRM == t.IDNhanSuHRM)).ToList();
            }

            // Trạng thái E_Hire - đang làm việc
            var listNSDanglamviec = DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE" && !ThaiSans.Any(t => a.IDNhanSuHRM == t.IDNhanSuHRM)).ToList();

            //Nghỉ thai sản
            var listNSTS = DSNhanSuNguons.Where(a =>/* a.TrangThai == "E_HIRE" &&*/ ThaiSans.Any(t => t.IDNhanSuHRM == a.IDNhanSuHRM && t.NgayBatDauNghiThaiSan.Month == currentMonth && t.NgayBatDauNghiThaiSan.Year == currentYear
            && (a.NgayNghiViec == null || a.NgayNghiViec > t.NgayKetthucNghiThaiSan))).ToList();

            // List User tháng
            var listHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);

            // Tổng hợp danh sách NS đang làm việc
            var listAllDanglamviec = listNSDanglamviec.Concat(listNSStop_danglamviec);

            // Tổng hợp danh sách NS đã nghỉ
            var listAllNghiviec = listNSStop_danghi.Concat(listNSTS);

            var allOffice = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var allZone = _unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var users = _unitOfWork.UserRepository.GetQuery().ToList();
            var historyUserList = new List<HistoryUser>();

            // Danh sách Ns sau khi được Điều chuyển
            var listNSSauDieuchuyen = QuaTrinhCongTacs.Where(q => currentMonth == q.NgayApDung.Month && q.NgayApDung.Year == currentYear && (q.Loai == "DieuChuyen" || q.Loai == "BoNhiem" || q.Loai == "MienNhiem"));

            //Danh sách NS Điều chuyển
            foreach (var item /*(banghiA)*/ in listNSSauDieuchuyen)
            {
                var NsDieuchuyen = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM && a != item); /*(bản ghi B)*/
                if (NsDieuchuyen != null)
                {
                    var nhanSuNguon = DSNhanSuNguons.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM);
                    if (nhanSuNguon == null)
                    {
                        logger.Error("Khong nhan su nguon, IDNhanSuHRM: " + item.IDNhanSuHRM);
                        continue;
                    }
                    // Ngày đc là ngày banghiA - QTCT, chức danh và CN - Vùng là banghiB - QTCT

                    if (string.IsNullOrEmpty(nhanSuNguon.MaNhanSu))
                    {
                        logger.Error("Khong co ma nhan su, IDNhanSuHRM: " + item.IDNhanSuHRM);
                        continue;
                    }
                    var type = _userTypeService.GetTypeUser(NsDieuchuyen.MaChucDanh);
                    if (type == null)
                    {
                        logger.Error("Nhan su " + nhanSuNguon.MaNhanSu + ": Khong ton tai CDCM: " + NsDieuchuyen.MaChucDanh);
                        var historyUserWrongTypeUser = listHistoryUser.FirstOrDefault(a => a.Status == StatusUser.Transfer && a.User.MaNhanVien == nhanSuNguon.MaNhanSu);
                        if (historyUserWrongTypeUser != null)
                            historyUserWrongTypeUser.Active = false;

                        continue;
                    }
                    if (nhanSuNguon.NgayVaoLam == null)
                    {
                        logger.Error("Nhan su " + nhanSuNguon.MaNhanSu + ": Ngay vao lam null");
                        continue;
                    }
                    var ngayVaoLam = nhanSuNguon.NgayVaoLam;
                    var logVaoLamLai = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM && (a.Loai == "VaoLamlai" || a.PositionOld == "Nhân viên Học việc"));
                    if (logVaoLamLai != null)
                        ngayVaoLam = logVaoLamLai.NgayApDung;
                    if (ngayVaoLam == null || ngayVaoLam.Value.Date > date)
                    {
                        logger.Error("Nhan su " + nhanSuNguon.MaNhanSu + ": Ngay vao lam lon hon ngay hien tai");
                        continue;
                    }
                    Office office = null;
                    Zone zone = null;
                    if (!string.IsNullOrEmpty(NsDieuchuyen.WorkPlaceName))
                    {
                        NsDieuchuyen.WorkPlaceName = VietnameseCodeHelper.NormalizeVietnameseCode(NsDieuchuyen.WorkPlaceName);

                        if (NsDieuchuyen.WorkPlaceName == "OE Buôn Ma Thuột")
                        {
                            NsDieuchuyen.WorkPlaceName = "OE BMT";
                        }
                        office = allOffice.FirstOrDefault(a => a.ShortName == NsDieuchuyen.WorkPlaceName);
                        zone = allZone.FirstOrDefault(a => a.Name == NsDieuchuyen.WorkPlaceName);
                        if (zone == null && office == null)
                        {
                            logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + NsDieuchuyen.WorkPlaceName);
                        }
                        if (office != null && zone != null)
                            office = null;
                    }

                    var sort = _userTypeService.GetSort((TypeUser)type);

                    var user = users.FirstOrDefault(a => a.MaNhanVien == nhanSuNguon.MaNhanSu);
                    user = UpdateUserInfo(user, nhanSuNguon.MaNhanSu, nhanSuNguon.TenNhanSu, item.MaChucDanh, password, office, zone, type.Value, users, StatusUser.Transfer, isToday, _unitOfWork, true);

                    if (office == null && zone == null)
                    {
                        logger.Error("Nhan su co noi lam viec null: " + nhanSuNguon.MaNhanSu);
                        continue;
                    }

                    UpdateHistoryUserInfo(user, NsDieuchuyen.MaChucDanh, office, zone, type.Value, ngayVaoLam.Value, item.NgayApDung, sort, listHistoryUser, StatusUser.Transfer, currentMonth, currentYear, historyUserList, true);
                }
            }
            // Xử lý ns đang làm việc
            foreach (var item in listAllDanglamviec)
            {
                if (string.IsNullOrEmpty(item.MaNhanSu))
                {
                    logger.Error("Khong co ma nhan su, IDNhanSuHRM: " + item.IDNhanSuHRM);
                    continue;
                }
                var QTCT = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM);
                if (QTCT == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai QTCT");
                    continue;
                }
                var type = _userTypeService.GetTypeUser(QTCT.MaChucDanh);
                if (type == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai CDCM: " + QTCT.MaChucDanh);
                    // xóa các ns đang làm việc do sai phân quyền
                    var historyUserWrongTypeUser = listHistoryUser.FirstOrDefault(a => a.Status == StatusUser.Active && a.User.MaNhanVien == item.MaNhanSu);
                    if (historyUserWrongTypeUser != null)
                        historyUserWrongTypeUser.Active = false;

                    continue;
                }
                if (item.NgayVaoLam == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Ngay vao lam null");
                    continue;
                }
                var ngayVaoLam = item.NgayVaoLam;
                var logVaoLamLai = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM && (a.Loai == "VaoLamlai" || a.PositionOld == "Nhân viên Học việc"));
                if (logVaoLamLai != null)
                    ngayVaoLam = logVaoLamLai.NgayApDung;
                if (ngayVaoLam == null || ngayVaoLam.Value.Date > today)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Ngay vao lam lon hon ngay hien tai");
                    continue;
                }
                Office office = null;
                Zone zone = null;
                if (!string.IsNullOrEmpty(QTCT.WorkPlaceName))
                {
                    QTCT.WorkPlaceName = VietnameseCodeHelper.NormalizeVietnameseCode(QTCT.WorkPlaceName);
                    if (QTCT.WorkPlaceName == "OE Buôn Ma Thuột")
                    {
                        QTCT.WorkPlaceName = "OE BMT";
                    }
                    office = allOffice.FirstOrDefault(a => a.ShortName == QTCT.WorkPlaceName);
                    //if (office == null)
                    //{
                    zone = allZone.FirstOrDefault(a => a.Name == QTCT.WorkPlaceName);
                    if (zone == null && office == null)
                    {
                        logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + QTCT.WorkPlaceName);
                        //continue;
                    }
                    //}
                }

                var sort = _userTypeService.GetSort((TypeUser)type);

                var user = users.FirstOrDefault(a => a.MaNhanVien == item.MaNhanSu);

                user = UpdateUserInfo(user, item.MaNhanSu, item.TenNhanSu, QTCT.MaChucDanh, password, office, zone, type.Value, users, StatusUser.Active, isToday, _unitOfWork, true);

                if (office == null && zone == null)
                {
                    logger.Error("Nhan su co noi lam viec null: " + item.MaNhanSu);
                    continue;
                }

                var historyUserOld = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.Status == StatusUser.Active);
                if (historyUserOld != null)
                {
                    historyUserOld.OfficeId = office?.Id;
                    historyUserOld.ZoneId = zone?.Id;
                    historyUserOld.CDCM = QTCT.MaChucDanh;
                    historyUserOld.TypeUser = (TypeUser)type;
                    historyUserOld.DayStart = (DateTime)ngayVaoLam;
                    historyUserOld.DayEnd = item.NgayNghiViec;
                    historyUserOld.Sort = sort;

                    if (office != null && (string.IsNullOrEmpty(historyUserOld.OfficeIds) || historyUserOld.OfficeIds.Trim(',').Split(',').Length == 1))
                        historyUserOld.OfficeIds = "," + office.Id + ",";
                    if (zone != null && (string.IsNullOrEmpty(historyUserOld.ZoneIds) || historyUserOld.ZoneIds.Trim(',').Split(',').Length == 1))
                        historyUserOld.ZoneIds = "," + zone.ShortCode + ",";
                }
                else
                {
                    UpdateHistoryUserInfo(user, QTCT.MaChucDanh, office, zone, type.Value, ngayVaoLam.Value, item.NgayNghiViec, sort, listHistoryUser, StatusUser.Active, currentMonth, currentYear, historyUserList, true);
                }

            }
            foreach (var item in listAllNghiviec)
            {
                // Xử lý ns nghỉ việc / TS
                if (string.IsNullOrEmpty(item.MaNhanSu))
                {
                    logger.Error("Khong co ma nhan su, IDNhanSuHRM: " + item.IDNhanSuHRM);
                    continue;
                }

                var QTCT = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM);
                if (QTCT == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai QTCT");
                    continue;
                }
                if (string.IsNullOrEmpty(QTCT.MaChucDanh))
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": MaChucDanh QTCT null");
                    continue;
                }
                var type = _userTypeService.GetTypeUser(QTCT.MaChucDanh);
                if (type == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai CDCM QTCT: " + QTCT.MaChucDanh);
                    var historyUserWrongTypeUser = listHistoryUser.FirstOrDefault(a => a.Status == StatusUser.InActive && a.User.MaNhanVien == item.MaNhanSu);
                    if (historyUserWrongTypeUser != null)
                        historyUserWrongTypeUser.Active = false;
                    continue;
                }
                if (item.NgayVaoLam == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Ngay vao lam null");
                    continue;
                }
                var ngayVaoLam = item.NgayVaoLam;
                var logVaoLamLai = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM && (a.Loai == "VaoLamlai" || a.PositionOld == "Nhân viên Học việc"));
                if (logVaoLamLai != null)
                    ngayVaoLam = logVaoLamLai.NgayApDung;
                if (ngayVaoLam == null || ngayVaoLam.Value.Date > today)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Ngay vao lam lon hon ngay hien tai");
                    continue;
                }
                DateTime? ngayNghiViec = null;
                if (item.NgayNghiViec != null)
                    ngayNghiViec = item.NgayNghiViec;
                else
                {
                    var nsTS = ThaiSans.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM);
                    if (nsTS != null)
                    {
                        ngayNghiViec = nsTS.NgayBatDauNghiThaiSan;
                    }
                }
                if (ngayNghiViec == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong co ngay nghi viec");
                    continue;
                }
                if (ngayNghiViec.Value.Month != currentMonth)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Co thang nghi viec khong phai thang hien tai");
                    continue;
                }
                Office office = null;
                Zone zone = null;

                if (!string.IsNullOrEmpty(QTCT.WorkPlaceName))
                {
                    QTCT.WorkPlaceName = VietnameseCodeHelper.NormalizeVietnameseCode(QTCT.WorkPlaceName);
                    if (QTCT.WorkPlaceName == "OE Buôn Ma Thuột")
                    {
                        QTCT.WorkPlaceName = "OE BMT";
                    }
                    office = allOffice.FirstOrDefault(a => a.ShortName == QTCT.WorkPlaceName);
                    //if (office == null)
                    //{
                    zone = allZone.FirstOrDefault(a => a.Name == QTCT.WorkPlaceName);
                    if (zone == null && office == null)
                    {
                        logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + QTCT.WorkPlaceName);
                        //continue;
                    }
                    //}
                }
                var sort = _userTypeService.GetSort((TypeUser)type);
                var user = users.FirstOrDefault(a => a.MaNhanVien == item.MaNhanSu);
                user = UpdateUserInfo(user, item.MaNhanSu, item.TenNhanSu, QTCT.MaChucDanh, password, office, zone, type.Value, users, StatusUser.InActive, isToday, _unitOfWork, true);

                if (office == null && zone == null)
                {
                    logger.Error("Nhan su co noi lam viec null: " + item.MaNhanSu);
                    continue;
                }
                UpdateHistoryUserInfo(user, QTCT.MaChucDanh, office, zone, type.Value, ngayVaoLam.Value, ngayNghiViec, sort, listHistoryUser, StatusUser.InActive, currentMonth, currentYear, historyUserList, true);
            }
            // Quay lại xử lý ns đã nghỉ việc tháng trước - cập nhật muộn
            foreach (var item in DSNhanSuDelete)
            {
                if (string.IsNullOrEmpty(item.MaNhanSu))
                {
                    logger.Error("Khong co ma nhan su, IDNhanSuHRM: " + item.IDNhanSuHRM);
                    continue;
                }
                var user = users.FirstOrDefault(a => a.MaNhanVien == item.MaNhanSu);
                if (user != null)
                {
                    user.Active = false;
                    var historyUsers = listHistoryUser.Where(a => a.UserId == user.Id);
                    foreach (var h in historyUsers)
                    {
                        h.Active = false;
                    }
                }
            }

            if (historyUserList.Any())
                _unitOfWork.HistoryUserRepository.InsertRange(historyUserList);
            _unitOfWork.Save();

            // Báo cáo cuộc gọi, ĐB Sale
            var listReportCategoryId = new List<int> { 100, 26, 27, 99, 101 };
            var reportDatas = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && listReportCategoryId.Contains(a.ReportCategoryId));

            var listNewHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);
            var reportDataList = new List<ReportData>();

            // Cuộc gọi: CT,TD,HT Nhân sự; CT, TD Chi nhánh
            SyncBaoCaoCuocGoi(config, workingDayFull.Value, reportDatas, reportDataList, listNewHistoryUser, currentYear, currentMonth, lastMonth, endDayLastMonth, _unitOfWork);

            if (reportDataList.Any())
                _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
            _unitOfWork.Save();

            // Tính % HT cuộc gọi CN, ĐB sale, Chỉ tiêu CN - NV
            if (CheckAutoRevenue(config))
            {
                var reportDataList2 = new List<ReportData>();
                listReportCategoryId.AddRange(new List<int> { 28, 22, 23, 24 });
                reportDatas = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && listReportCategoryId.Contains(a.ReportCategoryId));

                // Tải trước các bản ghi vào bộ nhớ
                var historyOffices = _unitOfWork.HistoryOfficeRepository.Get(a => a.Active && a.Year == currentYear && a.Month == currentMonth);
                var historyUserMonthList = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Year == currentYear && a.Month == currentMonth
                    && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.SAB)
                    && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value.Month != currentMonth || (a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == currentMonth))));
                var revenueOffices = _unitOfWork.RevenueOfficeRepository.Get(a => a.Month == currentMonth && a.Year == currentYear);
                var revenueUsers = _unitOfWork.RevenueUser_MonthRepository.Get(a => a.Month == currentMonth && a.Year == currentYear);
                var listRevenueUserDataBases = _unitOfWork.RevenueUser_MonthRepository.Get(a => a.HistoryUserId != null
                       && (a.HistoryUser.TypeUser == TypeUser.EC || a.HistoryUser.TypeUser == TypeUser.ALT) && a.Month == currentMonth && a.Year == currentYear
                       && (a.HistoryUser.DayEnd == null || (a.HistoryUser.DayEnd != null && a.HistoryUser.DayEnd.Value.Month != currentMonth || (a.HistoryUser.DayEnd.Value.Day != 1 && a.HistoryUser.DayEnd.Value.Month == currentMonth))));
                var oldPosittions = _unitOfWork.HistoryUserRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.Status == StatusUser.Transfer, q => q.OrderByDescending(a => a.DayEnd));
                var NVKDLastMonths = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Year == yearLastMonth && a.Month == lastMonth && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT)
                                && a.DayStart <= endDayLastMonth && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value > endDayLastMonth)));


                var reportDatas87 = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 87);
                var reportDataHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 30);
                var reportDataCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 34);
                var reportTDHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 31);
                var datahtHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 32);
                var reportDatactHVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 95);
                var reportTDHVNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 96);
                var datahtHVNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 97);
                var reportTDDSNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 88);
                var datahtDSNVs = _unitOfWork.ReportDataRepository.Get(a => a.ReportCategoryId == 89 && a.Month == currentMonth && a.Year == currentYear);
                var reportTDDSCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 35);
                var datahtDSCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 36);

                var targetGroup = _unitOfWork.TargetGroupRepository.GetQuery(a => a.Active && a.Year == currentYear && a.Month == currentMonth).FirstOrDefault();
                if (targetGroup == null)
                {
                    logger.Error("Chưa có dữ liệu bảng chỉ tiêu NVĐT theo tháng - tháng" + currentMonth + "/" + currentYear);
                    return;
                }

                var reportDataListAdd = new List<ReportData>();
                var newRevenueList = new List<RevenueOffice>();
                var newRevenueList2 = new List<RevenueUser_Month>();

                foreach (var office in allOffice)
                {
                    #region chỉ tiêu doanh số
                    // Chỉ tiêu DS, thực đạt HV, chỉ tiêu phân bổ DS nhân sự

                    var historyOffice = historyOffices.FirstOrDefault(a => a.OfficeId == office.Id);
                    if (historyOffice == null)
                    {
                        logger.Error("Chưa có dữ liệu chi nhánh theo tháng chi nhánh " + office.ShortName);
                        continue;
                    }
                    if (historyOffice.DBEC + historyOffice.DBATL <= 0)
                    {
                        logger.Error("Định biên NVKD chi nhánh " + office.ShortName + " không hợp lệ");
                        continue;
                    }
                    if (historyOffice.BaseTarget == 0)
                    {
                        logger.Error("Không có chỉ tiêu cơ sở chi nhánh " + office.ShortName);
                        continue;
                    }

                    SyncChitieuDS(currentMonth, currentYear, workingDayFull.Value, lastMonth, yearLastMonth, endDayLastMonth, office, historyOffice, workingDay, targetGroup, workingDayLastYear, newRevenueList,
                        newRevenueList2, reportDataListAdd, historyUserMonthList, revenueOffices, listRevenueUserDataBases, revenueUsers,
                         NVKDLastMonths, reportDatactHVs, reportDatas87, reportTDDSNVs, datahtDSNVs, reportDataCNs,
                        reportTDDSCNs, datahtDSCNs, reportDataHVCNs);

                    #endregion

                    #region Cuộc gọi chi nhánh

                    var callTarget = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                    if (callTarget == null)
                        callTarget = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                    var callTD = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                    if (callTD == null)
                        callTD = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                    var callHT = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 28);
                    if (callHT == null)
                        callHT = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 28);
                    if (callTarget?.DataReal > 0 && callTD?.DataReal != null)
                    {
                        var callTargetInt = callTarget.DataReal ?? 1;
                        var callTDInt = callTD.DataReal ?? 0;
                        var ht = (callTDInt / callTargetInt);
                        if (callHT != null)
                        {
                            callHT.Data = (ht * 100).ToString("F2") + "%";
                            callHT.DataReal = ht;
                        }
                        else
                        {
                            callHT = new ReportData()
                            {
                                Data = (ht * 100).ToString("F2") + "%",
                                DataReal = ht,
                                Month = currentMonth,
                                Year = currentYear,
                                ReportCategoryId = 28,
                                //check null
                                OfficeId = office.Id,
                                Sort = 9,
                            };

                            reportDataList2.Add(callHT);
                        }
                    }

                    #endregion

                    #region Định biên Sale

                    var countNVKD = historyUserMonthList.Count(a => a.OfficeId == office.Id && a.Status == StatusUser.Active && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT));
                    var TDDBSale = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 23);
                    if (TDDBSale == null)
                        TDDBSale = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 23);
                    if (TDDBSale == null)
                    {
                        TDDBSale = new ReportData()
                        {
                            Data = countNVKD.ToString(),
                            DataReal = countNVKD,
                            Month = currentMonth,
                            Year = currentYear,
                            ReportCategoryId = 23,
                            OfficeId = office.Id,
                            Sort = 5,
                        };

                        reportDataList2.Add(TDDBSale);
                    }
                    else
                    {
                        TDDBSale.Data = countNVKD.ToString();
                        TDDBSale.DataReal = countNVKD;
                    }
                    var DBSale = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 22);
                    if (DBSale == null)
                        DBSale = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 22);
                    if (DBSale != null)
                    {
                        if (DBSale.DataReal > 0)
                        {
                            var ht = countNVKD / DBSale.DataReal;
                            var htDBSale = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 24);
                            if (htDBSale == null)
                                htDBSale = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 24);
                            if (htDBSale != null)
                            {
                                htDBSale.Data = ((ht ?? 0) * 100).ToString("F2") + "%";
                                htDBSale.DataReal = ht;
                            }
                            else
                            {
                                htDBSale = new ReportData()
                                {
                                    Data = ((ht ?? 0) * 100).ToString("F2") + "%",
                                    DataReal = ht,
                                    Month = currentMonth,
                                    Year = currentYear,
                                    ReportCategoryId = 24,
                                    OfficeId = office.Id,
                                    Sort = 6,
                                };

                                reportDataList2.Add(htDBSale);
                            }

                        }
                    }
                    else
                    {
                        DBSale = new ReportData()
                        {
                            Data = "",
                            DataReal = null,
                            Month = currentMonth,
                            Year = currentYear,
                            ReportCategoryId = 22,
                            OfficeId = office.Id,
                            Sort = 4,
                        };
                        reportDataList2.Add(DBSale);
                    }
                    #endregion
                }

                if (newRevenueList.Any())
                {
                    _unitOfWork.RevenueOfficeRepository.InsertRange(newRevenueList);
                }
                if (newRevenueList2.Any())
                {
                    _unitOfWork.RevenueUser_MonthRepository.InsertRange(newRevenueList2);
                }
                if (reportDataListAdd.Any())
                {
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataListAdd);
                }
                if (reportDataList2.Any())
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList2);
                _unitOfWork.Save();
            }

        }

        public async Task SyncUserAsync(DateTime date)
        {
            await Task.Run(() =>
            {
                SyncUser(date);
            });
        }
    }
}

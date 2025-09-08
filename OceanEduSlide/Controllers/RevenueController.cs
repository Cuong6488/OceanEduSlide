using Antlr.Runtime.Misc;
using ExcelDataReader;
using Helpers;
using Microsoft.Ajax.Utilities;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Z.EntityFramework.Plus;
using System.Security.Policy;
using System.Threading.Tasks;

namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class RevenueController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Fullname => RouteData.Values["Fullname"].ToString();
        #region ChiTieuCN_NV
        public ActionResult RankOffice()
        {
            return View();
        }
        [HttpPost]
        public ActionResult RankOffice(FormCollection fc)
        {
            var file = Request.Files["RankFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var ranks = _unitOfWork.RankOfficeRepository.GetQuery(a => a.Active && a.Year == DateTime.Now.Year && a.Month == DateTime.Now.Month);
                ranks.Delete();
                var month = DateTime.Now.Month;
                var year = DateTime.Now.Year;
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var zone = tbl.Rows[i][0].ToString().Trim();
                    var officename = tbl.Rows[i][1].ToString().Trim();

                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.Name == officename).FirstOrDefault();
                    if (office == null) continue;
                    var topht = tbl.Rows[i][2].ToString().Trim();
                    if (string.IsNullOrEmpty(topht)) continue;
                    var topdt = tbl.Rows[i][3].ToString().Trim();
                    if (string.IsNullOrEmpty(topdt)) continue;
                    var ptht = tbl.Rows[i][4].ToString().Trim().Replace("%", "");
                    if (string.IsNullOrEmpty(ptht)) continue;
                    var dsht = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(dsht)) continue;
                    //var ptdt = tbl.Rows[i][6].ToString().Trim().Replace("%", "");
                    //if (string.IsNullOrEmpty(ptdt)) continue;
                    //var dsdt = tbl.Rows[i][7].ToString().Trim();
                    //if (string.IsNullOrEmpty(dsdt)) continue;
                    var rank = new RankOffice
                    {
                        OfficeId = office.Id,
                        Month = month,
                        Year = year,
                        TopDT = topdt,
                        TopHT = topht,
                        DSHT = dsht,
                        //DSDT = dsdt,
                        PTHT = ptht,
                        //PTHTDT = ptdt,
                        Active = true,
                    };
                    _unitOfWork.RankOfficeRepository.Insert(rank);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index", "Vcms");
        }

        public ActionResult TargetOffice(string result = "")
        {
            ViewBag.Result = result;
            return View();
        }
        [HttpPost]
        public ActionResult TargetOffice(FormCollection fc)
        {
            var file = Request.Files["TargetOfficeFile"];
            if (file != null && file.ContentLength > 0)
            {

                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type2,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var newRevenueList = new List<RevenueOffice>();
                var newRevenueList2 = new List<RevenueUser_Month>();
                //var reportDataList = new List<ReportData>();
                //var historyOffices = _unitOfWork.HistoryOfficeRepository.GetQuery();
                //var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery();
                var offices = _unitOfWork.OfficeRepository.GetQuery();
                var workingDays = _unitOfWork.WorkingDayRepository.GetQuery();
                var targetGroups = _unitOfWork.TargetGroupRepository.GetQuery(a => a.Active);
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officeshortname = tbl.Rows[i][0].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeshortname).FirstOrDefault();
                    if (office == null)
                    {
                        ModelState.AddModelError("", @"Không tồn tại chi nhánh nào có tên ngắn là " + officeshortname);
                        return View();
                    }

                    var monthStr = tbl.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(monthStr))
                    {
                        ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " không có dữ liệu cột tháng");
                        return View();
                    }
                    if (!int.TryParse(monthStr, out var monthInt))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột tháng, chi nhánh " + officeshortname);
                        return View();
                    }
                    var yearStr = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(yearStr))
                    {
                        ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " không có dữ liệu cột năm");
                        return View();
                    }
                    if (!int.TryParse(yearStr, out var yearInt))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột năm, chi nhánh " + officeshortname);
                        return View();
                    }
                    var historyOffice = _unitOfWork.HistoryOfficeRepository.GetQuery(a => a.OfficeId == office.Id && a.Year == yearInt && a.Month == monthInt).FirstOrDefault();
                    if (historyOffice == null)
                    {
                        ModelState.AddModelError("", @"Chưa có dữ liệu chi nhánh theo tháng chi nhánh " + officeshortname);
                        return View();
                    }
                    if (historyOffice.DBEC + historyOffice.DBATL <= 0)
                    {
                        ModelState.AddModelError("", @"Định biên NVKD chi nhánh " + officeshortname + " không hợp lệ");
                        return View();
                    }
                    var workingDay = workingDays.FirstOrDefault(a => a.Year == yearInt);
                    if (workingDay == null)
                    {
                        ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + yearInt);
                        return View();
                    }
                    var workingDayLastYear = workingDays.FirstOrDefault(a => a.Year == yearInt - 1);
                    if (workingDayLastYear == null)
                    {
                        ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + (yearInt - 1));
                        return View();
                    }
                    var targetGroup = targetGroups.FirstOrDefault(a => a.Year == yearInt && a.Month == monthInt);
                    if (targetGroup == null)
                    {
                        ModelState.AddModelError("", @"Chưa có dữ liệu bảng chỉ tiêu NVĐT theo tháng - tháng" + monthInt + "/" + yearInt);
                        return View();
                    }
                    var targetBase = tbl.Rows[i][9].ToString().Trim();
                    if (string.IsNullOrEmpty(targetBase))
                    {
                        ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " không có dữ liệu cột Chỉ tiêu Doanh số cơ sở");
                        return View();
                    }
                    if (!decimal.TryParse(targetBase, out var targetBaseDec))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột Chỉ tiêu Doanh số cơ sở, chi nhánh " + officeshortname);
                        return View();
                    }
                    int workingDayFull = 1;
                    switch (monthInt)
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
                    var historyUserMonths = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == yearInt && a.Month == monthInt && a.OfficeId == office.Id
                    && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.SAB)
                    && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value.Month != monthInt || (a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == monthInt))));
                    var revenueOffice = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id).FirstOrDefault();
                    if (revenueOffice == null)
                    {
                        revenueOffice = new RevenueOffice
                        {
                            Target_TS = 0,
                            Target_HV = 0,
                            Target_SAB = 0,
                            Month = monthInt,
                            Year = yearInt,
                            OfficeId = office.Id,
                        };
                        newRevenueList.Add(revenueOffice);
                        //_unitOfWork.RevenueOfficeRepository.Insert(revenueOffice);
                        //_unitOfWork.Save();
                    }
                    else
                    {
                        revenueOffice.Target_TS = 0;
                        revenueOffice.Target_SAB = 0;
                        revenueOffice.Target_HV = 0;
                    }
                    var DBKD = historyOffice.DBEC + historyOffice.DBATL;
                    foreach (var item in historyUserMonths)
                    {

                        decimal targetNS = 0;
                        if (item.TypeUser == TypeUser.EC || item.TypeUser == TypeUser.ALT)
                        {
                            HistoryUser oldPosittion = null;
                            var startDateReal = item.DayStart;
                            if (item.Status == StatusUser.Active)
                            {
                                oldPosittion = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == item.UserId && a.Month == monthInt && a.Year == yearInt && a.Status == StatusUser.Transfer, q => q.OrderByDescending(a => a.DayEnd)).FirstOrDefault();
                                if (oldPosittion != null)
                                {
                                    if (oldPosittion.DayEnd == null)
                                    {
                                        ModelState.AddModelError("", @"Nhân sự điều chuyển " + oldPosittion.User.MaNhanVien + " không có ngày điều chuyển");
                                        return View();
                                    }
                                    startDateReal = oldPosittion.DayEnd.Value;
                                }
                            }
                            int workingDayTT = 0;
                            bool nsFullTarget = true;
                            int lastMonth = 0;
                            int yearLastMonth = 0;
                            if (monthInt == 1)
                            {
                                lastMonth = 12;
                                yearLastMonth = yearInt - 1;
                            }
                            else
                            {
                                lastMonth = monthInt - 1;
                                yearLastMonth = yearInt;
                            }
                            DateTime endDayLastMonth = new DateTime(yearLastMonth, lastMonth, DateTime.DaysInMonth(yearLastMonth, lastMonth));
                            if ((startDateReal.Year < yearInt || (startDateReal.Year == yearInt && startDateReal.Month < monthInt)) && (item.DayEnd == null || (item.DayEnd != null && item.DayEnd.Value.Month > monthInt)))
                            {
                                workingDayTT = workingDayFull;
                            }
                            else
                            {

                                DateTime ngayBatDau = item.DayStart.Year < yearInt || (item.DayStart.Year == yearInt && item.DayStart.Month < monthInt) ? new DateTime(yearInt, monthInt, 1) : item.DayStart;
                                if (oldPosittion != null)
                                {
                                    var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                                    var oldDayWork = oldDayFull - (oldDayFull / 7);
                                    workingDayTT = Math.Max(workingDayFull - oldDayWork, 0);
                                }
                                else
                                {
                                    DateTime ngayKetThuc = item.DayEnd != null ? item.DayEnd.Value.AddDays(-1) : new DateTime(yearInt, monthInt, DateTime.DaysInMonth(yearInt, monthInt));
                                    // Số ngày làm việc + nghỉ
                                    int soNgayLamViec = (ngayKetThuc - ngayBatDau).Days + 1;
                                    if (soNgayLamViec < 0)
                                    {
                                        ModelState.AddModelError("", @"Nhân viên " + item.User.MaNhanVien + " có ngày vào làm > ngày nghỉ việc");
                                        return View();
                                    }
                                    int soNgayNghi = soNgayLamViec / 7;
                                    workingDayTT = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                                }

                            }
                            decimal targetDBCS = targetBaseDec / DBKD;
                            targetNS = targetDBCS * ((decimal)workingDayTT / workingDayFull);
                            if (item.DayStart.Month == monthInt && item.DayStart.Year == yearInt)
                            {
                                nsFullTarget = false;
                                targetNS = targetNS / 2;
                            }
                            else if (item.DayStart.Month == lastMonth && item.DayStart.Year == yearLastMonth)
                            {
                                var dayLastMonth = 0;
                                var day50Total = 0;
                                switch (monthInt)
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
                                var dayFree = dayTotal / 7;
                                //Số ngày làm việc tháng trước
                                dayLastMonth = Math.Min(dayTotal - dayFree, day50Total);
                                //Số ngày làm việc tính 50% chỉ tiêu tháng này
                                var dayThisMonth50 = Math.Min(day50Total - dayLastMonth, workingDayTT);
                                //Số ngày làm việc tính 100% chỉ tiêu tháng này
                                var dayThisMonthFull = Math.Max(workingDayTT - dayThisMonth50, 0);
                                if (dayThisMonthFull < workingDayFull)
                                    nsFullTarget = false;
                                decimal targetNS50 = 0;
                                decimal targetNSFull = 0;
                                targetNS50 = targetDBCS * dayThisMonth50 / workingDayFull / 2;
                                targetNSFull = targetDBCS * dayThisMonthFull / workingDayFull;
                                targetNS = targetNS50 + targetNSFull;
                            }
                            if (historyOffice.QD156 && nsFullTarget)
                            {
                                var countNVKDLastMonth = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == yearInt && a.Month == monthInt && a.OfficeId == office.Id && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT)
                                && a.DayStart <= endDayLastMonth && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value > endDayLastMonth))).Count();
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
                                default:
                                    break;
                            }
                            revenueOffice.Target_HV += targetNS;
                        }
                        else if (item.TypeUser == TypeUser.SAB)
                        {
                            targetNS = targetBaseDec / (historyOffice.DBEC + historyOffice.DBATL) * 65 / 100;
                            revenueOffice.Target_SAB += targetNS;
                        }
                        // Chỉ tiêu báo cáo nhân sự
                        //var reportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 87).FirstOrDefault();
                        //if (reportData == null)
                        //{
                        //    reportData = new ReportData()
                        //    {
                        //        Data = targetNS.ToString("N0"),
                        //        UserId = item.UserId,
                        //        HistoryUserId = item.Id,
                        //        Month = monthInt,
                        //        Year = yearInt,
                        //        ReportCategoryId = 87,
                        //        OfficeId = office.Id,
                        //        Sort = 10,
                        //    };
                        //    reportDataList.Add(reportData);
                        //}
                        //else
                        //{
                        //    reportData.Data = targetNS.ToString("N0");
                        //}
                        var r = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt).FirstOrDefault();
                        if (r != null)
                        {
                            r.Target = targetNS;
                        }
                        else
                        {
                            var rnew = new RevenueUser_Month
                            {
                                Target = targetNS,
                                Month = monthInt,
                                Year = yearInt,
                                UserId = item.UserId,
                                HistoryUserId = item.Id,

                            };
                            newRevenueList2.Add(rnew);
                        }

                    }
                    revenueOffice.Target_TS = Math.Max(targetBaseDec, revenueOffice.Target_TS);
                    // Chỉ tiêu báo cáo chi nhánh
                    //var reportDataCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 34).FirstOrDefault();
                    //if (reportDataCN == null)
                    //{
                    //    reportDataCN = new ReportData()
                    //    {
                    //        Data = revenueOffice.Target_TS.ToString("N0"),
                    //        Month = monthInt,
                    //        Year = yearInt,
                    //        ReportCategoryId = 34,
                    //        OfficeId = office.Id,
                    //        Sort = 13,
                    //    };
                    //    reportDataList.Add(reportDataCN);
                    //}
                    //else
                    //{
                    //    reportDataCN.Data = revenueOffice.Target_TS.ToString("N0");
                    //}

                }

                if (newRevenueList.Any())
                {
                    _unitOfWork.RevenueOfficeRepository.InsertRange(newRevenueList);
                }
                if (newRevenueList2.Any())
                {
                    _unitOfWork.RevenueUser_MonthRepository.InsertRange(newRevenueList2);
                }
                //if (reportDataList.Any())
                //{
                //    _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
                //}
                _unitOfWork.Save();

                var tbl3 = result.Tables[1];
                var listRevenue = new List<RevenueUser_Week_Real>();
                for (var i = 1; i < tbl3.Rows.Count; i++)
                {
                    var manhanvien = tbl3.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == manhanvien).FirstOrDefault();
                    if (user == null) continue;
                    var officeSortName = tbl3.Rows[i][1].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeSortName).FirstOrDefault();
                    if (office == null) continue;
                    var typeUser = tbl3.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(typeUser))
                        continue;
                    TypeUser type = new TypeUser();
                    switch (typeUser)
                    {
                        case "EC":
                            type = TypeUser.EC;
                            break;
                        case "BM":
                            type = TypeUser.BM;
                            break;
                        case "BSA":
                            type = TypeUser.SAB;
                            break;
                        case "SAB":
                            type = TypeUser.SAB;
                            break;
                        case "ATL":
                            type = TypeUser.ALT;
                            break;
                        case "CM":
                            type = TypeUser.CM;
                            break;
                        case "TTL":
                            type = TypeUser.TTL;
                            break;
                        default:
                            break;
                    }
                    var month = tbl3.Rows[i][6].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    var monthInt = int.Parse(month);
                    var year = tbl3.Rows[i][7].ToString().Trim();
                    if (string.IsNullOrEmpty(year)) continue;
                    var yearInt = int.Parse(year);
                    var week = tbl3.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(week)) continue;
                    var weekInt = int.Parse(week);
                    var historyUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == user.Id && a.OfficeId == office.Id && a.TypeUser == type && a.Month == monthInt && a.Year == yearInt).FirstOrDefault();
                    if (historyUser == null) continue;
                    var ds = tbl3.Rows[i][8].ToString().Trim();
                    decimal dsDec = string.IsNullOrEmpty(ds) ? 0 : decimal.Parse(ds);
                    var revenue = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && (int)a.WeekNumber == weekInt).FirstOrDefault();
                    if (revenue != null)
                    {
                        revenue.TargetBM = dsDec;
                    }
                    else
                    {
                        var newRevenue = new RevenueUser_Week_Real
                        {
                            UserId = user.Id,
                            HistoryUserId = historyUser.Id,
                            Month = monthInt,
                            Year = yearInt,
                            TargetBM = dsDec,
                            Active = true,
                        };
                        switch (weekInt)
                        {
                            case 1:
                                newRevenue.WeekNumber = WeekNumber.Week1;
                                break;
                            case 2:
                                newRevenue.WeekNumber = WeekNumber.Week2;
                                break;
                            case 3:
                                newRevenue.WeekNumber = WeekNumber.Week3;
                                break;
                            case 4:
                                newRevenue.WeekNumber = WeekNumber.Week4;
                                break;
                            case 5:
                                newRevenue.WeekNumber = WeekNumber.Week5;
                                break;
                            case 6:
                                newRevenue.WeekNumber = WeekNumber.Week6;
                                break;
                            default:
                                break;
                        }
                        //_unitOfWork.RevenueUser_Week_RealRepository.Insert(newRevenue);
                        listRevenue.Add(newRevenue);
                    }

                }
                if (listRevenue.Any())
                    _unitOfWork.RevenueUser_Week_RealRepository.InsertRange(listRevenue);
                _unitOfWork.Save();
                ViewBag.Result = "add";
                return View();

            }

            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult TargetOffice2()
        {
            return View();
        }
        [HttpPost]
        public ActionResult TargetOffice2(FormCollection fc)
        {
            var file = Request.Files["TargetOfficeFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type2,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var newRevenueList = new List<RevenueOffice>();

                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officeshortname = tbl.Rows[i][0].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeshortname).FirstOrDefault();
                    if (office == null) continue;

                    var countNVHV = _unitOfWork.UserRepository
                        .GetQuery(a => (a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL) && a.OfficeId == office.Id)
                        .Count();

                    var countNVKT = _unitOfWork.UserRepository
                        .GetQuery(a => a.TypeUser == TypeUser.SAB && a.OfficeId == office.Id)
                        .Count();

                    var monthStr = tbl.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(monthStr)) continue;
                    if (!int.TryParse(monthStr, out var monthInt)) continue;

                    var yearStr = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(yearStr)) continue;
                    if (!int.TryParse(yearStr, out var yearInt)) continue;

                    var targetTS = tbl.Rows[i][10].ToString().Trim();
                    if (string.IsNullOrEmpty(targetTS)) continue;
                    if (!decimal.TryParse(targetTS, out var targetTSDec)) continue;

                    var revenue = _unitOfWork.RevenueOfficeRepository
                        .GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt)
                        .FirstOrDefault();

                    if (revenue != null)
                    {
                        revenue.Target_TS = targetTSDec;
                    }
                    else
                    {
                        var newRevenue = new RevenueOffice
                        {
                            OfficeId = office.Id,
                            Month = monthInt,
                            Year = yearInt,
                            Target_TS = targetTSDec,
                            Active = true,
                        };
                        newRevenueList.Add(newRevenue);
                    }
                }

                if (newRevenueList.Any())
                {
                    _unitOfWork.RevenueOfficeRepository.InsertRange(newRevenueList);
                }

                _unitOfWork.Save();
                var tbl2 = result.Tables[1];

                var newRevenueList2 = new List<RevenueUser_Month>();
                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery();
                for (var i = 1; i < tbl2.Rows.Count; i++)
                {
                    var manhanvien = tbl2.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository
                        .GetQuery(a => a.MaNhanVien == manhanvien)
                        .FirstOrDefault();
                    if (user == null) continue;

                    var officeSortName = tbl2.Rows[i][1].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeSortName).FirstOrDefault();
                    if (office == null) continue;
                    var typeUser = tbl2.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(typeUser))
                        continue;
                    TypeUser type = new TypeUser();
                    switch (typeUser)
                    {
                        case "EC":
                            type = TypeUser.EC;
                            break;
                        case "BM":
                            type = TypeUser.BM;
                            break;
                        case "BSA":
                            type = TypeUser.SAB;
                            break;
                        case "SAB":
                            type = TypeUser.SAB;
                            break;
                        case "ATL":
                            type = TypeUser.ALT;
                            break;
                        case "CM":
                            type = TypeUser.CM;
                            break;
                        case "TTL":
                            type = TypeUser.TTL;
                            break;
                        default:
                            break;
                    }
                    var monthStr = tbl2.Rows[i][20].ToString().Trim();
                    if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt)) continue;

                    var yearStr = tbl2.Rows[i][21].ToString().Trim();
                    if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt)) continue;

                    var targetStr = tbl2.Rows[i][12].ToString().Trim();
                    if (string.IsNullOrEmpty(targetStr) || !decimal.TryParse(targetStr, out var targetDec)) continue;
                    var historyUser = historyUsers.FirstOrDefault(a => a.UserId == user.Id && a.OfficeId == office.Id && a.TypeUser == type && a.Month == monthInt && a.Year == yearInt);
                    if (historyUser == null) continue;
                    var revenue = _unitOfWork.RevenueUser_MonthRepository
                        .GetQuery(a => a.UserId == user.Id && a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt)
                        .FirstOrDefault();

                    if (revenue != null)
                    {
                        revenue.Target = targetDec;
                    }
                    else
                    {
                        var newRevenue = new RevenueUser_Month
                        {
                            UserId = user.Id,
                            HistoryUserId = historyUser.Id,
                            Month = monthInt,
                            Year = yearInt,
                            Target = targetDec,
                            Active = true
                        };
                        newRevenueList2.Add(newRevenue);
                    }
                }

                if (newRevenueList2.Any())
                {
                    _unitOfWork.RevenueUser_MonthRepository.InsertRange(newRevenueList2);
                }
                _unitOfWork.Save();
            }

            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult ListRevenueOffice(int? page, int? officeId, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var revenueOffices = _unitOfWork.RevenueOfficeRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenByDescending(a => a.Target_TS)).AsNoTracking();

            if (officeId > 0)
            {
                revenueOffices = revenueOffices.Where(a => a.OfficeId == officeId);
            }
            var model = new ListRevenueOfficeViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "Name"),
                RevenueOffices = revenueOffices.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
            };
            return View(model);
        }
        public ActionResult PhieuThuTHDB()
        {
            return View();
        }
        [HttpPost]
        public ActionResult PhieuThuTHDB(FormCollection fc,int Month)
        {
            var file = Request.Files["PhieuThuFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type10,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var ngayThanhToanStr1 = tbl.Rows[1][1].ToString().Trim();
                if (string.IsNullOrEmpty(ngayThanhToanStr1))
                {
                    ModelState.AddModelError("", @"Thiếu dữ liệu cột Ngày thanh toán");
                    return View();
                }

                if (!DateTime.TryParse(ngayThanhToanStr1, out var ngayThanhToan1))
                {
                    ModelState.AddModelError("", @"Không thể chuyển đổi thành ngày ở cột ngày thanh toán: Dòng 1");
                    return View();
                }
                var oldList = _unitOfWork.PhieuThuRepository.GetQuery(a => a.THDB && a.NgayThanhToan != null && a.NgayThanhToan.Value.Year == ngayThanhToan1.Year && a.ThangTinhDThu == Month);
                oldList.Delete();
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officename = tbl.Rows[i][0].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officename).FirstOrDefault();
                    if (office == null)
                    {
                        //continue;

                        ModelState.AddModelError("", @"Không có chi nhánh nào có tên ngắn là " + officename);
                        return View();
                    }
                    var ngayThanhToanStr = tbl.Rows[i][1].ToString().Trim();
                    if (string.IsNullOrEmpty(ngayThanhToanStr))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Ngày thanh toán");
                        return View();
                    }

                    if (!DateTime.TryParse(ngayThanhToanStr, out var ngayThanhToan))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành ngày ở cột ngày thanh toán: " + ngayThanhToanStr + ", chi nhánh " + officename);
                        return View();
                    }
                    var loai = tbl.Rows[i][2].ToString().Trim();
                    if (string.IsNullOrEmpty(loai))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Loại");
                        return View();
                    }
                    var receiptCode = tbl.Rows[i][3].ToString().Trim();
                    var maHV = tbl.Rows[i][4].ToString().Trim();
                    var tenHV = tbl.Rows[i][5].ToString().Trim();
                    var tUDStr = tbl.Rows[i][6].ToString().Trim();
                    if (!decimal.TryParse(tUDStr, out var tUD))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột trước ưu đãi: " + tUDStr + ", chi nhánh " + officename);
                        return View();
                    }

                    var sUDStr = tbl.Rows[i][7].ToString().Trim();
                    if (string.IsNullOrEmpty(sUDStr))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Sau ưu đãi");
                        return View();
                    }
                    if (!decimal.TryParse(sUDStr, out var sUD))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột sau ưu đãi: " + sUDStr + ", chi nhánh " + officename);
                        return View();
                    }
                    var phanTramUDStr = tbl.Rows[i][8].ToString().Trim();
                    if (!double.TryParse(phanTramUDStr, out var phanTramUD))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột % ưu đãi: " + phanTramUDStr + ", chi nhánh " + officename);
                        return View();
                    }
                    var gioiTinh = tbl.Rows[i][9].ToString().Trim();
                    var hinhThucThanhToan = tbl.Rows[i][10].ToString().Trim();
                    var notes = tbl.Rows[i][11].ToString().Trim();
                    var dangKy = tbl.Rows[i][12].ToString().Trim();
                    var gioTaoStr = tbl.Rows[i][13].ToString().Trim();
                    if (!DateTime.TryParse(gioTaoStr, out var gioTao))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành giờ ở cột giờ tạo: " + gioTaoStr + ", chi nhánh " + officename);
                        return View();
                    }

                    var chotSale = tbl.Rows[i][15].ToString().Trim();
                    var congTacVien = tbl.Rows[i][16].ToString().Trim();
                    var thangHocDuKienStr = tbl.Rows[i][17].ToString().Trim();
                    if (!decimal.TryParse(thangHocDuKienStr, out var thangHocDuKien))
                    {
                        ModelState.AddModelError("", @"Không thể chuyển đổi thành số ở cột tháng học dự kiến: " + thangHocDuKienStr + ", chi nhánh " + officename);
                        return View();
                    }
                    var uD_FINAL = tbl.Rows[i][18].ToString().Trim();
                    var loaiCTH = tbl.Rows[i][19].ToString().Trim();
                    var chuongTrinhHoc = tbl.Rows[i][20].ToString().Trim();
                    var capDo = tbl.Rows[i][21].ToString().Trim();
                    var modun = tbl.Rows[i][22].ToString().Trim();
                    var uD_NhomUDFINAL = tbl.Rows[i][23].ToString().Trim();
                    var maNVChotSale = tbl.Rows[i][14].ToString().Trim();
                    if (!string.IsNullOrEmpty(maNVChotSale))
                    {
                        var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == maNVChotSale).FirstOrDefault();
                        if (user == null)
                        {
                            ModelState.AddModelError("", @"Chưa có tài khoản của nhân sự: " + maNVChotSale);
                            return View();
                        }
                        var historyUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == user.Id && a.DayStart <= ngayThanhToan && (a.DayEnd == null || a.DayEnd.Value >= ngayThanhToan)).FirstOrDefault();
                        if (historyUser == null)
                        {
                            ModelState.AddModelError("", @"Chưa có bản ghi nhân sự theo tháng của nhân sự: " + maNVChotSale);
                            return View();
                        }
                    }
                    var phieuThu = new BC_PhieuThu_DB
                    {
                        ChiNhanh = officename,
                        MaNVChotSale = maNVChotSale,
                        NgayThanhToan = ngayThanhToan,
                        SUD = sUD,
                        TUD = tUD,
                        Loai = loai,
                        ReceiptCode = receiptCode,
                        MaHV = maHV,
                        TenHV = tenHV,
                        PhanTramUD = phanTramUD,
                        GioiTinh = gioiTinh,
                        HinhThucThanhToan = hinhThucThanhToan,
                        Notes = notes,
                        DangKy = dangKy,
                        GioTao = gioTao,
                        ChotSale = chotSale,
                        CongTacVien = congTacVien,
                        ThangHocDuKien = (int)thangHocDuKien,
                        UD_FINAL = uD_FINAL,
                        LoaiCTH = loaiCTH,
                        ChuongTrinhHoc = chuongTrinhHoc,
                        CapDo = capDo,
                        Modun = modun,
                        UD_NhomUDFINAL = uD_NhomUDFINAL,
                        THDB = true,
                        ThangTinhDThu = Month

                    };
                    _unitOfWork.PhieuThuRepository.Insert(phieuThu);
                }
                _unitOfWork.Save();
                var phieuThuSerVice = new PhieuThuService();
                phieuThuSerVice.SyncDthu(Month,ngayThanhToan1.Year);
                ViewBag.Result = "add";
                return View();
            }
            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult ListPhieuThu(int? page, string username, string officeId, int? month, int? year, int? type, string result = "")
        {
            var pageNumber = page ?? 1;
            const int pageSize = 20;
            var phieuThus = _unitOfWork.PhieuThuRepository.GetQuery(orderBy: q => q.OrderBy(a => a.ChiNhanh).ThenBy(a => a.MaNVChotSale));
            if (!string.IsNullOrEmpty(officeId))
            {
                phieuThus = phieuThus.Where(l => l.ChiNhanh == officeId);
            }
            if (month != null)
                phieuThus = phieuThus.Where(l => l.ThangTinhDThu == month);

            if (year != null)
                phieuThus = phieuThus.Where(l => l.NgayThanhToan != null && l.NgayThanhToan.Value.Year == year);

            if (type == 0)
            {
                phieuThus = phieuThus.Where(l => !l.THDB);
            }
            if (type == 1)
            {
                phieuThus = phieuThus.Where(l => l.THDB);
            }
            if (username != null)
            {
                var newkey = username.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.MaNVChotSale.Contains(newkey) || l.MaNVChotSale.Contains(newkey) || l.MaNVChotSale.Contains(newkey));
                }
            }

            var model = new ListPhieuThuViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "ShortName", "Name"),
                PhieuThus = phieuThus.ToPagedList(pageNumber, pageSize),
                officeId = officeId,
                Username = username,
                type = type,
                year = year,
                month = month,
            };
            return View(model);
        }

        #endregion

        #region LogImport
        public PartialViewResult ListFile(int type)
        {
            var typeName = "";

            switch (type)
            {
                case 0:
                    typeName = "Báo cáo TH CN - NV";
                    break;
                case 1:
                    typeName = "Bhỉ tiêu CN - NV - DS hoàn thành thực tế tuần";
                    break;
                case 2:
                    typeName = "Vùng";
                    break;
                case 3:
                    typeName = "Chi nhánh";
                    break;
                case 4:
                    typeName = "Tài khoản nhân sự";
                    break;
                case 5:
                    typeName = "Nhân sự theo tháng";
                    break;
                case 6:
                    typeName = "QĐ ưu đãi";
                    break;
                case 7:
                    typeName = "Quy định chung/ QĐ PTS";
                    break;
                case 8:
                    typeName = "Chi nhánh theo tháng";
                    break;
                case 9:
                    typeName = "Phiếu thu đặc biệt";
                    break;
                default:
                    break;
            }
            ViewBag.TypeName = typeName;
            var logImport = _unitOfWork.LogImportRepository.GetQuery(a => (int)a.TypeImport == type && a.CreateDate.Month == DateTime.Now.Month,q => q.OrderByDescending(a => a.CreateDate));
            return PartialView(logImport);
        }
        public ActionResult ListFileAll(int? page, int? type)
        {
            var pageNumber = page ?? 1;
            const int pageSize = 20;
            var files = _unitOfWork.LogImportRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.CreateDate).ThenBy(a => a.TypeImport));
            if (type != null)
            {
                files = files.Where(a => (int)a.TypeImport == type);
            }
            var model = new ListFileAllViewModel
            {
                LogImports = files.ToPagedList(pageNumber, pageSize),
                TypeFile = type
            };
            return View(model);
        }
        #endregion

        #region WorkingDays
        public ActionResult CreateWorkingDay(string result = "")
        {
            ViewBag.Result = result;
            return View(new WorkingDay { Year = DateTime.Now.Year });
        }
        [HttpPost]
        public ActionResult CreateWorkingDay(WorkingDay model)
        {
            if (ModelState.IsValid)
            {
                var workingDayOld = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == model.Year);
                if (workingDayOld.Any())
                {
                    ModelState.AddModelError("", @"Đã tồn tại bản ghi của năm " + model.Year);
                    return View(model);
                }
                _unitOfWork.WorkingDayRepository.Insert(model);
                _unitOfWork.Save();
                return RedirectToAction("ListWorkingDay", new { result = "success" });
            }
            return View(model);
        }
        public ActionResult UpdateWorkingDay(int workingDayId)
        {
            var workingDay = _unitOfWork.WorkingDayRepository.GetById(workingDayId);
            if (workingDay == null)
                return RedirectToAction("ListWorkingDay");
            return View(workingDay);
        }
        [HttpPost]
        public ActionResult UpdateWorkingDay(WorkingDay model)
        {
            if (ModelState.IsValid)
            {
                //var workingDay = _unitOfWork.WorkingDayRepository.GetById(model.Id);
                //if (workingDay == null)
                //    return RedirectToAction("ListWorkingDay");
                _unitOfWork.WorkingDayRepository.Update(model);
                _unitOfWork.Save();
                return RedirectToAction("ListWorkingDay", new { result = "update" });
            }
            return View(model);
        }
        public ActionResult ListWorkingDay(string result = "")
        {
            ViewBag.Result = result;
            var workingDays = _unitOfWork.WorkingDayRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year));

            return View(workingDays);
        }
        #endregion

        #region TargetGroup
        public ActionResult CreateTargetGroup(string result = "")
        {
            ViewBag.Result = result;
            var model = new InsertTargetGroupViewModel
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateTargetGroup(InsertTargetGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var targetOld = _unitOfWork.TargetGroupRepository.GetQuery(a => a.Year == model.Year && a.Month == model.Month);
                if (targetOld.Any())
                {
                    ModelState.AddModelError("", @"Đã tồn tại bản ghi của tháng " + model.Month + " - " + model.Year);
                    return View(model);
                }
                var targetGroup = new TargetGroup()
                {
                    Month = model.Month,
                    Year = model.Year,
                    Target_A = Convert.ToDecimal(model.Target_A.Replace(",", "")),
                    Target_B = Convert.ToDecimal(model.Target_B.Replace(",", "")),
                    Target_C = Convert.ToDecimal(model.Target_C.Replace(",", "")),
                    Target_D = Convert.ToDecimal(model.Target_D.Replace(",", "")),

                };
                _unitOfWork.TargetGroupRepository.Insert(targetGroup);
                _unitOfWork.Save();
                return RedirectToAction("ListTargetGroup", new { result = "success" });
            }
            return View(model);
        }

        public ActionResult UpdateTargetGroup(int targetId)
        {
            var target = _unitOfWork.TargetGroupRepository.GetById(targetId);
            if (target == null)
                return RedirectToAction("ListTargetGroup");
            var model = new InsertTargetGroupViewModel
            {
                Target_A = target.Target_A.ToString("N0"),
                Target_B = target.Target_B.ToString("N0"),
                Target_C = target.Target_C.ToString("N0"),
                Target_D = target.Target_D.ToString("N0"),
                Month = target.Month,
                Year = target.Year,
                TargetGroupId = target.Id,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateTargetGroup(InsertTargetGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                var targetGroup = _unitOfWork.TargetGroupRepository.GetById(model.TargetGroupId);
                if (targetGroup == null)
                    return RedirectToAction("ListTargetGroup");
                targetGroup.Target_A = Convert.ToDecimal(model.Target_A.Replace(",", ""));
                targetGroup.Target_B = Convert.ToDecimal(model.Target_B.Replace(",", ""));
                targetGroup.Target_C = Convert.ToDecimal(model.Target_C.Replace(",", ""));
                targetGroup.Target_D = Convert.ToDecimal(model.Target_D.Replace(",", ""));
                _unitOfWork.Save();
                return RedirectToAction("ListTargetGroup", new { result = "update" });
            }
            return View(model);
        }
        public ActionResult ListTargetGroup(int? page, int? month, int? year, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 12;
            var targetgroups = _unitOfWork.TargetGroupRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month));
            if (month != null)
                targetgroups = targetgroups.Where(l => l.Month == month);

            if (year != null)
                targetgroups = targetgroups.Where(l => l.Year == year);

            var model = new ListTargetGroupViewModel
            {
                TargetGroups = targetgroups.ToPagedList(pageNumber, pageSize),
                month = month,
                year = year,
            };
            return View(model);
        }
        #endregion


        public ActionResult ListRevenueUser(int? page, int? officeId, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var revenueUsers = _unitOfWork.RevenueUser_MonthRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenByDescending(a => a.Target)).AsNoTracking();

            if (officeId > 0)
            {
                revenueUsers = revenueUsers.Where(a => a.User.OfficeId == officeId);
            }
            var model = new ListRevenueUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "Name"),
                Revenues = revenueUsers.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
            };
            return View(model);
        }
        public ActionResult TestSyncPhieuThu(int date, int month)
        {
            var phieuthuService = new PhieuThuService();
            phieuthuService.TestSyncPhieuThu(date,month);
            return Content("Đã đồng bộ phiếu thu");
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
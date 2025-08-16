using Antlr.Runtime.Misc;
using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Z.EntityFramework.Plus;

namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class RevenueController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Fullname => RouteData.Values["Fullname"].ToString();

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
                    var ptdt = tbl.Rows[i][6].ToString().Trim().Replace("%", "");
                    if (string.IsNullOrEmpty(ptdt)) continue;
                    var dsdt = tbl.Rows[i][7].ToString().Trim();
                    if (string.IsNullOrEmpty(dsdt)) continue;
                    var rank = new RankOffice
                    {
                        OfficeId = office.Id,
                        Month = month,
                        Year = year,
                        TopDT = topdt,
                        TopHT = topht,
                        DSHT = dsht,
                        DSDT = dsdt,
                        PTHT = ptht,
                        PTHTDT = ptdt,
                        Active = true,
                    };
                    _unitOfWork.RankOfficeRepository.Insert(rank);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index", "Vcms");
        }

        public ActionResult TargetOffice()
        {
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

                    //var targetKT = tbl.Rows[i][12].ToString().Trim();
                    //if (string.IsNullOrEmpty(targetKT)) continue;
                    //if (!decimal.TryParse(targetKT, out var targetKTDec)) continue;

                    //var targetHV = tbl.Rows[i][13].ToString().Trim();
                    //if (string.IsNullOrEmpty(targetHV)) continue;
                    //if (!decimal.TryParse(targetHV, out var targetHVDec)) continue;

                    var revenue = _unitOfWork.RevenueOfficeRepository
                        .GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt)
                        .FirstOrDefault();

                    if (revenue != null)
                    {
                        revenue.Target_TS = targetTSDec;
                        //revenue.Target_HV = targetHVDec * countNVHV;
                        //revenue.Target_SAB = targetKTDec * countNVKT;
                    }
                    else
                    {
                        var newRevenue = new RevenueOffice
                        {
                            OfficeId = office.Id,
                            Month = monthInt,
                            Year = yearInt,
                            Target_TS = targetTSDec,
                            //Target_HV = targetHVDec * countNVHV,
                            //Target_SAB = targetKTDec * countNVKT,
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
                    if(manhanvien == "10183255")
                    {

                    }
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
                var tbl3 = result.Tables[2];
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
                    var historyUser = historyUsers.FirstOrDefault(a => a.UserId == user.Id && a.OfficeId == office.Id && a.TypeUser == type && a.Month == monthInt && a.Year == yearInt);
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
                //var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                //HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                //var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                //var logImport = new Models.LogImport
                //{
                //    Admin = Fullname,
                //    Name = Path.GetFileName(file.FileName),
                //    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                //    TypeImport = TypeImport.Type2,
                //};
                //_unitOfWork.LogImportRepository.Insert(logImport);
                //_unitOfWork.Save();
                //// Lưu tệp tài liệu
                //var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                //file.SaveAs(filePath);
            }

            return RedirectToAction("Index", "Vcms");
        }

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
                default:
                    break;
            }
            ViewBag.TypeName = typeName;
            var logImport = _unitOfWork.LogImportRepository.GetQuery(a => (int)a.TypeImport == type);
            return PartialView(logImport);
        }
        //public ActionResult TargetUser()
        //{
        //    return View();
        //}
        [HttpPost]
       
        public ActionResult TargetUser(FormCollection fc)
        {
            var file = Request.Files["TargetUserFile"];
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

                var newRevenueList = new List<RevenueUser_Month>();

                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var manhanvien = tbl.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository
                        .GetQuery(a => a.MaNhanVien == manhanvien)
                        .FirstOrDefault();
                    if (user == null) continue;

                    var monthStr = tbl.Rows[i][20].ToString().Trim();
                    if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt)) continue;

                    var yearStr = tbl.Rows[i][21].ToString().Trim();
                    if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt)) continue;

                    var targetStr = tbl.Rows[i][12].ToString().Trim();
                    if (string.IsNullOrEmpty(targetStr) || !decimal.TryParse(targetStr, out var targetDec)) continue;

                    var revenue = _unitOfWork.RevenueUser_MonthRepository
                        .GetQuery(a => a.UserId == user.Id && a.Month == monthInt && a.Year == yearInt)
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
                            Month = monthInt,
                            Year = yearInt,
                            Target = targetDec,
                            Active = true
                        };
                        newRevenueList.Add(newRevenue);
                    }
                }

                if (newRevenueList.Any())
                {
                    _unitOfWork.RevenueUser_MonthRepository.InsertRange(newRevenueList);
                }

                _unitOfWork.Save();
            }

            return RedirectToAction("Index", "Vcms");
        }

        //public ActionResult RevenueUserWeek_Real()
        //{
        //    return View();
        //}
        [HttpPost]
        public ActionResult RevenueUserWeek_Real(FormCollection fc)
        {
            var file = Request.Files["WeekRealFile"];
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
                var listRevenue = new List<RevenueUser_Week_Real>();
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var manhanvien = tbl.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == manhanvien).FirstOrDefault();
                    if (user == null) continue;
                    var month = tbl.Rows[i][6].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    var monthInt = int.Parse(month);
                    var year = tbl.Rows[i][7].ToString().Trim();
                    if (string.IsNullOrEmpty(year)) continue;
                    var yearInt = int.Parse(year);
                    var week = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(week)) continue;
                    var weekInt = int.Parse(week);
                    var ds = tbl.Rows[i][8].ToString().Trim();
                    decimal dsDec = string.IsNullOrEmpty(ds) ? 0 : decimal.Parse(ds);
                    var revenue = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.UserId == user.Id && a.Month == monthInt && a.Year == yearInt && (int)a.WeekNumber == weekInt).FirstOrDefault();
                    if (revenue != null)
                    {
                        revenue.TargetBM = dsDec;
                    }
                    else
                    {
                        var newRevenue = new RevenueUser_Week_Real
                        {
                            UserId = user.Id,
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
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
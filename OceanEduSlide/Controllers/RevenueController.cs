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
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Z.EntityFramework.Plus;

namespace OceanEduSlide.Controllers
{
    [Authorize]
    [ForcePasswordChangeFilter]
    public class RevenueController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

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
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officeshortname = tbl.Rows[i][0].ToString().Trim();

                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeshortname).FirstOrDefault();
                    if (office == null) continue;
                    var countNVHV = _unitOfWork.UserRepository.GetQuery(a => (a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL) && a.OfficeId == office.Id).Count();
                    var countNVKT = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.SAB && a.OfficeId == office.Id).Count();
                    var month = tbl.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    var monthInt = int.Parse(month);
                    var year = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(year)) continue;
                    var yearInt = int.Parse(year);
                    var targetTS = tbl.Rows[i][10].ToString().Trim();
                    if (string.IsNullOrEmpty(targetTS)) continue;
                    var targetTSDec = decimal.Parse(targetTS);
                    var targetKT = tbl.Rows[i][12].ToString().Trim();
                    if (string.IsNullOrEmpty(targetKT)) continue;
                    var targetKTDec = decimal.Parse(targetKT);
                    var targetHV = tbl.Rows[i][13].ToString().Trim();
                    if (string.IsNullOrEmpty(targetHV)) continue;
                    var targetHVDec = decimal.Parse(targetHV);
                    var revenue = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt).FirstOrDefault();
                    if (revenue != null)
                    {
                        revenue.Target_TS = targetTSDec;
                        revenue.Target_HV = targetHVDec * countNVHV;
                        revenue.Target_SAB = targetKTDec * countNVKT;
                    }
                    else
                    {
                        var newRevenue = new RevenueOffice
                        {
                            OfficeId = office.Id,
                            Month = monthInt,
                            Year = yearInt,
                            Target_TS = targetTSDec,
                            Target_HV = targetHVDec * countNVHV,
                            Target_SAB = targetKTDec * countNVKT,
                            Active = true,
                        };
                        _unitOfWork.RevenueOfficeRepository.Insert(newRevenue);
                    }

                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index", "Vcms");
        }

        public ActionResult TargetUser()
        {
            return View();
        }
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
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var manhanvien = tbl.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == manhanvien).FirstOrDefault();
                    if (user == null) continue;
                    var month = tbl.Rows[i][20].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    var monthInt = int.Parse(month);
                    var year = tbl.Rows[i][21].ToString().Trim();
                    if (string.IsNullOrEmpty(year)) continue;
                    var yearInt = int.Parse(year);
                    var target = tbl.Rows[i][12].ToString().Trim();
                    if (string.IsNullOrEmpty(target)) continue;
                    var targetDec = decimal.Parse(target);
                    var revenue = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.UserId == user.Id && a.Month == monthInt && a.Year == yearInt).FirstOrDefault();
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
                            Active = true,
                        };
                        _unitOfWork.RevenueUser_MonthRepository.Insert(newRevenue);
                    }

                    _unitOfWork.Save();
                }
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

        public ActionResult RevenueUserWeek_Real()
        {
            return View();
        }
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
                        _unitOfWork.RevenueUser_Week_RealRepository.Insert(newRevenue);
                    }

                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("Index", "Vcms");
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
using Antlr.Runtime.Misc;
using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
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
    public class ReportController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();

        public ActionResult Report(int type)
        {
            ViewBag.Type = type;
            return View();
        }
        [HttpPost]
        public ActionResult Report(int type, FormCollection fc)
        {
            var file = Request.Files["ReportFile"];
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
                //var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Sort));
                string lastCategoryParent = "";

                if (type == 1)
                    for (int i = 2; i < tbl.Rows.Count; i++)
                    {
                        var month = tbl.Rows[i][0].ToString().Trim();
                        if (string.IsNullOrEmpty(month)) continue;
                        var monthInt = int.Parse(month);
                        var officeShortName = tbl.Rows[i][3].ToString().Trim();
                        if (string.IsNullOrEmpty(officeShortName)) continue;
                        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeShortName).FirstOrDefault();
                        if (office == null) continue;
                        for (int j = 4; j < tbl.Columns.Count; j++)
                        {
                            var value = tbl.Rows[i][j].ToString().Trim();
                            //if (string.IsNullOrEmpty(value)) 
                            var categoryChild = tbl.Rows[1][j].ToString().Trim();      // danh mục con: "Thực tế"
                            if (string.IsNullOrEmpty(categoryChild)) continue;
                            var rawCategoryParentCategory = tbl.Rows[0][j].ToString().Trim();
                            if (!string.IsNullOrEmpty(rawCategoryParentCategory))
                            {
                                lastCategoryParent = rawCategoryParentCategory; // danh mục cha: "Định biên sale"
                            }
                            if (string.IsNullOrEmpty(lastCategoryParent)) continue;
                            var category = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Name.Trim() == categoryChild && a.CategoryParent.Name.Trim() == lastCategoryParent && a.TypeCat == TypeCat.Type1).FirstOrDefault();
                            if (category == null)
                                continue;
                            var oldData = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Year == DateTime.Now.Year && a.Month == monthInt && a.ReportCategoryId == category.Id).FirstOrDefault();
                            if (oldData == null)
                            {
                                var data = new ReportData
                                {
                                    Month = monthInt,
                                    Year = DateTime.Now.Year,
                                    OfficeId = office.Id,
                                    ReportCategoryId = category.Id,
                                    Data = value,
                                    Sort = j
                                };
                                _unitOfWork.ReportDataRepository.Insert(data);
                            }
                            else
                            {
                                oldData.Data = value;
                            }
                        }
                    }

                else if (type == 2)
                    for (int i = 2; i < tbl.Rows.Count; i++)
                    {
                        var month = tbl.Rows[i][0].ToString().Trim();
                        if (string.IsNullOrEmpty(month)) continue;
                        var monthInt = int.Parse(month);
                        var officeShortName = tbl.Rows[i][2].ToString().Trim();
                        if (string.IsNullOrEmpty(officeShortName)) continue;
                        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officeShortName).FirstOrDefault();
                        if (office == null) continue;
                        var maNhanVien = tbl.Rows[i][3].ToString().Trim();
                        var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == maNhanVien).FirstOrDefault();
                        if (user == null) continue;
                        var ngayVao = tbl.Rows[i][4].ToString().Trim();
                        var fullName = tbl.Rows[i][5].ToString().Trim();
                        var typeUser = tbl.Rows[i][6].ToString().Trim();
                        var note = tbl.Rows[i][7].ToString().Trim();
                        var dayOff = tbl.Rows[i][8].ToString().Trim();
                        var daysWork = tbl.Rows[i][9].ToString().Trim();

                        for (int j = 10; j < tbl.Columns.Count; j++)
                        {
                            var value = tbl.Rows[i][j].ToString().Trim();
                            //if (string.IsNullOrEmpty(value)) 
                            var categoryChild = tbl.Rows[1][j].ToString().Trim();      // danh mục con: "Thực tế"
                            if (string.IsNullOrEmpty(categoryChild)) continue;
                            var rawCategoryParentCategory = tbl.Rows[0][j].ToString().Trim();
                            if (!string.IsNullOrEmpty(rawCategoryParentCategory))
                            {
                                lastCategoryParent = rawCategoryParentCategory; // danh mục cha: "Định biên sale"
                            }
                            if (string.IsNullOrEmpty(lastCategoryParent)) continue;
                            var category = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Name.Trim() == categoryChild && a.CategoryParent.Name.Trim() == lastCategoryParent && a.TypeCat == TypeCat.Type2).FirstOrDefault();
                            if (category == null)
                                continue;
                            var oldData = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Year == DateTime.Now.Year && a.Month == monthInt && a.ReportCategoryId == category.Id).FirstOrDefault();
                            if (oldData == null)
                            {
                                var data = new ReportData
                                {
                                    Month = monthInt,
                                    Year = DateTime.Now.Year,
                                    OfficeId = office.Id,
                                    UserId = user.Id,
                                    ReportCategoryId = category.Id,
                                    Data = value,
                                    Sort = j
                                };
                                _unitOfWork.ReportDataRepository.Insert(data);
                            }
                            else
                            {
                                oldData.Data = value;
                            }
                        }
                    }
                _unitOfWork.Save();
            }
            return RedirectToAction("Report", new { type });
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
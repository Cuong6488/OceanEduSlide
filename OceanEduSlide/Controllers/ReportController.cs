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
    public class ReportController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Fullname => RouteData.Values["Fullname"].ToString();

        public ActionResult Report(string result="")
        {
            return View();
        }
        [HttpPost]

        public ActionResult Report(FormCollection fc)
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
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type1,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Sort));
                string lastCategoryParent = "";
                //string previousCategoryParent = "";

                var reportDataList = new List<ReportData>();

                // Cache dữ liệu để tránh query lặp lại
                var allOffices = _unitOfWork.OfficeRepository.GetQuery().ToList();
                var allCategories = _unitOfWork.ReportCategoryRepository
                    .GetQuery(a => a.TypeCat == TypeCat.Type1)
                    .Include(a => a.CategoryParent)
                    .ToList();

                for (int i = 2; i < tbl.Rows.Count; i++)
                {
                    var month = tbl.Rows[i][0].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    if (!int.TryParse(month, out int monthInt)) continue;

                    var officeShortName = tbl.Rows[i][3].ToString().Trim();
                    if (string.IsNullOrEmpty(officeShortName)) continue;

                    var office = allOffices.FirstOrDefault(a => a.ShortName == officeShortName);
                    if (office == null) continue;

                    int cChildSort = 1;
                    int group = 1;

                    for (int j = 4; j < tbl.Columns.Count; j++)
                    {
                        var value = tbl.Rows[i][j].ToString().Trim();
                        string valueReal = "";

                        var categoryChild = tbl.Rows[1][j].ToString().Trim();
                        if (string.IsNullOrEmpty(categoryChild)) continue;

                        var rawCategoryParentCategory = tbl.Rows[0][j].ToString().Trim();
                        if (!string.IsNullOrEmpty(rawCategoryParentCategory))
                        {
                            if (rawCategoryParentCategory != lastCategoryParent)
                            {
                                if (j != 4)
                                {
                                    cChildSort = 1;
                                    group++;
                                }

                                lastCategoryParent = rawCategoryParentCategory;
                            }
                        }

                        if (string.IsNullOrEmpty(lastCategoryParent)) continue;

                        // Tìm danh mục
                        var category = allCategories
                            .FirstOrDefault(a => a.Sort == cChildSort && a.CategoryParent?.Sort == group && a.TypeCat == TypeCat.Type1);
                        if (category == null) continue;
                        if (category.Id == 26 || category.Id == 27 || category.Id == 28) continue;
                        // Xử lý dữ liệu hiển thị
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (category.Name.Contains("%"))
                            {
                                if (decimal.TryParse(value, out decimal valDec))
                                {
                                    valueReal = Math.Round(valDec * 100, 1).ToString() + "%";
                                }
                            }
                            else
                            {
                                if (decimal.TryParse(value, out decimal valDec))
                                {
                                    valueReal = valDec.ToString("N0");
                                }
                            }
                        }

                        // Check tồn tại trước khi thêm mới
                        var existing = _unitOfWork.ReportDataRepository.GetQuery(a =>
                            a.OfficeId == office.Id &&
                            a.Year == DateTime.Now.Year &&
                            a.Month == monthInt &&
                            a.ReportCategoryId == category.Id).FirstOrDefault();

                        if (existing != null)
                        {
                            existing.Data = valueReal;
                        }
                        else
                        {
                            var data = new ReportData
                            {
                                Month = monthInt,
                                Year = DateTime.Now.Year,
                                OfficeId = office.Id,
                                ReportCategoryId = category.Id,
                                Data = valueReal,
                                Sort = j
                            };

                            reportDataList.Add(data);
                        }

                        cChildSort++;
                    }
                }

                // Bulk insert
                if (reportDataList.Any())
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList);

                _unitOfWork.Save();
                string lastCategoryParent2 = "";
                var tbl2 = result.Tables[1];
                var reportDataList2 = new List<ReportData>();

                // Cache offices and users to avoid repeated DB hits
                //var allOffices = _unitOfWork.OfficeRepository.GetQuery().ToList();
                var allUsers = _unitOfWork.UserRepository.GetQuery().ToList();
                var allCategories2 = _unitOfWork.ReportCategoryRepository
                    .GetQuery(a => a.TypeCat == TypeCat.Type2)
                    .Include(a => a.CategoryParent) // include parent if needed
                    .ToList();

                var historyUsers = _unitOfWork.HistoryUserRepository.GetQuery();
                for (int i = 2; i < tbl2.Rows.Count; i++)
                {
                    var month = tbl2.Rows[i][0].ToString().Trim();
                    if (string.IsNullOrEmpty(month)) continue;
                    var monthInt = int.Parse(month);

                    var officeShortName = tbl2.Rows[i][2].ToString().Trim();
                    if (string.IsNullOrEmpty(officeShortName)) continue;
                    var office = allOffices.FirstOrDefault(a => a.ShortName == officeShortName);
                    if (office == null) continue;

                    var maNhanVien = tbl2.Rows[i][3].ToString().Trim();
                    var user = allUsers.FirstOrDefault(a => a.MaNhanVien == maNhanVien);
                    if (user == null) continue;
                    var typeUser = tbl2.Rows[i][6].ToString().Trim();
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
                    var dayStart = tbl2.Rows[i][4].ToString().Trim().Replace("'", "");
                    if (string.IsNullOrEmpty(dayStart))
                        continue;
                    var startDate = new DateTime();

                    if (DateTime.TryParse(dayStart, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                        startDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                    else
                        continue;
                    var historyUser = historyUsers.FirstOrDefault(a => a.UserId == user.Id && a.OfficeId == office.Id && a.TypeUser == type && a.Month == monthInt && a.Year == DateTime.Now.Year && a.DayStart == startDate);
                    if (historyUser == null) continue;
                    int cChildSort = 1;
                    int group = 1;

                    for (int j = 10; j < tbl2.Columns.Count; j++)
                    {
                        var value = tbl2.Rows[i][j].ToString().Trim();
                        string valueReal = "";
                        var categoryChild = tbl2.Rows[1][j].ToString().Trim();
                        if (string.IsNullOrEmpty(categoryChild)) continue;

                        var rawCategoryParentCategory = tbl2.Rows[0][j].ToString().Trim();
                        if (!string.IsNullOrEmpty(rawCategoryParentCategory))
                        {
                            if (rawCategoryParentCategory != lastCategoryParent2)
                            {
                                if (j != 10)
                                {
                                    cChildSort = 1;
                                    group++;
                                }

                                lastCategoryParent2 = rawCategoryParentCategory;
                            }
                        }

                        if (string.IsNullOrEmpty(lastCategoryParent2)) continue;

                        var category = allCategories2
                            .FirstOrDefault(a => a.Sort == cChildSort && a.CategoryParent?.Sort == group);
                        if (category == null) continue;
                        if (/*category.Id == 87 ||*/ category.Id == 99 || category.Id == 100 || category.Id == 101) continue;
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (category.Name.Contains("%"))
                            {
                                if (decimal.TryParse(value, out decimal valDec))
                                {
                                    valueReal = Math.Round(valDec * 100, 1).ToString() + "%";
                                }
                            }
                            else
                            {
                                if (decimal.TryParse(value, out decimal valDec))
                                {
                                    valueReal = valDec.ToString("N0");
                                }
                            }
                        }
                        // Avoid inserting if already exists
                        var existing = _unitOfWork.ReportDataRepository.GetQuery(a =>
                            a.UserId == user.Id && a.HistoryUserId == historyUser.Id &&
                            a.Year == DateTime.Now.Year &&
                            a.Month == monthInt &&
                            a.ReportCategoryId == category.Id).FirstOrDefault();

                        if (existing != null)
                        {
                            existing.Data = valueReal;
                        }
                        else
                        {
                            var data = new ReportData
                            {
                                Month = monthInt,
                                Year = DateTime.Now.Year,
                                OfficeId = office.Id,
                                UserId = user.Id,
                                HistoryUserId = historyUser.Id,
                                ReportCategoryId = category.Id,
                                Data = valueReal,
                                Sort = j
                            };

                            reportDataList2.Add(data);
                        }

                        cChildSort++;
                    }
                }

                // Bulk insert
                if (reportDataList2.Any())
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList2);

                _unitOfWork.Save();
                return RedirectToAction("Report", new {result="add"});
            }
            return RedirectToAction("Report");
        }

        //public ActionResult ReportCN(string result = "")
        //{
        //    return View();
        //}
        public ActionResult ClearReport()
        {
            var reportCategories = _unitOfWork.ReportCategoryRepository.GetQuery();
            reportCategories.Delete();
            return RedirectToAction("Index", "Vcms");
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
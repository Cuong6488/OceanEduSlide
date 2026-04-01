using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Z.EntityFramework.Plus;
using System.Text;
using OceanEduSlide.OEDongBo;
using OceanEduSlide.Utils;
using OceanEduSlide.Migrations;

namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class RevenueController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Fullname => RouteData.Values["Fullname"].ToString();
        private RoleAdmin Role => (RoleAdmin)Enum.Parse(typeof(RoleAdmin), RouteData.Values["Role"].ToString());
        public ConfigSite Config => (ConfigSite)HttpContext.Application["ConfigSite"];
        private readonly UserTypeService _userTypeService = new UserTypeService();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();


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
        private bool HandleUserByType(TypeUser type, string zones, HistoryUser historyUser, User user, int i, ModelStateDictionary modelState)
        {
            switch (type)
            {
                case TypeUser.ASM:
                    historyUser.ZoneIds = "," + zones + ",";
                    var listZn = zones.Split(',');

                    foreach (var item in listZn)
                    {
                        var zItem = _unitOfWork.ZoneRepository
                            .GetQuery(a => a.ShortCode == item)
                            .FirstOrDefault();

                        if (zItem == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại vùng nào có tên viết tắt là {item}, dòng {i + 1}"
                            );
                            return false;
                        }

                        if (item == listZn.First())
                        {
                            historyUser.ZoneId = zItem.Id;
                            historyUser.Zone = zItem;
                        }
                    }
                    break;

                case TypeUser.BM:
                case TypeUser.EC:
                    //case TypeUser.AEC:
                    historyUser.OfficeIds = ",";

                    foreach (var item in zones.Split(','))
                    {
                        var o = _unitOfWork.OfficeRepository
                            .GetQuery(a => a.ShortCode == item && a.Active)
                            .FirstOrDefault();

                        if (o == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại CN nào có mã CN là {item}, dòng {i + 1}"
                            );
                            return false;
                        }

                        historyUser.OfficeIds += o.Id + ",";
                    }

                    historyUser.OfficeIds =
                        historyUser.OfficeIds == "," ? null : historyUser.OfficeIds;
                    break;
                case TypeUser.CV:
                    user.ZoneIds = "," + zones + ",";
                    var listZ = zones.Split(',');

                    foreach (var item in listZ)
                    {
                        var zItem = _unitOfWork.ZoneRepository
                            .GetQuery(a => a.ShortCode == item)
                            .FirstOrDefault();

                        if (zItem == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại vùng nào có mã vùng là {item}, dòng {i + 1}"
                            );
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }

        private bool HandleUserByType(TypeUser type, string zones, User user, int i, ModelStateDictionary modelState)
        {
            switch (type)
            {
                case TypeUser.ASM:
                    user.ZoneIds = "," + zones + ",";
                    var listZ = zones.Split(',');

                    foreach (var item in listZ)
                    {
                        var zItem = _unitOfWork.ZoneRepository
                            .GetQuery(a => a.ShortCode == item)
                            .FirstOrDefault();

                        if (zItem == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại vùng nào có tên viết tắt là {item}, dòng {i + 1}"
                            );
                            return false;
                        }

                        if (item == listZ.First())
                        {
                            user.ZoneId = zItem.Id;
                            user.Zone = zItem;
                        }
                    }
                    break;

                case TypeUser.BM:
                case TypeUser.EC:
                    //case TypeUser.AEC:
                    user.OfficeIds = ",";
                    user.OfficeNames = "";

                    foreach (var item in zones.Split(','))
                    {
                        var o = _unitOfWork.OfficeRepository
                            .GetQuery(a => a.ShortCode == item && a.Active)
                            .FirstOrDefault();

                        if (o == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại CN nào có mã CN là {item}, dòng {i + 1}"
                            );
                            return false;
                        }

                        user.OfficeIds += o.Id + ",";
                        user.OfficeNames += o.ShortCode + ",";
                    }

                    user.OfficeNames = user.OfficeNames.Trim(',');
                    user.OfficeIds =
                        user.OfficeIds == "," ? null : user.OfficeIds;
                    break;

                case TypeUser.CV:
                    user.ZoneIds = "," + zones + ",";
                    var listZn = zones.Split(',');

                    foreach (var item in listZn)
                    {
                        var zItem = _unitOfWork.ZoneRepository
                            .GetQuery(a => a.ShortCode == item)
                            .FirstOrDefault();

                        if (zItem == null)
                        {
                            modelState.AddModelError(
                                "",
                                $"Không tồn tại vùng nào có tên viết tắt là {item}, dòng {i + 1}"
                            );
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }

        public ActionResult TargetOffice(string result = "")
        {
            ViewBag.Result = result;
            return View();
        }
        [HttpPost]
        public ActionResult TargetOffice(FormCollection fc, int TypeUpdate)
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
                var typeImport = TypeImport.Type2;
                switch (TypeUpdate)
                {
                    case 2:
                        typeImport = TypeImport.Type11;
                        break;
                    case 3:
                        typeImport = TypeImport.Type12;
                        break;
                    case 4:
                        typeImport = TypeImport.Type13;
                        break;
                    default:
                        break;
                }
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = typeImport,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);

                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var offices = _unitOfWork.OfficeRepository.Get(a => a.Active);
                var listZone = _unitOfWork.ZoneRepository.Get(a => a.Active);
                foreach (var item in listZone)
                {
                    if (item.ShortName == null)
                    {
                        item.ShortName = "";
                    }
                    if (item.OfficeIds == null)
                    {
                        item.OfficeIds = "";
                    }
                }
                var monthStr = tbl.Rows[1][4].ToString().Trim();
                if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt))
                {
                    ModelState.AddModelError("", @"Kiểm tra lại cột tháng");
                    return View();
                }

                var yearStr = tbl.Rows[1][5].ToString().Trim();
                if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt))
                {
                    ModelState.AddModelError("", @"Kiểm tra lại cột năm");
                    return View();
                }

                var (lastMonth, yearLastMonth) = UserService.CalcLastMonth(monthInt, yearInt);
                var workingDay = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == yearInt).FirstOrDefault();
                if (workingDay == null)
                {
                    ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + yearInt);
                    return View();
                }

                var workingDayFull = UserService.CalcWorkingDayFull(monthInt, yearInt, workingDay);

                var isThisMonth = true;
                if (DateTime.Now.Year != yearInt || DateTime.Now.Month != monthInt)
                    isThisMonth = false;

                DateTime endDayLastMonth = new DateTime(yearLastMonth, lastMonth, DateTime.DaysInMonth(yearLastMonth, lastMonth));
                var workingDayLastYear = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == yearInt - 1).FirstOrDefault();
                if (workingDayLastYear == null)
                {
                    ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + (yearInt - 1));
                    return View();
                }
                var targetGroup = _unitOfWork.TargetGroupRepository.GetQuery(a => a.Active && a.Year == yearInt && a.Month == monthInt).FirstOrDefault();
                if (targetGroup == null)
                {
                    ModelState.AddModelError("", @"Chưa có dữ liệu bảng chỉ tiêu NVĐT theo tháng - tháng" + monthInt + "/" + yearInt);
                    return View();
                }

                // CN theo tháng
                if (TypeUpdate == 3 || TypeUpdate == 4)
                {
                    //foreach (var zone in listZone)
                    //{
                    //    zone.OfficeIds = "";
                    //    zone.ShortName = "";
                    //}
                    var reportDataList = new List<ReportData>();
                    var historyOfficeList = new List<HistoryOffice>();
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var shortname = tbl.Rows[i][0].ToString().Trim();
                        shortname = VietnameseCodeHelper.NormalizeVietnameseCode(shortname);
                        if (string.IsNullOrEmpty(shortname))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Chi nhánh - Dòng " + (i + 1));
                            return View();
                        }
                        var office = offices.FirstOrDefault(a => a.ShortName == shortname);
                        if (office == null)
                        {
                            ModelState.AddModelError("", @"Không tồn tại chi nhánh nào có tên ngắn là " + shortname);
                            return View();
                        }
                        var zonename = tbl.Rows[i][1].ToString().Trim();
                        zonename = VietnameseCodeHelper.NormalizeVietnameseCode(zonename);
                        if (string.IsNullOrEmpty(zonename))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Vùng - Dòng " + (i + 1));
                            return View();
                        }
                        var zone = listZone.FirstOrDefault(a => a.Name == zonename || a.ShortCode == zonename);
                        if (zone == null)
                        {
                            ModelState.AddModelError("", @"Không tồn tại vùng nào có tên là " + zonename);
                            return View();
                        }
                        if (monthInt == DateTime.Now.Month && yearInt == DateTime.Now.Year)
                        {
                            office.ZoneId = zone.Id;
                            if (!string.IsNullOrEmpty(zone.OfficeIds) && zone.OfficeIds[0] == ',')
                            {
                                zone.OfficeIds = "";
                                zone.ShortName = "";
                            }
                            if (!zone.OfficeIds.Contains("," + office.Id + ","))
                            {
                                zone.OfficeIds += office.Id + ",";
                                zone.ShortName += office.ShortCode + ",";
                            }

                            //Sync lại vùng đề xuất

                            var dexuats = _unitOfWork.ProposalRepository.GetQuery(a => a.CreateDate.Month == monthInt && a.CreateDate.Year == yearInt && a.OfficeId == office.Id);
                            foreach (var item in dexuats)
                            {
                                item.ZoneId = zone.Id;
                            }
                        }

                        var dbECStr = tbl.Rows[i][7].ToString().Trim();
                        if (string.IsNullOrEmpty(dbECStr) || !int.TryParse(dbECStr, out var dbECInt))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không có dữ liệu cột định biên EC, hoặc không thể chuyển thành dạng số");
                            return View();
                        }
                        var dbATLStr = tbl.Rows[i][6].ToString().Trim();
                        if (string.IsNullOrEmpty(dbATLStr) || !int.TryParse(dbATLStr, out var dbATLInt))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không có dữ liệu cột định biên ATL, hoặc không thể chuyển thành dạng số");
                            return View();
                        }
                        var baseTargetStr = tbl.Rows[i][10].ToString().Trim();
                        if (string.IsNullOrEmpty(baseTargetStr) || !decimal.TryParse(baseTargetStr, out var baseTargetStrDec))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không có dữ liệu cột Chỉ tiêu Doanh số cơ sở, hoặc không thể chuyển thành dạng số");
                            return View();
                        }
                        var dayOpen = tbl.Rows[i][3].ToString().Trim().Replace("'", "");
                        if (string.IsNullOrEmpty(dayOpen))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không có dữ liệu cột ngày khai trương");
                            return View();
                        }
                        var openDate = new DateTime();

                        if (!DateTime.TryParse(dayOpen, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không thể chuyển thành dạng ngày tháng cột ngày khai trương");
                            return View();
                        }
                        else
                        {
                            openDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                            office.OpenDate = openDate;
                        }
                        var group = tbl.Rows[i][15].ToString().Trim();
                        if (string.IsNullOrEmpty(group))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " không có dữ liệu cột phân nhóm đào tạo");
                            return View();
                        }
                        GroupOffice groupOffice = new GroupOffice();
                        switch (group)
                        {
                            case "A":
                                groupOffice = GroupOffice.A;
                                break;
                            case "B":
                                groupOffice = GroupOffice.B;
                                break;
                            case "C":
                                groupOffice = GroupOffice.C;
                                break;
                            case "D":
                                groupOffice = GroupOffice.D;
                                break;
                            case "E":
                                groupOffice = GroupOffice.E;
                                break;
                            default:
                                ModelState.AddModelError("", @"Chi nhánh " + shortname + " sai dữ liệu cột phân nhóm đào tạo");
                                return View();
                        }
                        var qd156 = tbl.Rows[i][2].ToString().Trim();
                        var dbSale = dbECInt + dbATLInt;
                        if (dbSale <= 0)
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " có định biên sale <= 0");
                            return View();
                        }

                        var moneyDownStr = tbl.Rows[i][24].ToString().Trim();
                        decimal moneyDown = 0;
                        if (!string.IsNullOrEmpty(moneyDownStr) && !decimal.TryParse(moneyDownStr, out moneyDown))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " sai định dạng cột Số tiền giảm chỉ tiêu");
                            return View();
                        }
                        var nvkdDownStr = tbl.Rows[i][25].ToString().Trim();
                        int nvkdDown = 0;
                        if (!string.IsNullOrEmpty(nvkdDownStr) && !int.TryParse(nvkdDownStr, out nvkdDown))
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + shortname + " sai định dạng cột Số NVKD cắt giảm chỉ tiêu");
                            return View();
                        }

                        if (tbl.Columns.Count <= 26)
                        {
                            ModelState.AddModelError("", @"Sheet chỉ tiêu CN thiếu cột Các chi nhánh gộp (AA)");
                            return View();
                        }
                        var officeCodes = tbl.Rows[i][26].ToString().Trim();
                        officeCodes = VietnameseCodeHelper.NormalizeVietnameseCode(officeCodes);
                        if (!string.IsNullOrEmpty(officeCodes))
                        {
                            var listCode = officeCodes.Split(',');
                            foreach (var code in listCode)
                            {
                                var off = offices.FirstOrDefault(a => a.ShortCode == code);
                                if (off == null)
                                {
                                    ModelState.AddModelError("", "Không có chi nhánh nào có Mã chi nhánh là \"" + code + "\" - Dòng " + i + 1 + ", Sheet 1");
                                    return View();
                                }
                            }
                        }
                        var bcCTDBCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 22).FirstOrDefault();
                        if (bcCTDBCN == null)
                            bcCTDBCN = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 22);
                        if (bcCTDBCN == null)
                        {
                            bcCTDBCN = new ReportData()
                            {
                                Data = dbSale.ToString("N0"),
                                DataReal = dbSale,
                                Month = monthInt,
                                Year = yearInt,
                                ReportCategoryId = 22,
                                OfficeId = office.Id,
                                Sort = 4,
                            };
                            reportDataList.Add(bcCTDBCN);
                        }
                        else
                        {
                            bcCTDBCN.Data = dbSale.ToString("N0");
                            bcCTDBCN.DataReal = dbSale;
                        }
                        // Báo cáo %ht ĐB Sale

                        // thực đạt ĐB Sale
                        var bcTDDBCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 23).FirstOrDefault();
                        if (bcTDDBCN?.DataReal != null)
                        {
                            var htDBCN = bcTDDBCN.DataReal / dbSale * 100;
                            var datahtDBCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 24 && a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id).FirstOrDefault();
                            if (datahtDBCN == null)
                                datahtDBCN = reportDataList.FirstOrDefault(a => a.ReportCategoryId == 24 && a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id);
                            if (datahtDBCN == null)
                            {
                                datahtDBCN = new ReportData()
                                {
                                    Data = (htDBCN ?? 0).ToString("F2") + "%",
                                    DataReal = htDBCN / 100,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 24,
                                    OfficeId = office.Id,
                                    Sort = 6,
                                };
                                reportDataList.Add(datahtDBCN);
                            }
                            else
                            {
                                datahtDBCN.Data = (htDBCN ?? 0).ToString("F2") + "%";
                                datahtDBCN.DataReal = htDBCN / 100;
                            }
                        }

                        var historyOffice = _unitOfWork.HistoryOfficeRepository.GetQuery(a => a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id).FirstOrDefault();

                        if (historyOffice != null)
                        {
                            historyOffice.GroupOffice = groupOffice;
                            historyOffice.ZoneId = zone.Id;
                            historyOffice.DBATL = dbATLInt;
                            historyOffice.DBEC = dbECInt;
                            historyOffice.QD156 = string.IsNullOrEmpty(qd156) ? false : true;
                            historyOffice.NVKDOver = nvkdDown;
                            historyOffice.TargetReduce = moneyDown;
                            historyOffice.BaseTarget = baseTargetStrDec;
                            if (!string.IsNullOrEmpty(officeCodes))
                                historyOffice.OfficeCodes = officeCodes;
                        }
                        else
                        {
                            var newhistoryOffice = new HistoryOffice
                            {
                                Month = monthInt,
                                Year = yearInt,
                                OfficeId = office.Id,
                                GroupOffice = groupOffice,
                                ZoneId = zone.Id,
                                DBATL = dbATLInt,
                                DBEC = dbECInt,
                                NVKDOver = nvkdDown,
                                TargetReduce = moneyDown,
                                BaseTarget = baseTargetStrDec,
                                QD156 = string.IsNullOrEmpty(qd156) ? false : true
                            };
                            if (!string.IsNullOrEmpty(officeCodes))
                                newhistoryOffice.OfficeCodes = officeCodes;
                            historyOfficeList.Add(newhistoryOffice);
                        }

                    }

                    foreach (var zone in listZone)
                    {
                        if (!string.IsNullOrEmpty(zone.ShortName))
                        {
                            zone.ShortName = zone.ShortName.Trim(',');
                        }
                        if (!string.IsNullOrEmpty(zone.OfficeIds) && zone.OfficeIds[0] != ',')
                            zone.OfficeIds = "," + zone.OfficeIds;
                    }
                    if (historyOfficeList.Any())
                        _unitOfWork.HistoryOfficeRepository.InsertRange(historyOfficeList);
                    if (reportDataList.Any())
                        _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
                    _unitOfWork.Save();
                }

                // Nhân sự tháng
                if (TypeUpdate == 2 || TypeUpdate == 4)
                {
                    var reportDataList = new List<ReportData>();
                    var tbl2 = result.Tables[1];
                    var historyUserList = new List<HistoryUser>();
                    var listHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt);
                    var users = _unitOfWork.UserRepository.GetQuery().ToList();

                    var lockImport = _unitOfWork.LockImportRepository.GetQuery(a => a.Year == yearInt && a.Month == monthInt && a.Active && a.TypeLock == TypeLock.HistoryUser).FirstOrDefault();
                    if (lockImport != null && Role != RoleAdmin.Admin)
                    {
                        ModelState.AddModelError("", @"Số liệu tháng trước đã được khóa, chỉ quyền quản trị viên mới có thể cập nhật");
                        return View();
                    }

                    for (var i = 1; i < tbl2.Rows.Count; i++)
                    {
                        var manhanvien = tbl2.Rows[i][2].ToString().Trim().Replace("'", "");
                        if (string.IsNullOrEmpty(manhanvien))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Mã nhân viên dòng " + (i + 1));
                            return View();
                        }
                        var user = users.FirstOrDefault(a => a.MaNhanVien == manhanvien);

                        var officeShortName = tbl2.Rows[i][1].ToString().Trim();
                        officeShortName = VietnameseCodeHelper.NormalizeVietnameseCode(officeShortName);
                        var office = offices.FirstOrDefault(a => a.ShortName == officeShortName);

                        var zoneName = tbl2.Rows[i][0].ToString().Trim();
                        zoneName = VietnameseCodeHelper.NormalizeVietnameseCode(zoneName);
                        var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Name == zoneName).FirstOrDefault();

                        var cdcm = tbl2.Rows[i][4].ToString().Trim();
                        cdcm = VietnameseCodeHelper.NormalizeVietnameseCode(cdcm);

                        //var typeUser = tbl2.Rows[i][10].ToString().Trim();
                        //typeUser = VietnameseCodeHelper.NormalizeVietnameseCode(typeUser);
                        if (string.IsNullOrEmpty(cdcm))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột chức danh dòng " + (i + 1));
                            return View();
                        }
                        var type = _userTypeService.GetTypeUser(cdcm);
                        if (type == null)
                        {
                            ModelState.AddModelError("", @"Chưa tồn tại chức danh " + type + ", dòng " + (i + 1));
                            return View();
                        }
                        var status = tbl2.Rows[i][5].ToString().Trim();
                        if (string.IsNullOrEmpty(status))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Trạng thái nhân viên chốt dòng " + (i + 1));
                            return View();
                        }

                        var statusUser = UserService.GetStatusUser(status);

                        if (statusUser == null)
                        {
                            ModelState.AddModelError("", @"Chưa tồn tại Trạng thái " + status + ", dòng " + (i + 1));
                            return View();
                        }

                        var password = HtmlHelpers.ComputeHash(Config.Password ?? "AUG2025@#", "SHA256", null);
                        var fullname = tbl2.Rows[i][3].ToString().Trim();
                        fullname = VietnameseCodeHelper.NormalizeVietnameseCode(fullname);

                        var zones = tbl2.Rows[i][12].ToString().Trim();
                        zones = VietnameseCodeHelper.NormalizeVietnameseCode(zones);

                        var dayReduceStr = tbl2.Rows[i][13].ToString().Trim();
                        int dayReduce = 0;
                        if (!string.IsNullOrEmpty(dayReduceStr) && !int.TryParse(dayReduceStr, out dayReduce))
                        {
                            ModelState.AddModelError("", @"Sai định dạng cột Số ngày công giảm, dòng " + (i + 1));
                            return View();
                        }
                        var dayReduceCGStr = tbl2.Rows[i][14].ToString().Trim();
                        int dayReduceCG = 0;
                        if (!string.IsNullOrEmpty(dayReduceCGStr) && !int.TryParse(dayReduceCGStr, out dayReduceCG))
                        {
                            ModelState.AddModelError("", @"Sai định dạng cột Số ngày giảm cuộc gọi, dòng " + (i + 1));
                            return View();
                        }
                        var sort = tbl2.Rows[i][11].ToString().Trim();
                        int sortValue = 0;
                        if (!string.IsNullOrEmpty(sort))
                            if (!int.TryParse(sort, out sortValue))
                            {
                                ModelState.AddModelError("", @"Lỗi định dạng cột Thứ tự, dòng " + (i + 1));
                                return View();
                            }
                        user = UserService.UpdateUserInfo(user, manhanvien, fullname, cdcm, password, office, zone, type.Value, users, statusUser.Value, isThisMonth, _unitOfWork, false);

                        if (!string.IsNullOrEmpty(zones))
                        {
                            if (!HandleUserByType(type.Value, zones, user, i, ModelState))
                            {
                                return View();
                            }
                        }

                        var dayStart = tbl2.Rows[i][6].ToString().Trim().Replace("'", "");
                        if (string.IsNullOrEmpty(dayStart))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Ngày vào làm, dòng " + (i + 1));
                            return View();
                        }
                        var dayEnd = tbl2.Rows[i][7].ToString().Trim().Replace("'", "");
                        var startDate = new DateTime();
                        var endDate = new DateTime();

                        if (DateTime.TryParse(dayStart, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                            startDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                        else
                        {
                            ModelState.AddModelError("", @"Lỗi định dạng cột Ngày vào làm, dòng " + (i + 1));
                            return View();
                        }
                        if (!string.IsNullOrEmpty(dayEnd))
                            if (DateTime.TryParse(dayEnd, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd2))
                                endDate = new DateTime(cd2.Year, cd2.Month, cd2.Day, 0, 0, 0);
                            else
                            {
                                ModelState.AddModelError("", @"Lỗi định dạng cột Ngày nghỉ việc/TS/ĐC, dòng " + (i + 1));
                                return View();
                            }
                        var historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.Status == StatusUser.Active);
                        var historyUserInActive = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.Status == StatusUser.InActive);
                        // Ghi đè ngày vào làm
                        if (historyUser != null && (statusUser == StatusUser.Active || statusUser == StatusUser.InActive))
                        {
                            if (historyUserInActive != null && statusUser == StatusUser.InActive)
                            {
                                historyUser = historyUserInActive;
                            }
                            historyUser.Status = statusUser.Value;
                            historyUser.OfficeId = office?.Id;
                            historyUser.ZoneId = zone?.Id;
                            historyUser.CDCM = cdcm;
                            historyUser.DayReduce = dayReduce;
                            historyUser.DayReduceCG = dayReduceCG;
                            historyUser.TypeUser = type.Value;
                            historyUser.DayStart = startDate;
                            historyUser.Sort = sortValue;
                            if (!string.IsNullOrEmpty(dayEnd))
                                historyUser.DayEnd = endDate;
                            if (!string.IsNullOrEmpty(zones))
                            {
                                if (!HandleUserByType(type.Value, zones, historyUser, user, i, ModelState))
                                {
                                    return View();
                                }
                            }

                        }
                        else
                        {
                            var ngaynghiviec = !string.IsNullOrEmpty(dayEnd) ? endDate : (DateTime?)null;

                            historyUser = UserService.UpdateHistoryUserInfo(user, cdcm, office, zone, type.Value, startDate, ngaynghiviec, sortValue, listHistoryUser, StatusUser.InActive, monthInt, yearInt, historyUserList, false);

                            if (!string.IsNullOrEmpty(zones))
                            {
                                if (!HandleUserByType(type.Value, zones, historyUser, user, i, ModelState))
                                {
                                    return View();
                                }
                            }

                        }
                    }
                    if (historyUserList.Any())
                        _unitOfWork.HistoryUserRepository.InsertRange(historyUserList);
                    _unitOfWork.Save();


                    // Báo cáo cuộc gọi, ĐB Sale
                    var listReportCategoryId = new List<int> { 100, 26, 27, 99, 101 };
                    var reportDatasNew = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && listReportCategoryId.Contains(a.ReportCategoryId));

                    var listNewHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt);

                    // Cuộc gọi: CT,TD,HT Nhân sự; CT, TD Chi nhánh
                    UserService.SyncBaoCaoCuocGoi(Config, workingDayFull.Value, reportDatasNew, reportDataList, listNewHistoryUser, yearInt, monthInt, lastMonth, endDayLastMonth, _unitOfWork);


                    var newListHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt);
                    var listId = new List<int> { 22, 23, 24 };
                    var reportDataDBSales = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && listId.Contains(a.ReportCategoryId));

                    if (reportDataList.Any())
                        _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
                    _unitOfWork.Save();

                }
                // Chỉ tiêu CN - NV
                if (Config.AutoRevenue)
                {
                    // Tải trước các bản ghi vào bộ nhớ
                    var historyOffices = _unitOfWork.HistoryOfficeRepository.Get(a => a.Active && a.Year == yearInt && a.Month == monthInt);
                    var historyUserMonthList = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Year == yearInt && a.Month == monthInt
                        && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL || a.TypeUser == TypeUser.SAB)
                        && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value.Month != monthInt || (a.DayEnd.Value.Day != 1 && a.DayEnd.Value.Month == monthInt))));
                    var revenueOffices = _unitOfWork.RevenueOfficeRepository.Get(a => a.Month == monthInt && a.Year == yearInt);
                    var reportDatas = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 87);
                    var revenueUsers = _unitOfWork.RevenueUser_MonthRepository.Get(a => a.Month == monthInt && a.Year == yearInt);
                    var listRevenueUserDataBases = _unitOfWork.RevenueUser_MonthRepository.Get(a => a.HistoryUserId != null
                           && (a.HistoryUser.TypeUser == TypeUser.EC || a.HistoryUser.TypeUser == TypeUser.ALT) && a.Month == monthInt && a.Year == yearInt
                           && (a.HistoryUser.DayEnd == null || (a.HistoryUser.DayEnd != null && a.HistoryUser.DayEnd.Value.Month != monthInt || (a.HistoryUser.DayEnd.Value.Day != 1 && a.HistoryUser.DayEnd.Value.Month == monthInt))));
                    var reportDataHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 30);
                    var reportDataCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 34);
                    var reportTDHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 31);
                    var datahtHVCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 32);
                    var reportDatactHVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 95);
                    var oldPosittions = _unitOfWork.HistoryUserRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.Status == StatusUser.Transfer, q => q.OrderByDescending(a => a.DayEnd));

                    var NVKDLastMonths = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Year == yearLastMonth && a.Month == lastMonth && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT)
                                    && a.DayStart <= endDayLastMonth && (a.DayEnd == null || (a.DayEnd != null && a.DayEnd.Value > endDayLastMonth)));
                    var reportTDHVNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 96);
                    var datahtHVNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 97);
                    var reportTDDSNVs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 88);
                    var datahtDSNVs = _unitOfWork.ReportDataRepository.Get(a => a.ReportCategoryId == 89 && a.Month == monthInt && a.Year == yearInt);
                    var reportTDDSCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 35);
                    var datahtDSCNs = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 36);
                    var reportCallOffices = _unitOfWork.ReportDataRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt && (a.ReportCategoryId == 26 || a.ReportCategoryId == 27));
                    var reportCallHTOffices = _unitOfWork.ReportDataRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 28);

                    foreach (var item in reportCallOffices)
                    {
                        item.Data = "0";
                        item.DataReal = 0;
                    }

                    var reportDataListAdd = new List<ReportData>();
                    var newRevenueList = new List<RevenueOffice>();
                    var newRevenueList2 = new List<RevenueUser_Month>();
                    foreach (var office in offices)
                    {
                        #region chỉ tiêu doanh số
                        // Chỉ tiêu DS, thực đạt HV, chỉ tiêu phân bổ DS nhân sự

                        var historyOffice = historyOffices.FirstOrDefault(a => a.OfficeId == office.Id);
                        if (historyOffice == null)
                        {
                            continue;
                        }
                        if (historyOffice.DBEC + historyOffice.DBATL <= 0)
                        {
                            ModelState.AddModelError("", "Định biên NVKD chi nhánh " + office.ShortName + " không hợp lệ");
                            return View();
                        }
                        if (historyOffice.BaseTarget == 0)
                        {
                            ModelState.AddModelError("", "Không có chỉ tiêu cơ sở chi nhánh " + office.ShortName);
                            return View();
                        }

                        UserService.SyncChitieuDS(monthInt, yearInt, workingDayFull.Value, lastMonth, yearLastMonth, endDayLastMonth, office, historyOffice, workingDay, targetGroup, workingDayLastYear, newRevenueList,
                             newRevenueList2, reportDataListAdd, historyUserMonthList, revenueOffices, listRevenueUserDataBases, revenueUsers,
                              NVKDLastMonths, reportDatactHVs, reportDatas, reportTDDSNVs, datahtDSNVs, reportDataCNs,
                             reportTDDSCNs, datahtDSCNs, reportDataHVCNs);
                        #endregion

                        #region Cuộc gọi chi nhánh
                        var callTarget = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                        if (callTarget == null)
                            callTarget = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                        var callTD = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                        if (callTD == null)
                            callTD = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                        var callHT = reportCallHTOffices.FirstOrDefault(a => a.OfficeId == office.Id);
                        if (callHT == null)
                            callHT = reportDataListAdd.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 28);
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
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 28,
                                    //check null
                                    OfficeId = office.Id,
                                    Sort = 9,
                                };

                                reportDataListAdd.Add(callHT);
                            }
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
                    _unitOfWork.Save();
                }
                ViewBag.Result = "add";
                return View();
            }

            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult PhieuThuTHDB()
        {
            return View();
        }
        [HttpPost]
        public ActionResult PhieuThuTHDB(FormCollection fc, int Month, int Year)
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
                    officename = VietnameseCodeHelper.NormalizeVietnameseCode(officename);
                    var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortName == officename).FirstOrDefault();
                    if (office == null)
                    {
                        //continue;

                        ModelState.AddModelError("", "Không có chi nhánh nào có tên ngắn là" + officename);

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
                        ThangHocDuKienDecimal = thangHocDuKien,
                        UD_FINAL = uD_FINAL,
                        LoaiCTH = loaiCTH,
                        ChuongTrinhHoc = chuongTrinhHoc,
                        CapDo = capDo,
                        Modun = modun,
                        UD_NhomUDFINAL = uD_NhomUDFINAL,
                        THDB = true,
                        ThangTinhDThu = Month,
                        NamTinhDThu = Year

                    };
                    _unitOfWork.PhieuThuRepository.Insert(phieuThu);
                }
                _unitOfWork.Save();
                var phieuThuSerVice = new PhieuThuService();
                phieuThuSerVice.SyncDthu(Month, ngayThanhToan1.Year);
                ViewBag.Result = "add";
                return View();
            }
            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult ListPhieuThu(int? page, string username, string maHV, string maDonHang, string loai, string officeId, int? month, int? year, int? type, string result = "")
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
            if (!string.IsNullOrEmpty(maDonHang))
            {
                var newkey = maDonHang.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.DonHang == newkey);
                }
            }
            if (!string.IsNullOrEmpty(maHV))
            {
                var newkey = maHV.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.MaHV == newkey);
                }
            }
            if (!string.IsNullOrEmpty(loai))
            {
                if (loai != "Học phí + Phiếu gộp")
                    phieuThus = phieuThus.Where(l => l.Loai == loai);
                else
                    phieuThus = phieuThus.Where(l => l.Loai == "Học phí" || l.Loai == "Phiếu gộp");

            }
            if (username != null)
            {
                var newkey = username.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.MaNVChotSale.Contains(newkey));
                }
            }

            var model = new ListPhieuThuViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "ShortName", "ShortName"),
                PhieuThus = phieuThus.ToPagedList(pageNumber, pageSize),
                officeId = officeId,
                Username = username,
                type = type,
                year = year,
                month = month,
                MaHV = maHV,
                Loai = loai,
                MaDonHang = maDonHang,
            };
            return View(model);
        }
        public ActionResult ListPhieuThuNguon(int? page, string maNVChotSale, string maHV, string phieuthuketoan, string maDonHang, string officeId, string loai, string trangThai, int? month, int? year)
        {
            var pageNumber = page ?? 1;
            const int pageSize = 20;
            var phieuThus = _dongBoTuyenSinh.BC_PhieuThu.OrderByDescending(a => a.NgayThanhToan).AsQueryable();
            if (!string.IsNullOrEmpty(officeId))
            {
                phieuThus = phieuThus.Where(l => l.ChiNhanh == officeId);
            }
            if (month != null)
                phieuThus = phieuThus.Where(l => l.NgayThanhToan.HasValue && l.NgayThanhToan.Value.Month == month);

            if (year != null)
                phieuThus = phieuThus.Where(l => l.NgayThanhToan.HasValue && l.NgayThanhToan.Value.Year == year);

            if (!string.IsNullOrEmpty(maNVChotSale))
            {
                var newkey = maNVChotSale.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.MaNVChotSale.Contains(newkey));
                }
            }
            if (!string.IsNullOrEmpty(maDonHang))
            {
                var newkey = maDonHang.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.DonHang == newkey);
                }
            }
            if (!string.IsNullOrEmpty(maHV))
            {
                var newkey = maHV.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    phieuThus = phieuThus.Where(l => l.MaHV == newkey);
                }
            }
            if (!string.IsNullOrEmpty(loai))
            {
                if (loai != "Học phí + Phiếu gộp")
                    phieuThus = phieuThus.Where(l => l.Loai == loai);
                else
                    phieuThus = phieuThus.Where(l => l.Loai == "Học phí" || l.Loai == "Phiếu gộp");

            }
            if (!string.IsNullOrEmpty(trangThai))
            {
                if (trangThai != "Complete + Confirm")
                    phieuThus = phieuThus.Where(l => l.TrangThai == trangThai);
                else
                    phieuThus = phieuThus.Where(l => l.TrangThai == "StatusPayment_Complete" || l.TrangThai == "StatusPayment_Confirm");
            }
            if (!string.IsNullOrEmpty(phieuthuketoan))
            {
                var newkey = phieuthuketoan.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    if (long.TryParse(newkey, out var longPhieuThuKeToan))
                    {
                        phieuThus = phieuThus.Where(l => l.PhieuThuKeToan == longPhieuThuKeToan);
                    }
                    else
                    {
                        ViewBag.Error = "Phiếu thu kế toán không hợp lệ, loại bỏ kết quả lọc với phiếu thu kế toán: " + newkey + ".";
                    }

                }
            }

            var model = new ListPhieuThuNguonViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "ShortName", "ShortName"),
                PhieuThus = phieuThus.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
                MaNVChotSale = maNVChotSale,
                MaHV = maHV,
                TrangThai = trangThai,
                Loai = loai,
                Year = year,
                Month = month,
                MaDonHang = maDonHang,
                Phieuthuketoan = phieuthuketoan,
                DoanhThu = phieuThus.Where(a => a.SUD.HasValue).Sum(a => a.SUD)
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
                    typeName = "Chỉ tiêu CN - NV";
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
            if (type == 1)
            {
                var logImport = _unitOfWork.LogImportRepository.GetQuery(a => ((int)a.TypeImport == 1 || (int)a.TypeImport == 10 || (int)a.TypeImport == 11 || (int)a.TypeImport == 12) && a.CreateDate.Month == DateTime.Now.Month, q => q.OrderByDescending(a => a.CreateDate));
                return PartialView(logImport);
            }
            else
            {
                var logImport = _unitOfWork.LogImportRepository.GetQuery(a => (int)a.TypeImport == type && a.CreateDate.Month == DateTime.Now.Month, q => q.OrderByDescending(a => a.CreateDate));
                return PartialView(logImport);
            }
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
                    Target_E = Convert.ToDecimal(model.Target_E.Replace(",", "")),

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
                Target_E = target.Target_E.ToString("N0"),
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
                targetGroup.Target_E = Convert.ToDecimal(model.Target_E.Replace(",", ""));
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "ShortName"),
                Revenues = revenueUsers.ToPagedList(pageNumber, pageSize),
                OfficeId = officeId,
            };
            return View(model);
        }
        public ActionResult TestSyncPhieuThu(int date, int month, int year)
        {
            var phieuthuService = new PhieuThuService();
            phieuthuService.TestSyncPhieuThu(date, month, year);
            return Content("Đã đồng bộ phiếu thu thủ công");
        }
        public ActionResult SyncPhieuThu()
        {
            var phieuthuService = new PhieuThuService();
            phieuthuService.SyncPhieuThu();
            return Content("Đã đồng bộ phiếu thu thủ công");
        }
        public ActionResult SyncDebt(/*int? day,int? month, int? year*/)
        {
            //var date = DateTime.Now;
            //if(day.HasValue && month.HasValue && year.HasValue)
            //{
            //    date = new DateTime(year.Value, month.Value, day.Value);
            //}
            var congNoService = new CongNoService();
            congNoService.SyncCongNo(/*date*/);
            return Content("Đã đồng bộ công nợ thủ công");
        }
        public ActionResult SyncUserAsync()
        {
            var userService = new UserService();
            userService.SyncUser(DateTime.Now);
            return Content("Đã đồng bộ nhân sự");
        }
        public ActionResult SyncUserLastMonthAsync()
        {
            var today = DateTime.Now;
            var date = new DateTime(today.Year, today.Month, 1).AddDays(-1);
            var userService = new UserService();
            userService.SyncUser(date);
            return Content("Đã đồng bộ nhân sự tháng trước");
        }

        public ActionResult DeleteDatax2(string listId, int month, int year)
        {

            var listIdString = listId.Split(',');
            var listIdInt = new List<int>();
            foreach (var item in listIdString)
            {
                var id = int.Parse(item);
                listIdInt.Add(id);
            }
            var datas = _unitOfWork.ReportDataRepository.GetQuery(a => listIdInt.Contains(a.ReportCategoryId) && a.Month == month && a.Year == year);
            datas.Delete();
            return Content("ok");
        }
        public ActionResult DeleteDatax22(int month, int year)
        {
            var datas = _unitOfWork.ReportDataRepository.GetQuery(a => a.Year == year && a.Month == month);
            datas.Delete();
            return Content("ok");
        }
        public ActionResult DeleteNSChuaDenNAD(int month, int year)
        {
            var today = DateTime.Now.Date;
            if (month != today.Month || year != today.Year)
            {
                today = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            }
            var listNS = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Year == year && a.Month == month && a.DayStart > today);
            var countNS = 0;
            var countHUS = 0;
            var listMNS = "";
            foreach (var item in listNS)
            {
                item.Active = false;
                countHUS++;
                listMNS += item.User.MaNhanVien + ",";
                var user = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.MaNhanVien == item.User.MaNhanVien).FirstOrDefault();
                if (user != null)
                {
                    user.Active = false;
                    countNS++;
                }
            }
            _unitOfWork.Save();
            return Content("Đã xóa " + countNS + " nhân sự, " + countHUS + " nhân sự tháng. List: " + listMNS);
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
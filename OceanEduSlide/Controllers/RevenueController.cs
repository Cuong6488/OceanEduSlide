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

namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class RevenueController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Fullname => RouteData.Values["Fullname"].ToString();
        private RoleAdmin Role => (RoleAdmin)Enum.Parse(typeof(RoleAdmin), RouteData.Values["Role"].ToString());
        public ConfigSite Config => (ConfigSite)HttpContext.Application["ConfigSite"];


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
                var workingDay = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == yearInt).FirstOrDefault();
                if (workingDay == null)
                {
                    ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + yearInt);
                    return View();
                }
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
                    var reportDataList = new List<ReportData>();
                    var historyOfficeList = new List<HistoryOffice>();
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var shortname = tbl.Rows[i][0].ToString().Trim();
                        if (string.IsNullOrEmpty(shortname))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Chi nhánh - Dòng " + (i + 1));
                            return View();
                        }
                        var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == shortname.Normalize(NormalizationForm.FormC));
                        if (office == null)
                        {
                            ModelState.AddModelError("", @"Không tồn tại chi nhánh nào có tên ngắn là " + shortname);
                            return View();
                        }
                        var zonename = tbl.Rows[i][1].ToString().Trim();
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
                        office.ZoneId = zone.Id;
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
                            historyOfficeList.Add(newhistoryOffice);
                        }

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

                    var reportCallOffices = _unitOfWork.ReportDataRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt && (a.ReportCategoryId == 26 || a.ReportCategoryId == 27));
                    var reportCallHTOffices = _unitOfWork.ReportDataRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 28);

                    foreach (var item in reportCallOffices)
                    {
                        item.Data = "0";
                        item.DataReal = 0;
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
                        var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == officeShortName.Normalize(NormalizationForm.FormC));

                        var zoneName = tbl2.Rows[i][0].ToString().Trim();
                        var zone = _unitOfWork.ZoneRepository.GetQuery(a => a.Name == zoneName).FirstOrDefault();

                        var cdcm = tbl2.Rows[i][4].ToString().Trim();

                        var typeUser = tbl2.Rows[i][10].ToString().Trim();
                        if (string.IsNullOrEmpty(typeUser))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột phân quyền dòng " + (i + 1));
                            return View();
                        }
                        TypeUser type = new TypeUser();
                        switch (typeUser)
                        {
                            case "ASM":
                                type = TypeUser.ASM;
                                break;
                            case "GĐTS":
                                type = TypeUser.HO;
                                break;
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
                            case "Chuyên viên":
                                type = TypeUser.CV;
                                break;
                            case "AEC":
                                type = TypeUser.AEC;
                                break;
                            default:
                                ModelState.AddModelError("", @"Chưa tồn tại phân quyền " + typeUser + ", dòng " + (i + 1));
                                return View();
                                //break;
                        }

                        var status = tbl2.Rows[i][5].ToString().Trim();
                        if (string.IsNullOrEmpty(status))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Trạng thái nhân viên chốt dòng " + (i + 1));
                            return View();
                        }

                        StatusUser statusUser = new StatusUser();
                        switch (status)
                        {
                            case "Đang làm việc":
                                statusUser = StatusUser.Active;
                                break;
                            case "Nghỉ thai sản":
                                statusUser = StatusUser.InActive;
                                break;
                            case "Đã nghỉ":
                                statusUser = StatusUser.InActive;
                                break;
                            case "Nghỉ việc":
                                statusUser = StatusUser.InActive;
                                break;
                            case "Miễn nhiệm":
                                statusUser = StatusUser.InActive;
                                break;
                            case "Điều chuyển":
                                statusUser = StatusUser.Transfer;
                                break;
                            case "Bổ nhiệm":
                                statusUser = StatusUser.Transfer;
                                break;
                            default:
                                ModelState.AddModelError("", @"Chưa tồn tại Trạng thái " + status + ", dòng " + (i + 1));
                                return View();
                        }
                        var password = HtmlHelpers.ComputeHash(Config.Password ?? "AUG2025@#", "SHA256", null);
                        var fullname = tbl2.Rows[i][3].ToString().Trim();
                        var zones = tbl2.Rows[i][12].ToString().Trim();
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
                        if (user == null)
                        {
                            if (statusUser == StatusUser.Active)
                            {
                                var newUser = new User
                                {
                                    Username = manhanvien,
                                    MaNhanVien = manhanvien,
                                    Password = password,
                                    Active = true,
                                    OfficeId = office?.Id,
                                    ZoneId = zone?.Id,
                                    Fullname = fullname,
                                    SaleKit = true,
                                    TypeUser = type,
                                    CDCM = cdcm,
                                    //ZoneIds = type == TypeUser.CV ? "," + zones + "," : null,
                                };
                                switch (type)
                                {
                                    case TypeUser.ASM:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            newUser.ZoneIds = "," + zones + ",";
                                            var listZ = zones.Split(',');
                                            foreach (var item in listZ)
                                            {
                                                var zItem = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                                if (zItem == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại vùng nào có tên viết tắt là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                if (item == listZ[0])
                                                {
                                                    newUser.ZoneId = zItem.Id;
                                                    newUser.Zone = zItem;
                                                }
                                            }
                                        }

                                        break;
                                    case TypeUser.BM:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            newUser.OfficeIds = ",";
                                            newUser.OfficeNames = "";
                                            foreach (var item in zones.Split(','))
                                            {
                                                var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                                if (o == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại CN nào có mã CN là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                newUser.OfficeIds += o.Id + ",";
                                                newUser.OfficeNames += o.ShortCode + ",";

                                            }
                                            newUser.OfficeNames = newUser.OfficeNames.Trim(',');
                                            newUser.OfficeIds = (newUser.OfficeIds == "," ? null : newUser.OfficeIds);
                                        }

                                        break;
                                    case TypeUser.EC:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            newUser.OfficeIds = ",";
                                            newUser.OfficeNames = "";
                                            foreach (var item in zones.Split(','))
                                            {
                                                var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                                if (o == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại CN nào có mã CN là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                newUser.OfficeIds += o.Id + ",";
                                                newUser.OfficeNames += o.ShortCode + ",";

                                            }
                                            newUser.OfficeNames = newUser.OfficeNames.Trim(',');
                                            newUser.OfficeIds = (newUser.OfficeIds == "," ? null : newUser.OfficeIds);
                                        }

                                        break;
                                    case TypeUser.CV:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            user.ZoneIds = "," + zones + ",";
                                            var listZ = zones.Split(',');
                                            foreach (var item in listZ)
                                            {
                                                var zItem = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                                if (zItem == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại vùng nào có mã vùng là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                            }
                                        }
                                        break;
                                    default:
                                        break;
                                }
                                try
                                {
                                    _unitOfWork.UserRepository.Insert(newUser);
                                    _unitOfWork.Save();
                                    users.Add(newUser);
                                    user = newUser;
                                }
                                catch (Exception e)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            user.CDCM = cdcm;
                            user.ZoneId = zone?.Id;
                            if (statusUser == StatusUser.Active)
                            {
                                user.Active = true;
                                user.SaleKit = true;
                                if (user.OfficeId != office?.Id || user.TypeUser != type)
                                {
                                    user.OfficeId = office?.Id;
                                    user.TypeUser = type;
                                    try
                                    {
                                        _unitOfWork.Save();
                                    }
                                    catch (Exception e)
                                    {
                                        continue;
                                    }
                                }
                                switch (type)
                                {
                                    case TypeUser.ASM:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            user.ZoneIds = "," + zones + ",";
                                            var listZ = zones.Split(',');
                                            foreach (var item in listZ)
                                            {
                                                var zItem = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                                if (zItem == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại vùng nào có tên viết tắt là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                if (item == listZ[0])
                                                {
                                                    user.ZoneId = zItem.Id;
                                                    user.Zone = zItem;
                                                }
                                            }
                                        }

                                        break;
                                    case TypeUser.EC:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            user.OfficeIds = ",";
                                            user.OfficeNames = "";
                                            foreach (var item in zones.Split(','))
                                            {
                                                var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                                if (o == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại CN nào có mã CN là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                user.OfficeIds += o.Id + ",";
                                                user.OfficeNames += o.ShortCode + ",";

                                            }
                                            user.OfficeNames = user.OfficeNames.Trim(',');
                                            user.OfficeIds = (user.OfficeIds == "," ? null : user.OfficeIds);
                                        }
                                        break;
                                    case TypeUser.BM:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            user.OfficeIds = ",";
                                            user.OfficeNames = "";
                                            foreach (var item in zones.Split(','))
                                            {
                                                var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                                if (o == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại CN nào có mã CN là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                                user.OfficeIds += o.Id + ",";
                                                user.OfficeNames += o.ShortCode + ",";

                                            }
                                            user.OfficeNames = user.OfficeNames.Trim(',');
                                            user.OfficeIds = (user.OfficeIds == "," ? null : user.OfficeIds);
                                        }

                                        break;
                                    case TypeUser.CV:
                                        if (!string.IsNullOrEmpty(zones))
                                        {
                                            user.ZoneIds = "," + zones + ",";
                                            var listZ = zones.Split(',');
                                            foreach (var item in listZ)
                                            {
                                                var zItem = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                                if (zItem == null)
                                                {
                                                    ModelState.AddModelError("", @"Không tồn tại vùng nào có tên viết tắt là " + item + ", dòng " + (i + 1));
                                                    return View();
                                                }
                                            }
                                        }

                                        break;
                                    default:
                                        break;
                                }
                            }
                            else if (statusUser == StatusUser.InActive)
                            {
                                user.Active = false;
                                try
                                {
                                    _unitOfWork.Save();
                                }
                                catch (Exception e)
                                {
                                    continue;
                                }
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
                        // Ghi đè ngày vào làm
                        if (historyUser != null)
                        {
                            if (statusUser == StatusUser.Active)
                            {
                                historyUser.OfficeId = office?.Id;
                                historyUser.ZoneId = zone?.Id;
                                historyUser.CDCM = cdcm;
                                historyUser.DayReduce = dayReduce;
                                historyUser.DayReduceCG = dayReduceCG;
                                historyUser.TypeUser = type;
                                historyUser.DayStart = startDate;
                                historyUser.Sort = sortValue;
                                if (!string.IsNullOrEmpty(dayEnd))
                                    historyUser.DayEnd = endDate;
                            }
                        }
                        else
                        {
                            historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.DayStart == startDate && a.TypeUser == type && ((office != null && a.OfficeId == office.Id) || (office == null && a.OfficeId == null)));

                            if (historyUser != null)
                            {
                                historyUser.Status = statusUser;
                                historyUser.ZoneId = zone?.Id;
                                historyUser.CDCM = cdcm;
                                historyUser.DayReduce = dayReduce;
                                historyUser.DayReduceCG = dayReduceCG;
                                historyUser.Sort = sortValue;
                                //historyUser.DayStart = startDate;
                                if (!string.IsNullOrEmpty(dayEnd))
                                    historyUser.DayEnd = endDate;
                                
                                //}
                            }
                            else
                            {
                                var newhistoryUser = new HistoryUser
                                {
                                    UserId = user.Id,
                                    Month = monthInt,
                                    Year = yearInt,
                                    TypeUser = type,
                                    OfficeId = office?.Id,
                                    ZoneId = zone?.Id,
                                    Status = statusUser,
                                    DayStart = startDate,
                                    CDCM = cdcm,
                                    DayReduce = dayReduce,
                                    DayReduceCG = dayReduceCG,
                                    Active = true
                                };

                                if (!string.IsNullOrEmpty(dayEnd))
                                    newhistoryUser.DayEnd = endDate;
                                if (!string.IsNullOrEmpty(sort))
                                    newhistoryUser.Sort = int.Parse(sort);
                                historyUserList.Add(newhistoryUser);
                            }
                        }
                        if(historyUser != null)
                        {
                            //Tính chỉ tiêu - TĐ - HT cuộc gọi

                            // cuộc gọi thực đạt
                            var countTD = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.CallDate.Year == yearInt && a.CallDate.Month == monthInt && a.BillSec >= 60).Count();
                            // Thêm hoặc update thực đạt CG cho NV
                            if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC || countTD > 0)
                            {
                                var reportDataCallTD = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 100).FirstOrDefault();
                                if (reportDataCallTD == null)
                                    reportDataCallTD = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 100);
                                if (reportDataCallTD == null)
                                {
                                    reportDataCallTD = new ReportData()
                                    {
                                        Data = countTD.ToString("N0"),
                                        DataReal = countTD,
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 100,
                                        OfficeId = office?.Id,
                                        ZoneId = zone?.Id,
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
                                if (historyUser.TypeUser != TypeUser.EC && historyUser.TypeUser != TypeUser.ALT && historyUser.TypeUser != TypeUser.AEC)
                                {
                                    var targetCallEmpty = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 99).FirstOrDefault();
                                    if (targetCallEmpty == null)
                                        targetCallEmpty = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 99);
                                    if (targetCallEmpty == null)
                                    {
                                        targetCallEmpty = new ReportData()
                                        {
                                            Data = "",
                                            UserId = historyUser.UserId,
                                            HistoryUserId = historyUser.Id,
                                            Month = monthInt,
                                            Year = yearInt,
                                            ReportCategoryId = 99,
                                            OfficeId = office?.Id,
                                            Sort = 18,
                                        };
                                        reportDataList.Add(targetCallEmpty);
                                    }

                                }
                            }
                            if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || historyUser.TypeUser == TypeUser.AEC)
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
                                int workingDayTT = 0;
                                if ((startDateReal.Year < yearInt || (startDateReal.Year == yearInt && startDateReal.Month < monthInt)) && (historyUser.DayEnd == null || (historyUser.DayEnd != null && historyUser.DayEnd.Value.Month > monthInt)))
                                {
                                    workingDayTT = workingDayFull;
                                }
                                else
                                {
                                    DateTime ngayBatDau = historyUser.DayStart.Year < yearInt || (historyUser.DayStart.Year == yearInt && historyUser.DayStart.Month < monthInt) ? new DateTime(yearInt, monthInt, 1) : historyUser.DayStart;
                                    if (oldPosittion != null)
                                    {
                                        var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                                        var oldDayWork = oldDayFull - (oldDayFull / 6);
                                        workingDayTT = Math.Max(workingDayFull - oldDayWork, 0);
                                    }
                                    else
                                    {
                                        DateTime ngayKetThuc = historyUser.DayEnd != null ? historyUser.DayEnd.Value.AddDays(-1) : new DateTime(yearInt, monthInt, DateTime.DaysInMonth(yearInt, monthInt));
                                        int soNgayLamViec = (ngayKetThuc - ngayBatDau).Days + 1;
                                        if (soNgayLamViec < 0)
                                        {
                                            ModelState.AddModelError("", @"Nhân viên " + historyUser.User.MaNhanVien + " có ngày vào làm > ngày nghỉ việc");
                                            return View();
                                        }
                                        int soNgayNghi = soNgayLamViec / 6;
                                        workingDayTT = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                                    }
                                }
                                if (historyUser.DayReduce > 0)
                                {
                                    workingDayTT -= historyUser.DayReduce ?? 0;
                                }
                                if (historyUser.DayReduceCG > 0)
                                {
                                    workingDayTT -= historyUser.DayReduceCG ?? 0;
                                }
                                int callTarget = 0;
                                if (historyUser.DayStart.Month == monthInt || historyUser.DayStart.Month == lastMonth)
                                {
                                    //Số ngày làm việc tháng trước
                                    var dayFree = (endDayLastMonth - historyUser.DayStart).Days + 1;
                                    if (dayFree < 0)
                                        dayFree = 0;
                                    if (dayFree <= 5)
                                    {
                                        workingDayTT = Math.Max(0, workingDayTT - (5 - dayFree));
                                    }
                                }
                                callTarget = 12 * workingDayTT;
                                // Chỉ tiêu báo cáo cuộc gọi nhân sự
                                var reportDataCall = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 99).FirstOrDefault();
                                if (reportDataCall == null)
                                    reportDataCall = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 99);
                                if (reportDataCall == null)
                                {
                                    reportDataCall = new ReportData()
                                    {
                                        Data = callTarget.ToString("N0"),
                                        DataReal = callTarget,
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 99,
                                        //check null
                                        OfficeId = office?.Id,
                                        ZoneId = zone?.Id,
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
                                //var ht = ((double)countTD / callTarget * 100).ToString("F2") + "%";
                                if (callTarget > 0)
                                {
                                    var ht = (decimal)countTD / callTarget;
                                    var reportDataCallHT = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 101).FirstOrDefault();
                                    if (reportDataCallHT == null)
                                        reportDataCallHT = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 101);
                                    if (reportDataCallHT == null)
                                    {
                                        reportDataCallHT = new ReportData()
                                        {
                                            Data = (ht * 100).ToString("F2") + "%",
                                            DataReal = ht,
                                            UserId = historyUser.UserId,
                                            HistoryUserId = historyUser.Id,
                                            Month = monthInt,
                                            Year = yearInt,
                                            ReportCategoryId = 101,
                                            OfficeId = office?.Id,
                                            ZoneId = zone?.Id,
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

                                //Chỉ tiêu - thực đạt cuộc gọi chi nhánh
                                if (office != null)
                                {
                                    //Chỉ tiêu
                                    var reportCallOfficeTarget = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                                    if (reportCallOfficeTarget == null)
                                        reportCallOfficeTarget = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                                    var reportCallOfficeTD = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                                    if (reportCallOfficeTD == null)
                                        reportCallOfficeTD = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                                    if (reportCallOfficeTarget == null)
                                    {
                                        reportCallOfficeTarget = new ReportData()
                                        {
                                            Data = callTarget.ToString("N0"),
                                            DataReal = callTarget,
                                            Month = monthInt,
                                            Year = yearInt,
                                            ReportCategoryId = 26,
                                            //check null
                                            OfficeId = office.Id,
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

                                    if (reportCallOfficeTD == null)
                                    {
                                        reportCallOfficeTD = new ReportData()
                                        {
                                            Data = countTD.ToString("N0"),
                                            DataReal = countTD,
                                            Month = monthInt,
                                            Year = yearInt,
                                            ReportCategoryId = 27,
                                            //check null
                                            OfficeId = office.Id,
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
                            }
                        }

                    }

                    if (historyUserList.Any())
                        _unitOfWork.HistoryUserRepository.InsertRange(historyUserList);
                    _unitOfWork.Save();

                    //var offices = _unitOfWork.OfficeRepository.GetQuery();
                    var newListHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == monthInt && a.Year == yearInt);
                    var listId = new List<int> { 22, 23, 24 };
                    var reportDataDBSales = _unitOfWork.ReportDataRepository.Get(a => a.Month == monthInt && a.Year == yearInt && listId.Contains(a.ReportCategoryId));

                    // Tính % HT cuộc gọi CN; ĐB Sale
                    foreach (var office in offices)
                    {
                        // Cuộc gọi CN
                        var callTarget = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                        if (callTarget == null)
                            callTarget = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                        var callTD = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                        if (callTD == null)
                            callTD = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                        var callHT = reportCallHTOffices.FirstOrDefault(a => a.OfficeId == office.Id);
                        if (callHT == null)
                            callHT = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 28);
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

                                reportDataList.Add(callHT);
                            }
                        }

                        //Định biên sale

                        //var DBSale = reportDataDBSales.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 22);
                        //if (DBSale == null)
                        //    DBSale = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 22);
                        //if (DBSale?.DataReal > 0)
                        //{
                        //    var countNVKD = newListHistoryUser.Count(a => a.OfficeId == office.Id && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT));
                        //    var TDDBSale = reportDataDBSales.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 23);
                        //    if (TDDBSale == null)
                        //        TDDBSale = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 23);
                        //    if (TDDBSale == null)
                        //    {
                        //        TDDBSale = new ReportData()
                        //        {
                        //            Data = countNVKD.ToString(),
                        //            DataReal = countNVKD,
                        //            Month = monthInt,
                        //            Year = yearInt,
                        //            ReportCategoryId = 23,
                        //            OfficeId = office.Id,
                        //            Sort = 5,
                        //        };

                        //        reportDataList.Add(TDDBSale);
                        //    }
                        //    else
                        //    {
                        //        TDDBSale.Data = countNVKD.ToString();
                        //        TDDBSale.DataReal = countNVKD;
                        //    }
                        //    if (TDDBSale?.DataReal != null)
                        //    {
                        //        var ht = countNVKD / DBSale.DataReal;
                        //        var htDBSale = reportDataDBSales.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 24);
                        //        if (htDBSale == null)
                        //            htDBSale = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 24);
                        //        if (htDBSale != null)
                        //        {
                        //            htDBSale.Data = ((ht ?? 0) * 100).ToString("F2") + "%";
                        //            htDBSale.DataReal = ht;
                        //        }
                        //        else
                        //        {
                        //            htDBSale = new ReportData()
                        //            {
                        //                Data = ((ht ?? 0) * 100).ToString("F2") + "%",
                        //                DataReal = ht,
                        //                Month = monthInt,
                        //                Year = yearInt,
                        //                ReportCategoryId = 24,
                        //                OfficeId = office.Id,
                        //                Sort = 6,
                        //            };

                        //            reportDataList.Add(htDBSale);
                        //        }
                        //    }
                        //}

                    }
                    if (reportDataList.Any())
                        _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
                    _unitOfWork.Save();

                }
                // Chỉ tiêu CN - NV

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


                var reportDataList2 = new List<ReportData>();
                var newRevenueList = new List<RevenueOffice>();
                var newRevenueList2 = new List<RevenueUser_Month>();

                for (var i = 1; i < tbl.Rows.Count; i++)
                {

                    var officeshortname = tbl.Rows[i][0].ToString().Trim();
                    var office = offices.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == officeshortname.Normalize(NormalizationForm.FormC));
                    if (office == null)
                    {
                        ModelState.AddModelError("", @"Không tồn tại chi nhánh nào có tên ngắn là " + officeshortname);
                        return View();
                    }

                    var historyOffice = historyOffices.FirstOrDefault(a => a.OfficeId == office.Id);
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

                    var targetBase = tbl.Rows[i][10].ToString().Trim();
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
                    var historyUserMonths = historyUserMonthList.Where(a => a.OfficeId == office.Id);

                    var revenueOffice = revenueOffices.FirstOrDefault(a => a.OfficeId == office.Id);
                    if (revenueOffice == null)
                    {
                        revenueOffice = newRevenueList.FirstOrDefault(a => a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id);
                    }
                    // Reset chỉ tiêu CN
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
                            HistoryUser oldPosittion = null;
                            var startDateReal = item.DayStart;
                            //if (item.Status == StatusUser.Active)
                            //{
                            //    oldPosittion = oldPosittions.FirstOrDefault(a => a.UserId == item.UserId);
                            //    if (oldPosittion != null)
                            //    {
                            //        if (oldPosittion.DayEnd == null)
                            //        {
                            //            ModelState.AddModelError("", @"Nhân sự điều chuyển " + oldPosittion.User.MaNhanVien + " không có ngày điều chuyển");
                            //            return View();
                            //        }
                            //        startDateReal = oldPosittion.DayEnd.Value;
                            //    }
                            //}
                            // Khởi tạo số ngày làm việc thực tế
                            int workingDayTT = 0;
                            bool nsFullTarget = true;
                            if ((startDateReal.Year < yearInt || (startDateReal.Year == yearInt && startDateReal.Month < monthInt)) && (item.DayEnd == null || (item.DayEnd != null && item.DayEnd.Value.Month > monthInt)))
                            {
                                workingDayTT = workingDayFull;
                            }
                            else
                            {

                                DateTime ngayBatDau = item.DayStart.Year < yearInt || (item.DayStart.Year == yearInt && item.DayStart.Month < monthInt) ? new DateTime(yearInt, monthInt, 1) : item.DayStart;
                                if (oldPosittion != null)
                                {
                                    // Số ngày làm việc ở vị trí cũ (tính cả ngày nghỉ)
                                    var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                                    // Số ngày làm việc ở vị trí cũ (sau khi trừ ngày nghỉ)
                                    var oldDayWork = oldDayFull - (oldDayFull / 6);
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
                                    // edit
                                    int soNgayNghi = soNgayLamViec / 6;
                                    workingDayTT = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                                }

                            }
                            if (item.DayReduce > 0)
                            {
                                workingDayTT -= item.DayReduce ?? 0;
                            }
                            workingDayTT = Math.Max(workingDayTT, 0);
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
                                reportDatactHV = reportDataList2.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 95);

                            // Tính số ngày từ khi khai trương
                            int totalMonths = (yearInt - office.OpenDate.Value.Year) * 12 + (monthInt - office.OpenDate.Value.Month);
                            var STHBQ = totalMonths > 6 ? 20 : 12;
                            decimal ctHV = 0;
                            ctHV = targetNS / (2989000 * ((decimal)50 / 100) * STHBQ);
                            // Cộng dồn chỉ tiêu HV CN
                            chitieuHVCN += ctHV;
                            if (reportDatactHV == null)
                            {
                                reportDatactHV = new ReportData()
                                {
                                    Data = ctHV.ToString("N2"),
                                    UserId = item.UserId,
                                    HistoryUserId = item.Id,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 95,
                                    OfficeId = office.Id,
                                    Sort = 15,
                                    DataReal = ctHV
                                };
                                reportDataList2.Add(reportDatactHV);
                            }
                            else
                            {
                                reportDatactHV.Data = ctHV.ToString("N2");
                                reportDatactHV.DataReal = ctHV;
                            }
                            // %ht báo cáo  HV NV
                            if (reportDatactHV.DataReal > 0)
                            {
                                // thực đạt HV NV
                                var reportTDHVNV = reportTDHVNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                                if (reportTDHVNV != null)
                                {
                                    var TDHVNV = reportTDHVNV.DataReal ?? 0;
                                    var htHVNV = TDHVNV / reportDatactHV.DataReal * 100;
                                    var datahtHVNV = datahtHVNVs.FirstOrDefault(a => a.HistoryUserId == item.Id);
                                    if (datahtHVNV == null)
                                        datahtHVNV = reportDataList2.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 97);
                                    if (datahtHVNV == null)
                                    {
                                        datahtHVNV = new ReportData()
                                        {
                                            Data = (htHVNV ?? 0).ToString("F2") + "%",
                                            DataReal = htHVNV,
                                            Month = monthInt,
                                            Year = yearInt,
                                            ReportCategoryId = 97,
                                            OfficeId = office.Id,
                                            Sort = 17,
                                        };
                                        reportDataList2.Add(datahtHVNV);
                                    }
                                    else
                                    {
                                        datahtHVNV.Data = (htHVNV ?? 0).ToString("F2") + "%";
                                        datahtHVNV.DataReal = htHVNV;
                                    }
                                }
                            }

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
                            targetNS = targetBaseDec / (historyOffice.DBEC + historyOffice.DBATL) * 65 / 100;
                            revenueOffice.Target_SAB += targetNS;
                        }
                        // Báo cáo chỉ tiêu doanh số nhân sự
                        var reportData = reportDatas.FirstOrDefault(a => a.HistoryUserId == item.Id);
                        if (reportData == null)
                            reportData = reportDataList2.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 87);
                        if (reportData == null)
                        {
                            reportData = new ReportData()
                            {
                                Data = targetNS.ToString("N0"),
                                DataReal = targetNS,
                                UserId = item.UserId,
                                HistoryUserId = item.Id,
                                Month = monthInt,
                                Year = yearInt,
                                ReportCategoryId = 87,
                                OfficeId = office.Id,
                                Sort = 10,
                            };
                            reportDataList2.Add(reportData);
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
                                    datahtDSNV = reportDataList2.FirstOrDefault(a => a.ReportCategoryId == 89 && a.Month == monthInt && a.Year == yearInt && a.HistoryUserId == item.Id);
                                if (datahtDSNV == null)
                                {
                                    datahtDSNV = new ReportData()
                                    {
                                        Data = htDSNV.ToString("F2") + "%",
                                        DataReal = htDSNV / 100,
                                        UserId = item.UserId,
                                        HistoryUserId = item.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 89,
                                        OfficeId = office.Id,
                                        Sort = 12,
                                    };
                                    reportDataList2.Add(datahtDSNV);
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
                            r = newRevenueList2.FirstOrDefault(a => a.HistoryUserId == item.Id && a.Month == monthInt && a.Year == yearInt);
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
                    if (historyOffice.TargetReduce > 0)
                    {
                        revenueOffice.Target_TS -= (historyOffice.TargetReduce ?? 0);
                    }
                    if (historyOffice.NVKDOver > 0)
                    {
                        var nvkdOver = historyOffice.NVKDOver ?? 0;

                        var listRevenueUserDataBase = listRevenueUserDataBases.Where(a => a.HistoryUser.OfficeId == historyOffice.OfficeId);

                        var listRevenueUserNew = newRevenueList2.Where(a => a.HistoryUser != null && a.HistoryUser.OfficeId == historyOffice.OfficeId
                        && (a.HistoryUser.TypeUser == TypeUser.EC || a.HistoryUser.TypeUser == TypeUser.ALT) && a.Month == monthInt && a.Year == yearInt);

                        var mergedList = listRevenueUserDataBase.Concat(listRevenueUserNew).OrderByDescending(a => a.Target).ToList();

                        var countNVKD = historyUserMonths.Where(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT).Count();
                        //var countNVKD = listRevenueUser.Count();
                        // Nếu số NVKD không bằng số chỉ tiêu của NVKD
                        var countRevenueUser = mergedList.Count();
                        //if (countRevenueUser != countNVKD)
                        //{
                        //    ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " có số bản ghi NVKD trong tháng là " + countNVKD + ", nhưng số bản ghi chỉ tiêu của NVKD trong tháng là " + countRevenueUser);
                        //    return View();
                        //}
                        // Nếu số NVKD không bằng số chỉ tiêu của NVKD
                        //if (listRevenueUser.Count() != countNVKD)
                        //{
                        //    ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " có số bản ghi NVKD trong tháng là " + countNVKD + ", nhưng số bản ghi chỉ tiêu của NVKD trong tháng là "+ listRevenueUser.Count());
                        //    return View();
                        //}
                        var skipNVKD = countNVKD - nvkdOver;
                        if (skipNVKD < DBKD)
                        {
                            ModelState.AddModelError("", @"Chi nhánh " + officeshortname + " có định biên NVKD là " + DBKD + ", mà hiện tại đang có " + countNVKD + " NVKD, không thể giảm trừ chỉ tiêu " + historyOffice.NVKDOver + " NVKD");
                            return View();
                        }
                        var sumTargetDown = mergedList.Skip(skipNVKD).Take(nvkdOver).Sum(a => a.Target);
                        revenueOffice.Target_TS -= sumTargetDown;
                    }
                    revenueOffice.Target_TS = Math.Max(targetBaseDec, revenueOffice.Target_TS);
                    //Chỉ tiêu báo cáo doanh thu chi nhánh
                    var reportDataCN = reportDataCNs.FirstOrDefault(a => a.OfficeId == office.Id);
                    if (reportDataCN == null)
                        reportDataCN = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 34);
                    if (reportDataCN == null)
                    {
                        reportDataCN = new ReportData()
                        {
                            Data = revenueOffice.Target_TS.ToString("N0"),
                            DataReal = revenueOffice.Target_TS,
                            Month = monthInt,
                            Year = yearInt,
                            ReportCategoryId = 34,
                            OfficeId = office.Id,
                            Sort = 13,
                        };
                        reportDataList2.Add(reportDataCN);
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
                                datahtDSCN = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 36);
                            if (datahtDSCN == null)
                            {
                                datahtDSCN = new ReportData()
                                {
                                    Data = (htDSCN ?? 0).ToString("F2") + "%",
                                    DataReal = htDSCN,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 36,
                                    OfficeId = office.Id,
                                    Sort = 15,
                                };
                                reportDataList2.Add(datahtDSCN);
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
                        reportDataHVCN = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 30);
                    if (reportDataHVCN == null)
                    {
                        reportDataHVCN = new ReportData()
                        {
                            Data = chitieuHVCN.ToString("N2"),
                            Month = monthInt,
                            Year = yearInt,
                            ReportCategoryId = 30,
                            OfficeId = office.Id,
                            Sort = 10,
                            DataReal = chitieuHVCN,
                        };
                        reportDataList2.Add(reportDataHVCN);
                    }
                    else
                    {
                        reportDataHVCN.Data = chitieuHVCN.ToString("N2");
                        reportDataHVCN.DataReal = chitieuHVCN;
                    }
                    // % ht báo cáo HV CN
                    if (reportDataHVCN.DataReal > 0)
                    {
                        // thực đạt HV CN
                        var reportTDHVCN = reportTDHVCNs.FirstOrDefault(a => a.OfficeId == office.Id);
                        if (reportTDHVCN != null)
                        {
                            var TDHVCN = reportTDHVCN.DataReal ?? 0;
                            var htHVCN = TDHVCN / reportDataHVCN.DataReal * 100;
                            var datahtHVCN = datahtHVCNs.FirstOrDefault(a => a.OfficeId == office.Id);
                            if (datahtHVCN == null)
                                datahtHVCN = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 32);
                            if (datahtHVCN == null)
                            {
                                datahtHVCN = new ReportData()
                                {
                                    Data = (htHVCN ?? 0).ToString("F2") + "%",
                                    DataReal = htHVCN,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 32,
                                    OfficeId = office.Id,
                                    Sort = 12,
                                };
                                reportDataList2.Add(datahtHVCN);
                            }
                            else
                            {
                                datahtHVCN.Data = (htHVCN ?? 0).ToString("F2") + "%";
                                datahtHVCN.DataReal = htHVCN;
                            }
                        }
                    }
                }

                if (newRevenueList.Any())
                {
                    _unitOfWork.RevenueOfficeRepository.InsertRange(newRevenueList);
                }
                if (newRevenueList2.Any())
                {
                    _unitOfWork.RevenueUser_MonthRepository.InsertRange(newRevenueList2);
                }
                if (reportDataList2.Any())
                {
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList2);
                }
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
                var reportDataList = new List<ReportData>();

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
                    // chỉ tiêu PBDS CN
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
                    // Báo cáo chỉ tiêu doanh số chi nhánh
                    var reportCTDSCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 34).FirstOrDefault();
                    if (reportCTDSCN == null)
                        reportCTDSCN = reportDataList.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 34);
                    if (reportCTDSCN == null)
                    {
                        reportCTDSCN = new ReportData()
                        {
                            Data = targetTSDec.ToString("N0"),
                            DataReal = targetTSDec,
                            Month = monthInt,
                            Year = yearInt,
                            ReportCategoryId = 34,
                            OfficeId = office.Id,
                            Sort = 13,
                        };
                        reportDataList.Add(reportCTDSCN);
                    }
                    else
                    {
                        reportCTDSCN.Data = targetTSDec.ToString("N0");
                        reportCTDSCN.DataReal = targetTSDec;
                    }
                    // Báo cáo %ht DS nhân sự
                    if (targetTSDec > 0)
                    {
                        // thực đạt doanh số NV
                        var reportTDDSCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 35).FirstOrDefault();
                        if (reportTDDSCN != null)
                        {
                            var TDDSCN = reportTDDSCN.DataReal ?? 0;
                            var htDSCN = TDDSCN / targetTSDec;
                            var datahtDSCN = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 36 && a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id).FirstOrDefault();
                            if (datahtDSCN == null)
                                datahtDSCN = reportDataList.FirstOrDefault(a => a.ReportCategoryId == 36 && a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id);
                            if (datahtDSCN == null)
                            {
                                datahtDSCN = new ReportData()
                                {
                                    Data = (htDSCN * 100).ToString("F2") + "%",
                                    DataReal = htDSCN,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 36,
                                    OfficeId = office.Id,
                                    Sort = 15,
                                };
                                reportDataList.Add(datahtDSCN);
                            }
                            else
                            {
                                datahtDSCN.Data = (htDSCN * 100).ToString("F2") + "%";
                                datahtDSCN.DataReal = htDSCN;
                            }
                        }
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
                    // chỉ tiêu PBDS NV
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
                    // Báo cáo chỉ tiêu doanh số nhân sự
                    var reportData = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 87).FirstOrDefault();
                    if (reportData == null)
                        reportData = reportDataList.FirstOrDefault(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 87);
                    if (reportData == null)
                    {
                        reportData = new ReportData()
                        {
                            Data = targetDec.ToString("N0"),
                            DataReal = targetDec,
                            UserId = historyUser.UserId,
                            HistoryUserId = historyUser.Id,
                            Month = monthInt,
                            Year = yearInt,
                            ReportCategoryId = 87,
                            OfficeId = office.Id,
                            Sort = 10,
                        };
                        reportDataList.Add(reportData);
                    }
                    else
                    {
                        reportData.Data = targetDec.ToString("N0");
                        reportData.DataReal = targetDec;
                    }
                    // Báo cáo %ht DS nhân sự
                    if (targetDec > 0)
                    {
                        // thực đạt doanh số NV
                        var reportTDDSNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 88).FirstOrDefault();
                        if (reportTDDSNV != null)
                        {
                            var TDDSNV = reportTDDSNV.DataReal ?? 0;
                            var htDSNV = TDDSNV / targetDec;
                            var datahtDSNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.ReportCategoryId == 89 && a.Month == monthInt && a.Year == yearInt && a.HistoryUserId == historyUser.Id).FirstOrDefault();
                            if (datahtDSNV == null)
                                datahtDSNV = reportDataList.FirstOrDefault(a => a.ReportCategoryId == 89 && a.Month == monthInt && a.Year == yearInt && a.HistoryUserId == historyUser.Id);
                            if (datahtDSNV == null)
                            {
                                datahtDSNV = new ReportData()
                                {
                                    Data = (htDSNV * 100).ToString("F2") + "%",
                                    DataReal = htDSNV,
                                    UserId = historyUser.UserId,
                                    HistoryUserId = historyUser.Id,
                                    Month = monthInt,
                                    Year = yearInt,
                                    ReportCategoryId = 89,
                                    OfficeId = office.Id,
                                    Sort = 12,
                                };
                                reportDataList.Add(datahtDSNV);
                            }
                            else
                            {
                                datahtDSNV.Data = (htDSNV * 100).ToString("F2") + "%";
                                datahtDSNV.DataReal = htDSNV;
                            }
                        }
                    }
                }

                if (reportDataList.Any())
                {
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.GetQuery(), "Id", "ShortName"),
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "ShortName", "ShortName"),
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
        public ActionResult TestSyncPhieuThu(int date, int month)
        {
            var phieuthuService = new PhieuThuService();
            phieuthuService.TestSyncPhieuThu(date, month);
            return Content("Đã đồng bộ phiếu thu");
        }
        public ActionResult SyncPhieuThu()
        {
            var phieuthuService = new PhieuThuService();
            phieuthuService.SyncPhieuThu();
            return Content("Đã đồng bộ phiếu thu thủ công");
        }
        public ActionResult SyncUserAsync()
        {
            var userService = new UserService();
            userService.SyncUser();
            return Content("Đã đồng bộ nhân sự");
        }

        public ActionResult DeleteDatax2(string listId, int month)
        {

            var listIdString = listId.Split(',');
            var listIdInt = new List<int>();
            foreach (var item in listIdString)
            {
                var id = int.Parse(item);
                listIdInt.Add(id);
            }
            var datas = _unitOfWork.ReportDataRepository.GetQuery(a => listIdInt.Contains(a.ReportCategoryId) && a.Month == month);
            datas.Delete();
            return Content("ok");
        }
        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
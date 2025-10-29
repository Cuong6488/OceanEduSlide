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
namespace OceanEduSlide.DAL
{
    public class UserService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();
        private readonly UserTypeService _userTypeService = new UserTypeService();


        public void SyncUser()
        {
            var config = _unitOfWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            if (config == null || !config.AutoUser)
            {
                return;
            }
            var password = HtmlHelpers.ComputeHash(config.Password ?? "AUG2025@#", "SHA256", null);

            //var allCDCM = _userTypeService.GetAllCDCM();
            //var allCDCM = new HashSet<string>(_userTypeService.GetAllCDCM());

            var today = DateTime.Now.Date;
            var currentMonth = today.Month;
            var currentYear = today.Year;
            int lastMonth = 0;
            int yearLastMonth = 0;
            if (currentMonth == 1)
            {
                lastMonth = 12;
                yearLastMonth = currentYear - 1;
            }
            else
            {
                lastMonth = currentMonth - 1;
                yearLastMonth = currentYear;
            }

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
            var DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.Where(a => (a.TrangThai == "E_HIRE" || (a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month >= currentMonth))/* && allCDCM.Contains(a.MaChucDanhChuyenMon)*/).ToList();
            //var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.Where(a => allCDCM.Contains(a.MaChucDanh) || a.Loai == "VaoLamlai").OrderByDescending(a => a.NgayApDung).ToList();
            var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.OrderByDescending(a => a.NgayApDung).ToList();

            var ThaiSans = _dongBoTuyenSinh.ThaiSans.Where(t => today >= t.NgayBatDauNghiThaiSan && today <= t.NgayKetthucNghiThaiSan).ToList();

            // List User tháng
            var listHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);
            // Nghỉ thai sản
            var listNSTS = DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE" && ThaiSans.Any(t => t.IDNhanSuHRM == a.IDNhanSuHRM && t.NgayBatDauNghiThaiSan.Month == currentMonth)).ToList();
            // Trạng thái Stop - đã nghỉ
            var listNSStop_danghi = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == currentMonth && a.NgayNghiViec.Value.Year == currentYear && a.TrangThai == "E_STOP" && today > a.NgayNghiViec).ToList();
            // Trạng thái Stop - vẫn đang làm việc
            var listNSStop_danglamviec = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.TrangThai == "E_STOP" && today <= a.NgayNghiViec).ToList();
            // Trạng thái E_Hire - đang làm việc
            var listNSDanglamviec = DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE" && !ThaiSans.Any(t => a.IDNhanSuHRM == t.IDNhanSuHRM)).ToList();

            // Tổng hợp danh sách NS đang làm việc
            var listAllDanglamviec = listNSDanglamviec.Concat(listNSStop_danglamviec);

            // Tổng hợp danh sách NS đã nghỉ
            var listAllNghiviec = listNSStop_danghi.Concat(listNSTS);



            var allOffice = _unitOfWork.OfficeRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            var allZone = _unitOfWork.ZoneRepository.GetQuery(a => a.Active).AsNoTracking().ToList();
            //var allHistoryUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == currentMonth && a.Year == currentYear).AsNoTracking().ToList();
            var users = _unitOfWork.UserRepository.GetQuery().ToList();
            //var usernews = new List<User>();
            var historyUserList = new List<HistoryUser>();
            //var userList = new List<User>();
            //var newRevenueList2 = new List<RevenueUser_Month>();

            // Danh sách Ns sau khi được Điều chuyển
            var listNSSauDieuchuyen = QuaTrinhCongTacs.Where(q => currentMonth == q.NgayApDung.Month && q.NgayApDung.Year == currentYear && (q.Loai == "DieuChuyen" || q.Loai == "BoNhiem" || q.Loai == "MienNhiem"));
            //Danh sách NS Điều chuyển
            //var listNSDieuchuyen = new List<QuaTrinhCongTac>();
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
                        logger.Error("Nhan su " + nhanSuNguon.MaNhanSu + ": Khong ton tai CDCM: " + nhanSuNguon.MaChucDanhChuyenMon);
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
                    if (string.IsNullOrEmpty(nhanSuNguon.MaChucDanhChuyenMon))
                    {
                        logger.Error("Nhan su " + nhanSuNguon.MaChucDanhChuyenMon + ": MaChucDanhChuyenMon null");
                        continue;
                    }
                    Office office = null;
                    Zone zone = null;
                    if (!string.IsNullOrEmpty(NsDieuchuyen.WorkPlaceName))
                    {

                        if (NsDieuchuyen.WorkPlaceName.Normalize(NormalizationForm.FormC) == "OE Buôn Ma Thuột")
                        {
                            NsDieuchuyen.WorkPlaceName = "OE BMT";
                        }
                        office = allOffice.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == NsDieuchuyen.WorkPlaceName.Normalize(NormalizationForm.FormC));
                        if (office == null)
                        {
                            zone = allZone.FirstOrDefault(a => a.Name.Normalize(NormalizationForm.FormC) == NsDieuchuyen.WorkPlaceName.Normalize(NormalizationForm.FormC));
                            if (zone == null)
                            {
                                logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + NsDieuchuyen.WorkPlaceName);
                                //continue;
                            }
                        }
                    }

                    var sort = _userTypeService.GetSort((TypeUser)type);
                    var user = users.FirstOrDefault(a => a.MaNhanVien == nhanSuNguon.MaNhanSu);
                    //if (user == null)
                    //{
                    //    user = usernews.FirstOrDefault(a => a.MaNhanVien == nhanSuNguon.MaNhanSu);
                    //}
                    //if (user != null)
                    //{
                    //    user.CDCM = item.MaChucDanh;
                    //    user.ZoneId = zone?.Id;
                    //}

                    if (user == null)
                    {
                        var newUser = new User
                        {
                            Username = nhanSuNguon.MaNhanSu,
                            MaNhanVien = nhanSuNguon.MaNhanSu,
                            Password = password,
                            Active = true,
                            OfficeId = office?.Id,
                            ZoneId = zone?.Id,
                            Fullname = nhanSuNguon.TenNhanSu,
                            SaleKit = true,
                            TypeUser = type,
                            CDCM = NsDieuchuyen.MaChucDanh,
                        };
                        _unitOfWork.UserRepository.Insert(newUser);
                        _unitOfWork.Save();
                        users.Add(newUser);

                        user = newUser;
                    }
                    else
                    {
                        user.CDCM = item.MaChucDanh;
                        user.ZoneId = zone?.Id;
                    }
                    if (office == null && zone == null)
                    {
                        logger.Error("Nhan su co noi lam viec null: " + nhanSuNguon.MaNhanSu);
                        continue;
                    }
                    var historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.DayStart.Date == ngayVaoLam.Value.Date && a.TypeUser == type && ((office != null && a.OfficeId == office.Id) || (office == null && a.OfficeId == null)));
                    if (historyUser != null)
                    {
                        historyUser.Status = StatusUser.Transfer;
                        historyUser.ZoneId = zone?.Id;
                        historyUser.CDCM = NsDieuchuyen.MaChucDanh;
                        historyUser.DayEnd = item.NgayApDung;
                        historyUser.Sort = sort;
                    }
                    else
                    {
                        var newhistoryUser = new HistoryUser
                        {
                            UserId = user.Id,
                            Month = currentMonth,
                            Year = currentYear,
                            TypeUser = (TypeUser)type,
                            OfficeId = office?.Id,
                            ZoneId = zone?.Id,
                            Status = StatusUser.Transfer,
                            DayStart = (DateTime)ngayVaoLam,
                            CDCM = NsDieuchuyen.MaChucDanh,
                            DayEnd = item.NgayApDung,
                            Sort = sort,
                            Active = true
                        };
                        historyUserList.Add(newhistoryUser);
                    }
                }
            }
            foreach (var item in listAllDanglamviec)
            {
                // Xử lý ns đang làm việc
                if (string.IsNullOrEmpty(item.MaNhanSu))
                {
                    logger.Error("Khong co ma nhan su, IDNhanSuHRM: " + item.IDNhanSuHRM);
                    continue;
                }
                var type = _userTypeService.GetTypeUser(item.MaChucDanhChuyenMon);
                if (type == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai CDCM: " + item.MaChucDanhChuyenMon);
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
                if (string.IsNullOrEmpty(item.MaChucDanhChuyenMon))
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": MaChucDanhChuyenMon null");
                    continue;
                }
                Office office = null;
                Zone zone = null;
                var QTCT = QuaTrinhCongTacs.FirstOrDefault(a => a.IDNhanSuHRM == item.IDNhanSuHRM);
                if (QTCT == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai QTCT");
                    continue;
                }
                if (!string.IsNullOrEmpty(QTCT.WorkPlaceName))
                {
                    if (QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC) == "OE Buôn Ma Thuột")
                    {
                        QTCT.WorkPlaceName = "OE BMT";
                    }
                    office = allOffice.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC));
                    if (office == null)
                    {
                        zone = allZone.FirstOrDefault(a => a.Name.Normalize(NormalizationForm.FormC) == QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC));
                        if (zone == null)
                        {
                            logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + QTCT.WorkPlaceName);
                            //continue;
                        }
                    }
                }

                var sort = _userTypeService.GetSort((TypeUser)type);
                var user = users.FirstOrDefault(a => a.MaNhanVien == item.MaNhanSu);
                if (user == null)
                {
                    var newUser = new User
                    {
                        Username = item.MaNhanSu,
                        MaNhanVien = item.MaNhanSu,
                        Password = password,
                        Active = true,
                        OfficeId = office?.Id,
                        ZoneId = zone?.Id,
                        Fullname = item.TenNhanSu,
                        SaleKit = true,
                        TypeUser = type,
                        CDCM = item.MaChucDanhChuyenMon,
                    };
                    _unitOfWork.UserRepository.Insert(newUser);
                    _unitOfWork.Save();
                    users.Add(newUser);
                    user = newUser;
                }
                else
                {
                    user.CDCM = item.MaChucDanhChuyenMon;
                    user.ZoneId = zone?.Id;
                    user.Active = true;
                    user.SaleKit = true;
                    if (user.OfficeId != office?.Id || user.TypeUser != type)
                    {
                        user.OfficeId = office?.Id;
                        user.TypeUser = type;
                    }
                }

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
                    historyUserOld.CDCM = item.MaChucDanhChuyenMon;
                    historyUserOld.TypeUser = (TypeUser)type;
                    historyUserOld.DayStart = (DateTime)ngayVaoLam;
                    historyUserOld.DayEnd = item.NgayNghiViec;
                    historyUserOld.Sort = sort;
                }
                else
                {
                    var historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.DayStart.Date == ngayVaoLam.Value.Date && a.TypeUser == type && ((office != null && a.OfficeId == office.Id) || (office == null && a.OfficeId == null)));

                    if (historyUser != null)
                    {
                        historyUser.Status = StatusUser.Active;
                        historyUser.ZoneId = zone?.Id;
                        historyUser.CDCM = item.MaChucDanhChuyenMon;
                        historyUser.DayEnd = item.NgayNghiViec;
                        historyUser.Sort = sort;
                    }
                    else
                    {
                        var newhistoryUser = new HistoryUser
                        {
                            UserId = user.Id,
                            Month = currentMonth,
                            Year = currentYear,
                            TypeUser = (TypeUser)type,
                            OfficeId = office?.Id,
                            ZoneId = zone?.Id,
                            Status = StatusUser.Active,
                            DayStart = (DateTime)ngayVaoLam,
                            CDCM = item.MaChucDanhChuyenMon,
                            DayEnd = item.NgayNghiViec,
                            Sort = sort,
                            Active = true
                        };
                        historyUserList.Add(newhistoryUser);
                    }
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
                var type = _userTypeService.GetTypeUser(item.MaChucDanhChuyenMon);
                if (type == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai CDCM: " + item.MaChucDanhChuyenMon);
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
                if (string.IsNullOrEmpty(item.MaChucDanhChuyenMon))
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": MaChucDanhChuyenMon null");
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
                var QTCT = QuaTrinhCongTacs.Where(a => a.IDNhanSuHRM == item.IDNhanSuHRM).OrderByDescending(a => a.NgayApDung).FirstOrDefault();
                if (QTCT == null)
                {
                    logger.Error("Nhan su " + item.MaNhanSu + ": Khong ton tai QTCT");
                    continue;
                }
                if (!string.IsNullOrEmpty(QTCT.WorkPlaceName))
                {
                    if (QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC) == "OE Buôn Ma Thuột")
                    {
                        QTCT.WorkPlaceName = "OE BMT";
                    }
                    office = allOffice.FirstOrDefault(a => a.ShortName.Normalize(NormalizationForm.FormC) == QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC));
                    if (office == null)
                    {
                        zone = allZone.FirstOrDefault(a => a.Name.Normalize(NormalizationForm.FormC) == QTCT.WorkPlaceName.Normalize(NormalizationForm.FormC));
                        if (zone == null)
                        {
                            logger.Error("Khong ton tai Chi nhanh hoac Vung nao co ten la: " + QTCT.WorkPlaceName);
                            //continue;
                        }
                    }
                }
                var sort = _userTypeService.GetSort((TypeUser)type);
                var user = users.FirstOrDefault(a => a.MaNhanVien == item.MaNhanSu);
                //if (user != null)
                //{

                //    user.CDCM = item.MaChucDanhChuyenMon;
                //    user.ZoneId = zone?.Id;
                //    user.Active = false;
                //    //continue;
                //}
                if (user == null)
                {
                    var newUser = new User
                    {
                        Username = item.MaNhanSu,
                        MaNhanVien = item.MaNhanSu,
                        Password = password,
                        Active = false,
                        OfficeId = office?.Id,
                        ZoneId = zone?.Id,
                        Fullname = item.TenNhanSu,
                        SaleKit = true,
                        TypeUser = type,
                        CDCM = item.MaChucDanhChuyenMon,
                    };
                    _unitOfWork.UserRepository.Insert(newUser);
                    _unitOfWork.Save();
                    users.Add(newUser);
                    user = newUser;
                }
                else
                {
                    user.CDCM = item.MaChucDanhChuyenMon;
                    user.ZoneId = zone?.Id;
                    user.Active = false;
                    user.SaleKit = true;
                    if (user.OfficeId != office?.Id || user.TypeUser != type)
                    {
                        user.OfficeId = office?.Id;
                        user.TypeUser = type;
                    }
                }
                if (office == null && zone == null)
                {
                    logger.Error("Nhan su co noi lam viec null: " + item.MaNhanSu);
                    continue;
                }
                var historyUser = listHistoryUser.FirstOrDefault(a => a.UserId == user.Id && a.DayStart.Date == ngayVaoLam.Value.Date && a.TypeUser == type && ((office != null && a.OfficeId == office.Id) || (office == null && a.OfficeId == null)));
                if (historyUser != null)
                {
                    historyUser.Status = StatusUser.InActive;
                    historyUser.ZoneId = zone?.Id;
                    historyUser.CDCM = item.MaChucDanhChuyenMon;
                    historyUser.DayEnd = ngayNghiViec;
                    historyUser.Sort = sort;
                }
                else
                {
                    var newhistoryUser = new HistoryUser
                    {
                        UserId = user.Id,
                        Month = currentMonth,
                        Year = currentYear,
                        TypeUser = (TypeUser)type,
                        OfficeId = office?.Id,
                        ZoneId = zone?.Id,
                        Status = StatusUser.InActive,
                        DayStart = (DateTime)ngayVaoLam,
                        CDCM = item.MaChucDanhChuyenMon,
                        DayEnd = ngayNghiViec,
                        Sort = sort,
                        Active = true
                    };
                    historyUserList.Add(newhistoryUser);
                }
            }

            //if (userList.Any())
            //    _unitOfWork.UserRepository.InsertRange(userList);
            if (historyUserList.Any())
                _unitOfWork.HistoryUserRepository.InsertRange(historyUserList);
            _unitOfWork.Save();

            // Báo cáo cuộc gọi
            var listReportCategoryId = new List<int> { 100, 26, 27, 99, 101 };
            var reportDatas = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && listReportCategoryId.Contains(a.ReportCategoryId));
            //var callLogs = _unitOfWork.CallLogRepository.GetQuery(a => a.CallDate.Year == currentYear && a.CallDate.Month == currentMonth && a.BillSec >= 60).AsNoTracking().ToList();

            // Group theo HistoryUserId
            //var callLogCounts = callLogs.GroupBy(a => a.HistoryUserId).ToDictionary(g => g.Key, g => g.Count());
            var listNewHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);
            var reportDataList = new List<ReportData>();
            foreach (var item in reportDatas.Where(a => a.ReportCategoryId == 26 || a.ReportCategoryId == 27))
            {
                item.Data = "0";
                item.DataReal = 0;
            }
            foreach (var historyUser in listNewHistoryUser)
            {
                //var zone = allZone.FirstOrDefault(a => a.Id == historyUser.ZoneId);
                //var office = allOffice.FirstOrDefault(a => a.Id == historyUser.OfficeId);

                // cuộc gọi thực đạt
                //var countTD = callLogCounts.TryGetValue(historyUser.Id, out var count) ? count : 0;
                var countTD = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.CallDate.Year == currentYear && a.CallDate.Month == currentMonth && a.BillSec >= 60).Count();
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
                    if (historyUser.TypeUser != TypeUser.EC && historyUser.TypeUser != TypeUser.ALT && historyUser.TypeUser != TypeUser.AEC)
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
                    int workingDayTT = 0;
                    if ((startDateReal.Year < currentYear || (startDateReal.Year == currentYear && startDateReal.Month < currentMonth)) && (historyUser.DayEnd == null || (historyUser.DayEnd != null && historyUser.DayEnd.Value.Month > currentMonth)))
                    {
                        workingDayTT = workingDayFull;
                    }
                    else
                    {
                        DateTime ngayBatDau = historyUser.DayStart.Year < currentYear || (historyUser.DayStart.Year == currentYear && historyUser.DayStart.Month < currentMonth) ? new DateTime(currentYear, currentMonth, 1) : historyUser.DayStart;
                        if (oldPosittion != null)
                        {
                            var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                            var oldDayWork = oldDayFull - (oldDayFull / 6);
                            workingDayTT = Math.Max(workingDayFull - oldDayWork, 0);
                        }
                        else
                        {
                            DateTime ngayKetThuc = historyUser.DayEnd != null ? historyUser.DayEnd.Value : new DateTime(currentYear, currentMonth, DateTime.DaysInMonth(currentYear, currentMonth));
                            int soNgayLamViec = (ngayKetThuc - ngayBatDau).Days + 1;
                            if (soNgayLamViec < 0)
                            {
                                logger.Error("Nhan vien " + historyUser.User?.MaNhanVien + " co ngay vao lam > ngay nghi viec");
                                continue;
                            }
                            int soNgayNghi = soNgayLamViec / 6;
                            workingDayTT = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                        }
                    }
                    if (historyUser.DayReduce > 0)
                    {
                        workingDayTT -= historyUser.DayReduce ?? 0;
                    }
                    int callTarget = 0;
                    if (historyUser.DayStart.Month == currentMonth || historyUser.DayStart.Month == lastMonth)
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
                    //var ht = ((double)countTD / callTarget * 100).ToString("F2") + "%";
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

                    //Chỉ tiêu - thực đạt cuộc gọi chi nhánh
                    if (historyUser.OfficeId != null)
                    {
                        //Chỉ tiêu
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
                }
            }

            if (reportDataList.Any())
                _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
            _unitOfWork.Save();

            // Tính % HT cuộc gọi CN
            var reportDataList2 = new List<ReportData>();
            listReportCategoryId.AddRange(new List<int> { 28, 22, 23, 24 });
            reportDatas = _unitOfWork.ReportDataRepository.Get(a => a.Month == currentMonth && a.Year == currentYear && listReportCategoryId.Contains(a.ReportCategoryId));
            var newListHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);

            foreach (var office in allOffice)
            {
                // Cuộc gọi chi nhánh
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

                // Định biên Sale

                var DBSale = reportDatas.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 22);
                //if (DBSale == null)
                //    DBSale = reportDataList2.FirstOrDefault(a => a.OfficeId == office.Id && a.Month == currentMonth && a.Year == currentYear && a.ReportCategoryId == 22);
                if (DBSale?.DataReal > 0)
                {
                    var countNVKD = newListHistoryUser.Count(a => a.OfficeId == office.Id && a.Status == StatusUser.Active && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT));
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
                    if (TDDBSale?.DataReal != null)
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
            }

            if (reportDataList2.Any())
                _unitOfWork.ReportDataRepository.InsertRange(reportDataList2);
            _unitOfWork.Save();
        }

        public async Task SyncUserAsync()
        {
            await Task.Run(() =>
            {
                SyncUser();
            });
        }
    }
}
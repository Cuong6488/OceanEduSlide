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

namespace OceanEduSlide.DAL
{
    public class UserService
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private DongBoTuyenSinhEntities _dongBoTuyenSinh = new DongBoTuyenSinhEntities();

        public void SyncUser()
        {
            var today = DateTime.Now.Date;
            var currentMonth = today.Month;
            var currentYear = today.Year;
            var DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.ToList();
            var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.ToList();
            var ThaiSans = _dongBoTuyenSinh.ThaiSans.ToList();

            // List User tháng
            var listHistoryUser = _unitOfWork.HistoryUserRepository.Get(a => a.Active && a.Month == currentMonth && a.Year == currentYear);
            // Nghỉ thai sản
            var listNSTS = DSNhanSuNguons.Where(a => ThaiSans.Any(t => t.IDNhanSuHRM == a.IDNhanSuHRM && today >= t.NgayBatDauNghiThaiSan && today <= t.NgayKetthucNghiThaiSan)).ToList();
            // Trạng thái Stop - đã nghỉ
            var listNSStop_danghi = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == currentMonth && a.TrangThai == "E_STOP" && today <= a.NgayNghiViec).ToList();
            // Trạng thái Stop - vẫn đang làm việc
            var listNSStop_danglamviec = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.TrangThai == "E_STOP" && today > a.NgayNghiViec).ToList();
            // Trạng thái E_Hire - đang làm việc
            var listNSDanglamviec = DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE").ToList();

            // Tổng hợp danh sách NS đang làm việc
            var listAllDanglamviec = listNSDanglamviec.Concat(listNSStop_danglamviec);

            // Tổng hợp danh sách NS đã nghỉ
            var listAllNghiviec = listNSStop_danghi.Concat(listNSTS);

            foreach (var item in listAllDanglamviec)
            {
                // Xử lý ns đang làm việc
                TypeUser type = new TypeUser();
                switch (item.MaChucDanhChuyenMon)
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
                        continue;
                        //break;
                }


            }
            foreach (var item in listAllNghiviec)
            {
                // Xử lý ns nghỉ việc / TS
            }

            // Danh sách Ns sau khi được Điều chuyển
            var listNSSauDieuchuyen = QuaTrinhCongTacs.Where(q => currentMonth == q.NgayApDung.Month && (q.Loai == "DieuChuyen" || q.Loai == "BoNhiem" || q.Loai == "MienNhiem"));
            //Danh sách NS Điều chuyển
            var listNSDieuchuyen = new List<QuaTrinhCongTac>();
            foreach (var item /*(banghiA)*/ in listNSSauDieuchuyen)
            {
                var NsDieuchuyen = QuaTrinhCongTacs.Where(a => a.IDNhanSuHRM == item.IDNhanSuHRM && a != item).OrderByDescending(a => a.NgayApDung).FirstOrDefault(); /*(bản ghi B)*/
                if (NsDieuchuyen != null)
                {
                    var manhanvien = DSNhanSuNguons.FirstOrDefault(a => a.IDNhanSuHRM == NsDieuchuyen.IDNhanSuHRM)?.MaNhanSu;
                    listNSDieuchuyen.Add(NsDieuchuyen);
                }

                // Ngày đc là ngày banghiA, chức danh và CN-Vùng là banghiB

            }
        }
        //public void SyncUser()
        //{
        //    var today = DateTime.Now.Date;
        //    var currentMonth = today.Month;
        //    var DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.ToList();
        //    var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.ToList();
        //    var ThaiSans = _dongBoTuyenSinh.ThaiSans.ToList();
        //    var listNSTS = new List<DSNhanSuNguon>();
        //    var listNSStop_danghi = new List<DSNhanSuNguon>();
        //    var listNSStop_danglamviec = new List<DSNhanSuNguon>();
        //    var listNSDanglamviec = new List<DSNhanSuNguon>();
        //    foreach (var ns in DSNhanSuNguons)
        //    {
        //        // Thai sản
        //        if (ThaiSans.Any(t => t.IDNhanSuHRM == ns.IDNhanSuHRM && today >= t.NgayBatDauNghiThaiSan.Date && today <= t.NgayKetthucNghiThaiSan.Date))
        //        {
        //            listNSTS.Add(ns);
        //        }

        //        // E_HIRE
        //        if (ns.TrangThai == "E_HIRE")
        //        {
        //            listNSDanglamviec.Add(ns);
        //        }
        //        // E_STOP
        //        else if (ns.TrangThai == "E_STOP" && ns.NgayNghiViec.HasValue)
        //        {
        //            if (ns.NgayNghiViec.Value.Month == currentMonth && today <= ns.NgayNghiViec.Value.Date)
        //            {
        //                listNSStop_danghi.Add(ns);
        //            }
        //            else if (today > ns.NgayNghiViec.Value.Date)
        //            {
        //                listNSStop_danglamviec.Add(ns);
        //            }
        //        }

        //    }
        //    // Nghỉ thai sản
        //    //var listNSTS = DSNhanSuNguons.Where(a => ThaiSans.Any(t => t.IDNhanSuHRM == a.IDNhanSuHRM && DateTime.Now.Date >= t.NgayBatDauNghiThaiSan && DateTime.Now.Date <= t.NgayKetthucNghiThaiSan)).ToList();
        //    //// Trạng thái Stop - đã nghỉ
        //    //var listNSStop_danghi = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.NgayNghiViec.Value.Month == DateTime.Now.Month && a.TrangThai == "E_STOP" && DateTime.Now.Date <= a.NgayNghiViec).ToList();
        //    //// Trạng thái Stop - vẫn đang làm việc
        //    //var listNSStop_danglamviec = DSNhanSuNguons.Where(a => a.NgayNghiViec.HasValue && a.TrangThai == "E_STOP" && DateTime.Now.Date > a.NgayNghiViec).ToList();
        //    //// Trạng thái E_Hire - đang làm việc
        //    //var listNSDanglamviec = DSNhanSuNguons.Where(a => a.TrangThai == "E_HIRE").ToList();

        //    // Tổng hợp danh sách NS đang làm việc
        //    var listAllDanglamviec = listNSDanglamviec.Concat(listNSStop_danglamviec);

        //    // Tổng hợp danh sách NS đã nghỉ
        //    var listAllNghiviec = listNSStop_danghi.Concat(listNSTS);

        //    foreach (var item in listAllDanglamviec)
        //    {
        //        // Xử lý ns đang làm việc

        //    }
        //    foreach (var item in listAllNghiviec)
        //    {
        //        // Xử lý ns nghỉ việc / TS
        //    }

        //    // Danh sách Ns sau khi được Điều chuyển
        //    var listNSSauDieuchuyen = QuaTrinhCongTacs.Where(q => currentMonth == q.NgayApDung.Month && (q.Loai == "DieuChuyen" || q.Loai == "BoNhiem" || q.Loai == "MienNhiem")).ToList();
        //    //Danh sách NS Điều chuyển
        //    var listNSDieuchuyen = new List<QuaTrinhCongTac>();
        //    var dictNhanSu = DSNhanSuNguons.ToDictionary(x => x.IDNhanSuHRM, x => x);
        //    var lookupQTCongTac = QuaTrinhCongTacs.GroupBy(q => q.IDNhanSuHRM).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.NgayApDung).ToList());

        //    foreach (var item /*(banghiA)*/ in listNSSauDieuchuyen)
        //    {
        //        //var NsDieuchuyen = QuaTrinhCongTacs.Where(a => a.IDNhanSuHRM == item.IDNhanSuHRM && a != item).OrderByDescending(a => a.NgayApDung).FirstOrDefault(); /*(bản ghi B)*/
        //        if (lookupQTCongTac.TryGetValue(item.IDNhanSuHRM, out var list))
        //        {
        //            var NsDieuchuyen = list.FirstOrDefault(x => x != item);
        //            if (NsDieuchuyen != null)
        //            {
        //                //var manhanvien = DSNhanSuNguons.FirstOrDefault(a => a.IDNhanSuHRM == NsDieuchuyen.IDNhanSuHRM)?.MaNhanSu;
        //                if (dictNhanSu.TryGetValue(NsDieuchuyen.IDNhanSuHRM, out var ns))
        //                {
        //                    var manhanvien = ns.MaNhanSu;
        //                }
        //                listNSDieuchuyen.Add(NsDieuchuyen);
        //            }
        //        }

        //        // Ngày đc là ngày banghiA, chức danh và CN-Vùng là banghiB

        //    }
        //}

        public async Task SyncUserAsync()
        {
            await Task.Run(() =>
            {
                SyncUser();
            });
        }
    }
}
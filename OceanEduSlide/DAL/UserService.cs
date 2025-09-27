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
            var DSNhanSuNguons = _dongBoTuyenSinh.DSNhanSuNguons.ToList();
            var QuaTrinhCongTacs = _dongBoTuyenSinh.QuaTrinhCongTacs.ToList();
            var ThaiSans = _dongBoTuyenSinh.ThaiSans.ToList();
            var listNSTS = DSNhanSuNguons.Where(a => ThaiSans.Any(t => t.IDNhanSuHRM.ToString() == a.IDNhanSuHRM.ToString() && DbFunctions.TruncateTime(DateTime.Now) >= t.NgayBatDauNghiThaiSan && DbFunctions.TruncateTime(DateTime.Now) <= t.NgayKetthucNghiThaiSan));
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
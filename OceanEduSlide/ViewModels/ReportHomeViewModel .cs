using OceanEduSlide.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{

    public class ListReportHomeViewModel
    {
        public IPagedList<Office> Offices { get; set; }
        public IEnumerable<User> Users { get; set; }
        public IEnumerable<ReportCategory> ReportCategories { get; set; }
        public IEnumerable<ReportData> ReportDatas { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? categoryId { get; set; }
        public int sort { get; set; }
        public User User { get; set; }
    }
    public class ListReportNVHomeViewModel
    {
        public IEnumerable<Office> Offices { get; set; }
        //public IPagedList<User> Users { get; set; }
        public IPagedList<HistoryUser> HistoryUsers { get; set; }
        public IEnumerable<ReportCategory> ReportCategories { get; set; }
        public IEnumerable<ReportData> ReportDatas { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public IEnumerable<HistoryUser> ListHistoryUser { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? UserId { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? categoryId { get; set; }
        public int sort { get; set; }
        public User User { get; set; }
        public int? UserType { get; set; }
        public List<int> ListMonth { get; set; }
    }

    public class BCTHCNViewModel
    {
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<ReportData> ReportDatas { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public int Year { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public User User { get; set; }
        public List<int> SoSale { get; set; }
        public List<int> DinhBien { get; set; }
        public List<decimal> ChiTieuDS { get; set; }
        public List<decimal> ThucDatDS { get; set; }
        public List<decimal> HTDS { get; set; }
        public List<decimal> TiTrongSale { get; set; }
        public List<decimal> TiTrongDaoTao { get; set; }
        public List<decimal> TiTrongKeToan { get; set; }
        public List<decimal> UuDaiBinhQuan { get; set; }
        public List<int> TongSoHocVien { get; set; }
        public List<int> HVGhiDanhLai { get; set; }
        public List<int> HVGhiDanhMoi { get; set; }
        public List<decimal> HTCuocGoi { get; set; }
        public List<decimal> HTHocVien { get; set; }
        public List<decimal> SaleOver100 { get; set; }
        public List<decimal> Sale30To50 { get; set; }
        public List<decimal> Sale20To30 { get; set; }
        public List<decimal> SaleUnder20 { get; set; }
        public List<decimal> DoanhThuNen { get; set; }
        public List<decimal> DoanhThuHocBong { get; set; }
        public List<decimal> DoanhThuVang { get; set; }
        public List<decimal> DoanhThuSuKien { get; set; }
    }

    public class ListCallViewModel
    {
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Zone> Zones { get; set; }

        [Display(Name = "Từ ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string StartDay { get; set; }
        [Display(Name = " Đến ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string EndDay { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public User User { get; set; }
        public IEnumerable<UserItem> UserItems { get; set; }
        public int TotalOver120s { get; set; }
        public int TotalOver90s { get; set; }
        public int TotalOver60s { get; set; }
        public int TotalUnder60s { get; set; }
        public int TotalUnder30s { get; set; }
        public class UserItem
        {
            //public User User { get; set; }
            public HistoryUser HistoryUser { get; set; }
            public int Over120s { get; set; }
            public int Over90s { get; set; }
            public int Over60s { get; set; }
            public int Under60s { get; set; }
            public int Under30s { get; set; }
            public int NoAns { get; set; }
            public int Busy { get; set; }
            public int Failed { get; set; }
            public int TotalOver60s { get; set; }
            public int TotalOver30s { get; set; }
            public int Total { get; set; }

        }

    }
    public class LoadListCallDayViewModel
    {

        public string StartDay { get; set; }
        public string EndDay { get; set; }
        public User User { get; set; }
        public IEnumerable<DateItem> DateItems { get; set; }

        public class DateItem
        {
            public DateTime Date { get; set; }
            public int Total { get; set; }
            public int Over60s { get; set; }
            public int Over30s { get; set; }
        }

    }

    public class LoadListCallViewModel
    {
        public HistoryUser User { get; set; }
        [Display(Name = "Từ ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string StartDay { get; set; }
        [Display(Name = " Đến ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string EndDay { get; set; }
        public IEnumerable<CallLog> CallLogs { get; set; }
    }
    public class ListLockImportViewModel
    {
        public PagedList.IPagedList<LockImport> LockImports { get; set; }
        public int? Type { get; set; }
        public SelectList SelectGroup { get; set; }
        public ListLockImportViewModel()
        {
            var listgroup = new Dictionary<int, string>
            {
                { 0, "Nhân sự tháng" },
                { 1, "Báo cáo KD CN - NV" },
            };
            SelectGroup = new SelectList(listgroup, "Key", "Value");
        }
    }
    public class ListReportCategoryViewModel
    {
        public IEnumerable<ReportCategory> ReportCategories { get; set; }
        public int? TypeCat { get; set; }
        public SelectList SelectGroup { get; set; }
        public ListReportCategoryViewModel()
        {
            var listgroup = new Dictionary<int, string>
            {
                { 1, "Chi Nhánh" },
                { 2, "Nhân sự" },
            };
            SelectGroup = new SelectList(listgroup, "Key", "Value");
        }
    }
}
using OceanEduSlide.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
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
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? categoryId { get; set; }
        public int sort { get; set; }
        public User User { get; set; }
        public int? UserType { get; set; }
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
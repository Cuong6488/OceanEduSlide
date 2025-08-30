using OceanEduSlide.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class RevenueViewModel
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? UserType { get; set; }
        public int? TypeView { get; set; }
        public SelectList SelectOffices { get; set; }
        public IPagedList<UserItem> UserItems { get; set; }
        public IEnumerable<OfficeItem> OfficeItems { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public RevenueOffice RevenueOffice { get; set; }
        //public IEnumerable<RevenueOffice> RevenueOffices { get; set; }
        public IEnumerable<RevenueOffice_BM> RevenueOffice_BMs { get; set; }
        public User User { get; set; }
        public class UserItem
        {
            //public User User { get; set; }
            public HistoryUser HistoryUser { get; set; }
            public RevenueUser_Month RevenueUser_Month { get; set; }
            public IEnumerable<RevenueUser_Month_BM> RevenueUser_Month_BMs { get; set; }
            public RevenueUser_Month_BM_real RevenueUser_Month_BM_real { get; set; }
            public IEnumerable<RevenueUser_Week> RevenueUser_Weeks { get; set; }
            public IEnumerable<RevenueUser_Week_Real> RevenueUser_Week_Reals { get; set; }
            public decimal Debt { get; set; }

        }
        public class OfficeItem
        {
            public Office Office { get; set; }
            public RevenueOffice RevenueOffice { get; set; }
            public IEnumerable<RevenueOffice_BM> RevenueOffice_BMs { get; set; }
        }
    }
    public class RevenueOfficeViewModel
    {
        public RevenueOffice RevenueOffice { get; set; }
        public SelectList SelectOffices { get; set; }
    }
    public class CategoryViewModel
    {
        public int? Month  { get; set; }
        public string MucLuc { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public List<string> Indexs { get; set; }
    }
    public class RevenueOffice_BMViewModel
    {
        public RevenueOffice_BM RevenueOffice { get; set; }
        [Display(Name = "Cam kết hoàn thành doanh số"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string TargetBM_TS { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo tái phí"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string TargetBM_HV { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo SAB"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string TargetBM_SAB { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo ghi danh mới"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string TargetBM_New { get; set; }
        public string OfficeName { get; set; }
    }
    public class ListRevenueOfficeViewModel
    {
        public PagedList.IPagedList<RevenueOffice> RevenueOffices { get; set; }
        public SelectList SelectOffices { get; set; }
        public int? OfficeId { get; set; }
    }
    public class ListRevenueUserViewModel
    {
        public PagedList.IPagedList<RevenueUser_Month> Revenues { get; set; }
        public SelectList SelectOffices { get; set; }
        public int? OfficeId { get; set; }
    }
    public class LoadHistoryRevenueUser_MonthViewModel
    {
        public User User { get; set; }
        public HistoryUser HistoryUser { get; set; }
        public IEnumerable<RevenueUser_Month_BM> Revenues { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
    public class LoadHistoryRevenueUser_WeekViewModel
    {
        public User User { get; set; }
        public IEnumerable<RevenueUser_Week> Revenues { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public WeekNumber WeekNumber { get; set; }
    }
    public class LoadHistoryRevenueOfficeViewModel
    {
        public Office Office { get; set; }
        public IEnumerable<RevenueOffice_BM> Revenues { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
    public class ListFileAllViewModel
    {
        public PagedList.IPagedList<LogImport> LogImports { get; set; }
        public int? TypeFile { get; set; }
        public SelectList SelectGroup { get; set; }
        public ListFileAllViewModel()
        {
            var listgroup = new Dictionary<int, string>
            {
                { 0, "Báo cáo TH CN - NV" },
                { 1, "Chỉ tiêu CN - NV - DS hoàn thành thực tế tuần" },
                { 2, "Vùng" },
                { 3, "Chi nhánh" },
                { 8, "Chi nhánh theo tháng" },
                { 4, "Tài khoản nhân sự" },
                { 5, "Nhân sự theo tháng" },
                { 6, "QĐ ưu đãi" },
                { 7, "Quy định chung/ QĐ PTS" },
                { 9, "Phiếu thu đặc biệt" },
            };
            SelectGroup = new SelectList(listgroup, "Key", "Value");
        }
    }

    public class InsertTargetGroupViewModel
    {
        public int? TargetGroupId { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm A"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string Target_A { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm B"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string Target_B { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm C"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string Target_C { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm D"), UIHint("MoneyBox"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string Target_D { get; set; }

    }

    public class ListTargetGroupViewModel
    {
        public PagedList.IPagedList<TargetGroup> TargetGroups { get; set; }
        public int? month { get; set; }
        public int? year { get; set; }
    }

    public class ListPhieuThuViewModel
    {
        public PagedList.IPagedList<BC_PhieuThu_DB> PhieuThus { get; set; }
        public SelectList SelectOffices { get; set; }
        public string officeId { get; set; }
        public int? type { get; set; }
        public int? month { get; set; }
        public int? year { get; set; }
        public string Username { get; set; }
    }
}

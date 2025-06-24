using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.ViewModels
{

    public class UserHomeViewModel
    {
        public IEnumerable<Debt> Debts { get; set; }
        public IEnumerable<RevenueUser_DayOfWeek> Revenues { get; set; }
        public User User { get; set; }
        public decimal TMonth { get; set; }
        public decimal RevenueMonthNow { get; set; }
        public decimal TargetMonthPercent { get; set; }
        public decimal RemainPercent { get; set; }
        public decimal TWeek { get; set; }
        public decimal RevenueWeekNow { get; set; }
        public decimal TargetWeekPercent { get; set; }
        [Display(Name = "Chỉ tiêu doanh số hoàn thành"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal? TargetBM { get; set; }
        [DisplayName("Số lượng data khai thác"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? DataQuantity { get; set; }

        [DisplayName("Số lượng data confirm lần 1"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm1 { get; set; }

        [DisplayName("Số lượng data confirm lần 2"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm2 { get; set; }

        [DisplayName("Số lượng data confirm lần 3"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm3 { get; set; }

        [DisplayName("Số lượng khách hàng check in"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? CI { get; set; }

        [DisplayName("Số lượng khách hàng chuyển đổi ra cọc/ doanh thu"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? DT { get; set; }
    }
    public class BMHomeViewModel
    {
        public int? OfficeId { get; set; }
        public IEnumerable<UserItem> UserItems { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public User User { get; set; }
        public string Date { get; set; }

        public class UserItem
        {
            public User User { get; set; }
            public IEnumerable<Debt> Debts { get; set; }
            public IEnumerable<RevenueUser_DayOfWeek> Revenues { get; set; }
            public RevenueUser_DayOfWeek_Real Report { get; set; }
            public decimal TMonth { get; set; }
            public decimal RevenueMonthNow { get; set; }
            public decimal TargetMonthPercent { get; set; }
            public decimal RemainPercent { get; set; }
            public decimal TWeek { get; set; }
            public decimal RevenueWeekNow { get; set; }
            public decimal TargetWeekPercent { get; set; }
            [Display(Name = "Chỉ tiêu doanh số hoàn thành"), DisplayFormat(DataFormatString = "{0:N0}đ")]
            public decimal? TargetBM { get; set; }
            [DisplayName("Số lượng data khai thác"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? DataQuantity { get; set; }

            [DisplayName("Số lượng data confirm lần 1"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? Confirm1 { get; set; }

            [DisplayName("Số lượng data confirm lần 2"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? Confirm2 { get; set; }

            [DisplayName("Số lượng data confirm lần 3"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? Confirm3 { get; set; }

            [DisplayName("Số lượng khách hàng check in"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? CI { get; set; }

            [DisplayName("Số lượng khách hàng chuyển đổi ra cọc/ doanh thu"), DisplayFormat(DataFormatString = "{0:N0}")]
            public decimal? DT { get; set; }
        }
    }
    public class ReportViewModel
    {
        public RevenueUser_DayOfWeek_Real Report { get; set; }
        [Display(Name = "Chỉ tiêu doanh số hoàn thành"), UIHint("MoneyBox"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public string TargetBM { get; set; }
        [DisplayName("Số lượng data khai thác"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? DataQuantity { get; set; }

        [DisplayName("Số lượng data confirm lần 1"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm1 { get; set; }

        [DisplayName("Số lượng data confirm lần 2"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm2 { get; set; }

        [DisplayName("Số lượng data confirm lần 3"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? Confirm3 { get; set; }

        [DisplayName("Số lượng khách hàng check in"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? CI { get; set; }

        [DisplayName("Số lượng khách hàng chuyển đổi ra cọc/ doanh thu"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal? DT { get; set; }
    }

    public class ListReportViewModel
    {
        public User User { get; set; }
        public IEnumerable<ReportItem> ReportItems { get; set; }
        public int OfficeId { get; set; }
        public string Date { get; set; }
        public class ReportItem
        {
            public User User { get; set; }
            public RevenueUser_DayOfWeek_Real Report { get; set; }
        }
    }
}
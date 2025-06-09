using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class RevenueUser_DayOfWeek
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? EventId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        [Display(Name = "Tuần")]
        public WeekNumber WeekNumber { get; set; }
        [Display(Name = "Thứ")]
        public DayofWeek DayofWeek { get; set; }
        [Display(Name = "Chỉ tiêu doanh số dự kiến hoàn thành"),DisplayFormat(DataFormatString = "{0:N0}đ"),RegularExpression(@"^\d*[1-9]\d*$", ErrorMessage = "Giá trị phải là số nguyên dương.")]
        public decimal TargetBM { get; set; }
        [DisplayName("Số lượng data khai thác"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal DataQuantity { get; set; }

        [DisplayName("Số lượng data confirm lần 1"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Confirm1 { get; set; }

        [DisplayName("Số lượng data confirm lần 2"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Confirm2 { get; set; }

        [DisplayName("Số lượng data confirm lần 3"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal Confirm3 { get; set; }

        [DisplayName("Số lượng khách hàng check in dự kiến"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal CI { get; set; }

        [DisplayName("Số lượng khách hàng chuyển đổi ra cọc/ doanh thu"), DisplayFormat(DataFormatString = "{0:N0}")]
        public decimal DT { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual User User { get; set; }
        public virtual Event Event { get; set; }
        public RevenueUser_DayOfWeek()
        {
            CreateDate = DateTime.Now;
            //Active = true;
        }
    }

    public enum DayofWeek
    {
        [Display(Name = "Thứ 2")]
        Monday = 2,
        [Display(Name = "Thứ 3")]
        Tuesday,
        [Display(Name = "Thứ 4")]
        Wednessday,
        [Display(Name = "Thứ 5")]
        Thursday,
        [Display(Name = "Thứ 6")]
        Friday,
        [Display(Name = "Thứ 7")]
        Saturday,
        [Display(Name = "Chủ nhật")]
        Sunday = 8,
    }
}
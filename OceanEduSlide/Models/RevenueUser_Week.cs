using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class RevenueUser_Week
    {
        public int Id { get; set; }
        [Display(Name = "Tên tuần"), StringLength(100, ErrorMessage = "Tối đa 20 ký tự"), UIHint("TextBox")]
        public string Name { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int UserId { get; set; }
        [Display(Name = "Chỉ tiêu doanh số dự kiến hoàn thành"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM { get; set; }
        [Display(Name = "Tuần")]
        public WeekNumber WeekNumber { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual User User { get; set; }
        [Display(Name = "Nhân sự theo tháng")]
        public int? HistoryUserId { get; set; }
        public virtual HistoryUser HistoryUser { get; set; }
        public RevenueUser_Week()
        {
            CreateDate = DateTime.Now;
            //Active = true;
        }
    }

    public enum WeekNumber
    {
        [Display(Name = "Tuần 1")]
        Week1 = 1,
        [Display(Name = "Tuần 2")]
        Week2,
        [Display(Name = "Tuần 3")]
        Week3,
        [Display(Name = "Tuần 4")]
        Week4,
        [Display(Name = "Tuần 5")]
        Week5,
        [Display(Name = "Tuần 6")]
        Week6,
    }
}
using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class RevenueUser_Week_Real
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
        public RevenueUser_Week_Real()
        {
            CreateDate = DateTime.Now;
            //Active = true;
        }
    }
}
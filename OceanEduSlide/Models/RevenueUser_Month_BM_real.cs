using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class RevenueUser_Month_BM_real
    {
        public int Id { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Nhân sự"), Required(ErrorMessage = "Hãy chọn nhân sự")]
        public int UserId { get; set; }
        [Display(Name = "Chỉ tiêu doanh số cam kết + hoàn thành thực tế"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual User User { get; set; }
        public RevenueUser_Month_BM_real()
        {
            CreateDate = DateTime.Now;
            //Active = true;
        }
    }
}
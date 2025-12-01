using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class RankOffice
    {
        public int Id { get; set; }
        //[Display(Name = "Tên chi nhánh"), Required(ErrorMessage = "Hãy nhập tên chi nhánh"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        //public string Name { get; set; }
        [Display(Name = "Tháng"), Required]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required]
        public int Year { get; set; }
        [Display(Name = "Chi nhánh"), Required]
        public int OfficeId { get; set; }
        [Display(Name = "Doanh số hoàn thành")]
        public string DSHT { get; set; }
        [Display(Name = "% hoàn thành")]
        public string PTHT { get; set; }
        [Display(Name = "Doanh số dự thu")]
        public string DSDT { get; set; }
        [Display(Name = "% hoàn thành dự thu")]
        public string PTHTDT { get; set; }
        [Display(Name = "BXH hoàn thành của CN")]
        public string TopHT { get; set; }
        [Display(Name = "BXH dự thu của CN")]
        public string TopDT { get; set; }
        //[Display(Name = "Chỉ tiêu doanh số dự kiến hoàn thành"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual Office Office { get; set; }
        public RankOffice()
        {
            CreateDate = DateTime.Now;
            Active = true;
        }
    }
}
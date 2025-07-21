using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class ReportData
    {
        public int Id { get; set; }
        [Display(Name = "Tháng"), Required]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required]
        public int Year { get; set; }
        [Display(Name = "Chi nhánh"), Required]
        public int OfficeId { get; set; }
        [Display(Name = "Cột"), Required]
        public int ReportCategoryId { get; set; }
        [Display(Name = "Data")]
        public string Data { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày cập nhật")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; } = true;
        [Display(Name = "Thứ tự"), Required]
        public int Sort { get; set; }
        public virtual ReportCategory ReportCategory { get; set; }
        public virtual Office Office { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class RevenueOffice_BM
    {
        public int Id { get; set; }
        //[Display(Name = "Tên chi nhánh"), Required(ErrorMessage = "Hãy nhập tên chi nhánh"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        //public string Name { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy chọn chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "Cam kết hoàn thành doanh số"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM_TS { get; set; }
        //[Display(Name = "% Cam kết hoàn thành doanh số"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal TargetBM_TS_Percent { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo tái phí"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM_HV { get; set; }
        //public decimal TargetBM_HV_Percent { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo SAB"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM_SAB { get; set; }
        //public decimal TargetBM_SAB_Percent { get; set; }
        [Display(Name = "Phân bổ doanh số dự kiến theo ghi danh mới"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TargetBM_New { get; set; }
        //public decimal TargetBM_New_Percent { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual Office Office { get; set; }
        public RevenueOffice_BM()
        {
            Active = true;
            CreateDate = DateTime.Now;
        }
    }
}
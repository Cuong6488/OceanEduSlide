using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class RevenueOffice
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
        [Display(Name = "Chỉ tiêu doanh số tuyển sinh"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_TS { get; set; }
        [Display(Name = "Chỉ tiêu doanh số học vụ"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_HV { get; set; }
        [Display(Name = "Chỉ tiêu doanh số kế toán"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_SAB { get; set; }
        //[Display(Name = "Cam kết hoàn thành doanh số"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM_TS { get; set; }
        ////[Display(Name = "% Cam kết hoàn thành doanh số"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM_TS_Percent { get; set; }
        //[Display(Name = "Phân bổ doanh số dự kiến theo tái phí"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM_HV { get; set; }
        //public decimal? TargetBM_HV_Percent { get; set; }
        //[Display(Name = "Phân bổ doanh số dự kiến theo SAB"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM_SAB { get; set; }
        //public decimal? TargetBM_SAB_Percent { get; set; }
        //[Display(Name = "Phân bổ doanh số dự kiến theo ghi danh mới"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //public decimal? TargetBM_New { get; set; }
        //public decimal? TargetBM_New_Percent { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public virtual Office Office { get; set; }
        //public RevenueOffice()
        //{
        //    Active = true;
        //    TargetBM_TS_Percent = TargetBM_TS == null ? null : (TargetBM_TS / Target_TS * 100);
        //    TargetBM_HV_Percent = TargetBM_HV == null ? null : (TargetBM_HV / Target_HV * 100);
        //    TargetBM_SAB_Percent = TargetBM_SAB == null ? null : (TargetBM_SAB / Target_SAB * 100);
        //    TargetBM_New_Percent = TargetBM_New == null ? null : (TargetBM_New / Target_TS * 100);
        //}
    }
}
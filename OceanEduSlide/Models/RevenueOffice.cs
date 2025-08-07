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
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual Office Office { get; set; }
        public RevenueOffice()
        {
            CreateDate = DateTime.Now;
        }
    }
}
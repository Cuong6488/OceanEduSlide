using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class TargetGroup
    {
        public int Id { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm A"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_A { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm B"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_B { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm C"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_C { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm D"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_D { get; set; }
        [Display(Name = "Chỉ tiêu NVĐT nhóm E"), Required(ErrorMessage = "Hãy nhập mục này"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal Target_E { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; } = true;
    }
}
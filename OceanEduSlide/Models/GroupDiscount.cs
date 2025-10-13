using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class GroupDiscount
    {
        public int Id { get; set; }
        [Display(Name = "Số QĐ"), Required(ErrorMessage = "Hãy điền tên QĐ"), UIHint("TextBox")]
        public string SoQD { get; set; }
        [Display(Name = "Nhóm QĐ"), Required(ErrorMessage = "Hãy điền tên QĐ"), UIHint("TextBox")]
        public string NhomQD { get; set; }
        public int Year { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Ngày bắt đầu")]
        public DateTime? StartDate { get; set; }
        [Display(Name = "Ngày kết thúc")]
        public DateTime? EndDate { get; set; }
        [Display(Name = "Ghi chú"), UIHint("Textbox")]
        public string Note { get; set; }
        [Display(Name = "Phân loại"), Required(ErrorMessage = "Hãy điền phân loại"), UIHint("Textbox")]
        public string PhanLoai { get; set; }
        [Display(Name = "Nội dung"), UIHint("Textbox")]
        public string Content { get; set; }
        public GroupDiscount()
        {
            Active = true;
        }
    }
}
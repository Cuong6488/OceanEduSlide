using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class Zone
    {
        public int Id { get; set; }
        [Display(Name = "Tên vùng"), Required(ErrorMessage = "Hãy nhập tên vùng"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        public string Name { get; set; }
        [Display(Name = "Mã vùng"), UIHint("TextBox")]
        public string ShortCode { get; set; }
        [Display(Name = "Các chi nhánh"), UIHint("TextBox")]
        public string ShortName { get; set; }
        [Display(Name = "Các Id chi nhánh"), UIHint("TextBox")]
        public string OfficeIds { get; set; }
        [Display(Name = "Hotline"), StringLength(20, ErrorMessage = "Tối đa 20 ký tự"), UIHint("TextBox")]
        public string Hotline { get; set; }
        [StringLength(50, ErrorMessage = "Tối đa 50 ký tự"), Display(Name = "Email"),
         EmailAddress(ErrorMessage = "Email không chính xác"), UIHint("TextBox")]
        public string Email { get; set; }
        public int Sort { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public virtual ICollection<Office> Offices { get; set; }
        public Zone()
        {
            Active = true;
            Sort = 1;
        }
    }
}
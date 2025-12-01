using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Office
    {
        public int Id { get; set; }
        [Display(Name = "Tên chi nhánh"), Required(ErrorMessage = "Hãy nhập tên chi nhánh"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        public string Name { get; set; }
        [Display(Name = "Địa chỉ"), UIHint("TextBox")]
        public string Place { get; set; }
        [Display(Name = "Mã chi nhánh"), UIHint("TextBox")]
        public string ShortCode { get; set; }
        [Display(Name = "Tên ngắn"), UIHint("TextBox")]
        public string ShortName { get; set; }
        [Display(Name = "Vùng")]
        public int? ZoneId { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        [Display(Name = "Ngày khai trương")]
        public DateTime? OpenDate { get; set; }
        [Display(Name = "Hotline"), StringLength(20, ErrorMessage = "Tối đa 20 ký tự"), UIHint("TextBox")]
        public string Hotline { get; set; }
        [StringLength(50, ErrorMessage = "Tối đa 50 ký tự"), Display(Name = "Email"),
         EmailAddress(ErrorMessage = "Email không chính xác"), UIHint("TextBox")]
        public string Email { get; set; }
        public string Image { get; set; }
        public int Sort { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public virtual ICollection<Discount> Discounts { get; set; }
        public virtual ICollection<Proposal> Proposals { get; set; }
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<RevenueOffice> RevenueOffices { get; set; }
        public virtual ICollection<RevenueOffice_BM> RevenueOffice_BMs { get; set; }
        public virtual ICollection<Event> Events { get; set; }
        public virtual Zone Zone { get; set; }
        public Office()
        {
            Active = true;
            Sort = 1;
        }
    }
}
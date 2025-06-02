using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{

    public class User
    {
        public int Id { get; set; }
        [Display(Name = "Tên đăng nhập", Description = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Họ và tên", Description = "Họ và tên"), UIHint("TextBox")]
        public string Fullname { get; set; }
        [DisplayName("Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "Phân quyền")]
        public TypeUser? TypeUser { get; set; }
        [Display(Name = "Các chi nhánh quản lý")]
        public string OfficeIds { get; set; }
        public virtual Office Office { get; set; }
        public virtual ICollection<RevenueUser_Month> RevenueUser_Months { get; set; }
        public virtual ICollection<RevenueUser_Month_BM> GetRevenueUser_Month_BMs { get; set; }
        public virtual ICollection<RevenueUser_Week> RevenueUser_Weeks { get; set; }
        public User()
        {
            Active = true;
        }
    }
    public enum TypeUser
    {
        [Display(Name = "User")]
        User,
        [Display(Name = "HO")]
        HO,
        [Display(Name = "ASM")]
        ASM,
        [Display(Name = "BM")]
        BM,
        [Display(Name = "EC")]
        EC,
        [Display(Name = "SL")]
        SL,
        [Display(Name = "SAB")]
        SAB,
        [Display(Name = "CM")]
        CM,

    }
}
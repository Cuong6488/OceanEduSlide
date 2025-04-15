using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using OceanEduSlide.Models;

namespace OceanEduSlide.ViewModels
{
    public class InfoAdminViewModel
    {
        //public IEnumerable<Article> Articles { get; set; }
        public IEnumerable<Admin> Admins { get; set; }
        //public IEnumerable<Banner> Banners { get; set; }
        //public IEnumerable<Product> Products { get; set; }
        //public IEnumerable<Person> People { get; set; }
        //public IEnumerable<ContactOffice> Contacts { get; set; }
        //public IEnumerable<Store> Stores { get; set; }
    }
    public class LoginAdminViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy nhập tên đăng nhập")]
        public string Username { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu")]
        public string Password { get; set; }
    }
    public class ChangePassWordViewModel
    {
        [Display(Name = "Mật khẩu hiện tại"), Required(ErrorMessage = "Hãy nhập mật khẩu hiện tại"), UIHint("Password")]
        public string OldPassword { get; set; }
        [Display(Name = "Mật khẩu mới"), Required(ErrorMessage = "Hãy nhập mật khẩu mới"),
         StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }
    }
    public class CreateAdminViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"),
            RegularExpression(@"[a-z0-9]{4,10}", ErrorMessage = "Chỉ nhập chữ thường và số 0-9, từ 4-10 ký tự"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu mới"),
         StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Hoạt động", Description = "Hoạt động")]
        public bool Active { get; set; }
        public CreateAdminViewModel()
        {
            Active = true;
        }
        public IEnumerable<Admin> Admins { get; set; }
    }
}
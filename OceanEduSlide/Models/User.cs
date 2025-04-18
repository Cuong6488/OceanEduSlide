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
        [DisplayName("Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Chi nhánh")]
        public int OfficeId { get; set; }
        public virtual Office Office { get; set; }
        public User()
        {
            Active = true;
        }
    }
}
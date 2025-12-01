using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace OceanEduSlide.Models
{
    public class Admin
    {
        public int Id { get; set; }
        [Display(Name = "Tên đăng nhập", Description = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"), RegularExpression(@"[a-z0-9]{4,10}", ErrorMessage = "Chỉ nhập chữ thường và số 0-9, từ 4-10 ký tự"), UIHint("TextBox")]
        public string Username { get; set; }
        [DisplayName("Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Hoạt động", Description = "Hoạt động")]
        public bool Active { get; set; }
        public RoleAdmin? RoleAdmin { get; set; }
        public Admin()
        {
            Active = true;
        }
    }
    public enum RoleAdmin
    {
        [Display(Name = "Quản trị viên")]
        Admin,
        [Display(Name = "Thư ký")]
        CopyWriter,
    }
}
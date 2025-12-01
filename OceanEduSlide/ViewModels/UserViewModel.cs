using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.ViewModels
{

    public class UserLoginModel
    {
        [Display(Name = "Id"), Required(ErrorMessage = "Hãy nhập Id")]
        public string Username { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu")]
        public string Password { get; set; }
    }

}
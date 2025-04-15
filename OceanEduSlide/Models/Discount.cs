using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Discount
    {
        public int Id { get; set; }
        [Display(Name = "Tên QĐ ưu đãi"), Required(ErrorMessage = "Hãy điền tên đăng nhập"), RegularExpression(@"[a-z0-9]{4,10}", ErrorMessage = "Chỉ nhập chữ thường và số 0-9, từ 4-10 ký tự"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name="Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy chọn chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "% ưu đãi")]
        public decimal? PercentDiscount { get; set; }
        [Display(Name = "Ưu đãi tiền mặt")]
        public int? MoneyDiscount { get; set; }
        [Display(Name = "Quà tặng"), UIHint("Textbox")]
        public string Gift { get; set; }
        public virtual Office Office { get; set; }
        public Discount()
        {
            Active = true;
        }
    }
}

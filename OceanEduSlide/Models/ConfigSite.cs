using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class ConfigSite
    {
        public int Id { get; set; }

        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn Facebook"),
         Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string Facebook { get; set; }
        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn Linkedin"),
         Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string Linkedin { get; set; }
        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn TikTok"),
        Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string TikTok { get; set; }
        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn Instagram"),
         Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string Instagram { get; set; }
        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn Twitter"),
         Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string Twitter { get; set; }
        [StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), Display(Name = "Đường dẫn Youtube"),
        Url(ErrorMessage = "Đường dẫn không chính xác"), UIHint("TextBox")]
        public string Youtube { get; set; }
        [Display(Name = "Đường dẫn messenger"), StringLength(200, ErrorMessage = "Tối đa 200 ký tự"), UIHint("TextBox")]
        public string UrlMessenger { get; set; }
        [StringLength(4000, ErrorMessage = "Tối đa 4000 ký tự"), Display(Name = "Mã nhúng Live chat"),
        UIHint("TextArea")]
        public string LiveChat { get; set; }
        [Display(Name = "Thẻ tiêu đề"),Required(ErrorMessage = "Hãy nhập mục này"), UIHint("TextBox"), StringLength(200)]
        public string Title { get; set; }
        [Display(Name = "Thẻ mô tả"), StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), UIHint("TextArea")]
        public string Description { get; set; }
        [Display(Name = "Số năm hoạt động"), Required(ErrorMessage = "Hãy nhập số năm hoạt động"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int Years { get; set; }
        [Display(Name = "Số khách hàng sử dụng"), Required(ErrorMessage = "Hãy nhập số khách hàng"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int Customers { get; set; }
        [Display(Name = "Số chi nhánh"), Required(ErrorMessage = "Hãy nhập số chi nhánh"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int Offices { get; set; }
        [Display(Name = "Slogan"), Required(ErrorMessage = "Hãy nhập mục này"), UIHint("TextBox"), StringLength(200)]
        public string Slogan { get; set; }
        [Display(Name = "Thông tin bài giới thiệu"), UIHint("EditorBox")]
        public string AboutText { get; set; }
        [Display(Name = "Bài viết giới thiệu"), UIHint("EditorBox")]
        public string AboutBody { get; set; }
        [Display(Name = "Thông tin chân trang"), UIHint("EditorBox")]
        public string AboutFooter { get; set; }
        [Display(Name = "Logo"), StringLength(500)]
        public string Image { get; set; }
        [Display(Name = "Favicon"), StringLength(500)]
        public string Favicon { get; set; }
        [StringLength(4000, ErrorMessage = "Tối đa 4000 ký tự"), Display(Name = "Mã Google Map"), UIHint("TextArea")]
        public string GoogleMap { get; set; }
        [Display(Name = "Địa chỉ"), UIHint("TextBox"), StringLength(1000)]
        public string Place { get; set; }
        [Display(Name = "Hotline"), Required(ErrorMessage = "Hãy nhập số điện thoại"), StringLength(20, ErrorMessage = "Tối đa 20 ký tự"), UIHint("TextBox")]
        public string Hotline { get; set; }
        [StringLength(50, ErrorMessage = "Tối đa 50 ký tự"), Required(ErrorMessage = "Hãy nhập Email"), Display(Name = "Email"),
         EmailAddress(ErrorMessage = "Email không chính xác"), UIHint("TextBox")]
        public string Email { get; set; }
        [DisplayName("Mật khẩu mặc định"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự")]
        public string Password { get; set; }
        [DisplayName("Chạy tự động Nhân sự")]
        public bool AutoUser { get; set; }
        [DisplayName("Chạy tự động Doanh thu")]
        public bool AutoRevenue { get; set; }
        [DisplayName("Chạy tự động Công nợ")]
        public bool AutoDebt { get; set; }

    }
}
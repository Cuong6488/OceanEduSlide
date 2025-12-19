using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Banner
    {
        public int Id { get; set; }
        [Display(Name = "Tên banner"), Required(ErrorMessage = "Hãy nhập tên banner"), StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        public string Name { get; set; }
        [Display(Name = "Hình ảnh"), StringLength(500), UIHint("ImageBanner")]
        public string Image { get; set; }
        [Display(Name = "Hình ảnh Mobile (Banner chính)"), StringLength(500), UIHint("ImageBanner")]
        public string ImageMobile { get; set; }
        [Display(Name = "Vị trí quảng cáo"), Required(ErrorMessage = "Hãy chọn vị trí quảng cáo"), UIHint("GroupId")]
        public int GroupId { get; set; }
        [Display(Name = "Slogan"),
         StringLength(100, ErrorMessage = "Tối đa 100 ký tự"), UIHint("TextBox")]
        public string Slogan { get; set; }
        [Display(Name = "Trích dẫn ngắn"),
         StringLength(300, ErrorMessage = "Tối đa 300 ký tự"), UIHint("TextArea")]
        public string Description { get; set; }
        [Display(Name = "Đường dẫn"), StringLength(500, ErrorMessage = "Tối đa 500 ký tự"), UIHint("TextBox")]
        public string Url { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Thứ tự"), Required(ErrorMessage = "Hãy nhập thứ tự"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên"), UIHint("NumberBox")]
        public int Sort { get; set; }
    }
}
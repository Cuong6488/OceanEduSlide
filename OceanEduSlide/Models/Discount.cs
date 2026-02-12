using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Discount
    {
        public int Id { get; set; }
        [Display(Name = "Tên QĐ ưu đãi"), Required(ErrorMessage = "Hãy điền tên QĐ"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        //[Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy chọn chi nhánh")]
        //public int OfficeId { get; set; }
        [Display(Name = "Các chi nhánh áp dụng"), Required(ErrorMessage = "Hãy nhập các chi nhánh"), UIHint("TextBox")]
        public string Offices { get; set; }
        [Display(Name = "% ưu đãi"), RegularExpression(@"^(?!0(\.0+)?$)\d+(\.\d+)?$", ErrorMessage = "Nhập số dương"), UIHint("NumberBox")]
        public double? PercentDiscount { get; set; }
        [Display(Name = "Ưu đãi tiền mặt"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public int? MoneyDiscount { get; set; }
        [Display(Name = "Số tháng từ"), Required(ErrorMessage = "Hãy nhập số tháng"), UIHint("NumberBox")]
        public int Pathway { get; set; }
        [Display(Name = "Số tháng đến"), Required(ErrorMessage = "Hãy nhập số tháng"), UIHint("NumberBox")]
        public int PathwayTo { get; set; }
        [Display(Name = "Ngày hết hạn")]
        public DateTime? EndDate { get; set; }
        [Display(Name = "Ngày hiệu lực")]
        public DateTime? StartDate { get; set; }
        [Display(Name = "Quà tặng"), UIHint("Textbox")]
        public string Gift { get; set; }
        [Display(Name = "Phân loại"), UIHint("Textbox")]
        public string PhanLoai { get; set; }
        [Display(Name = "Chương trình học")]
        public string Cth { get; set; }
        public virtual Office Office { get; set; }
        public Discount()
        {
            Active = true;
        }
    }
}

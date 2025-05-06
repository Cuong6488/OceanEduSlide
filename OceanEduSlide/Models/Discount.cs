using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy nhập các chi nhánh")]
        public string Offices { get; set; }
        [Display(Name = "% ưu đãi")]
        public double? PercentDiscount { get; set; }
        [Display(Name = "Ưu đãi tiền mặt"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public int? MoneyDiscount { get; set; }
        [Display(Name = "Số tháng từ"), Required(ErrorMessage = "Hãy nhập số tháng")]
        public int Pathway { get; set; }
        [Display(Name = "Số tháng đến"), Required(ErrorMessage = "Hãy nhập số tháng")]
        public int PathwayTo { get; set; }
        [Display(Name = "Quà tặng"), UIHint("Textbox")]
        public string Gift { get; set; }
        [Display(Name = "Chương trình học"), UIHint("Textbox")]
        public string Cth { get; set; }
        public virtual Office Office { get; set; }
        public Discount()
        {
            Active = true;
        }
    }
}

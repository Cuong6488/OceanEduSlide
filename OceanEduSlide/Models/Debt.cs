using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class Debt
    {
        public int Id { get; set; }
        [Display(Name = "Ngày phát sinh cọc"), Required(ErrorMessage = "Hãy chọn ngày")]
        public string DepositDate { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Nhân sự"), Required(ErrorMessage = "Hãy chọn nhân sự")]
        public int UserId { get; set; }
        [Display(Name = "Họ tên học viên")]
        public string StudentName { get; set; }
        [Display(Name = "Mã học viên")]
        public string StudentCode { get; set; }
        [Display(Name = "Chương trình học")]
        public string Cth { get; set; }
        [Display(Name = "Tên QĐ ưu đãi")]
        public string DiscountName { get; set; }
        [Display(Name = "Lộ trình"), DisplayFormat(DataFormatString = "{0:N1}")]
        public decimal Pathway { get; set; }
        [Display(Name = "Thành tiền"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal TotalMoney { get; set; }
        [Display(Name = "Tiền cọc giữ chỗ"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DebtMoney { get; set; }
        [Display(Name = "Tiền giảm lộ trình"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DownMoney { get; set; }
        [Display(Name = "Tiền cọc bổ sung làm hồ sơ"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DebtMoney2 { get; set; }
        [Display(Name = "Tiền còn lại phải thanh toán"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal RemainMoney { get; set; }
        [Display(Name = "Tình trạng khách hàng")]
        public TypeDebt? TypeDebt { get; set; }
        [Display(Name = "Kênh trả góp")]
        public ChannelPay? ChannelPay { get; set; }
        [Display(Name = "Hình thức thanh toán")]
        public TypePay? TypePay { get; set; }
        [Display(Name = "Tình trạng hồ sơ")]
        public string FileStatus { get; set; }
        [Display(Name = "Ngày phát sinh gộp phí")]
        public string GrossDate { get; set; }
        [Display(Name = "Nội dung khó khăn"),UIHint("TextArea")]
        public string HardContent { get; set; }
        [Display(Name = "Tình trạng liên hệ khách"), UIHint("TextArea")]
        public string ContactStatus { get; set; }
        [Display(Name = "Hướng xử lý"), UIHint("TextArea")]
        public string HandleWay { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}"),Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<DownPathway> DownPathways { get; set; }
        public Debt()
        {
            CreateDate = DateTime.Now;
            Active = true;
        }
    }
    public enum TypeDebt
    {
        [Display(Name = "Tiềm năng trong tháng")]
        Type1 = 1,
        [Display(Name = "Tiềm năng bám")]
        Type2,
        [Display(Name = "Tiềm năng giảm lộ trình")]
        Type3,
        [Display(Name = "Bỏ cọc")]
        Type4,
        [Display(Name = "Hoàn cọc")]
        Type5,
    }
    public enum TypePay
    {
        [Display(Name = "Đổi sang trả thẳng")]
        Paynow,
        [Display(Name = "Trả góp không thẻ")]
        NoCard,
        [Display(Name = "Trả góp có thẻ")]
        Card,
    }
    public enum ChannelPay
    {
        [Display(Name = "Lotte")]
        Lotte,
        [Display(Name = "VP Bank")]
        VPBank,
        [Display(Name = "MSB")]
        MSB,
        [Display(Name = "Sacombank")]
        Sacombank,
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Debt
    {
        public int Id { get; set; }
        [Display(Name = "Ngày phát sinh cọc"), Required(ErrorMessage = "Hãy chọn ngày")]
        public string DepositDate { get; set; }
        [Display(Name = "Ngày phát sinh cọc")]
        public DateTime? NgayPhatSinhCoc { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), Display(Name = "Ngày lên đơn")]
        public DateTime? NgayLenDon { get; set; }
        [Display(Name = "Mã đơn hàng"), UIHint("TextBox")]
        public string MaDonHang { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Nhân sự phụ trách"), Required(ErrorMessage = "Hãy chọn nhân sự")]
        public int UserId { get; set; }
        [Display(Name = "Nhân sự phát sinh")]
        public int? UserOriginId { get; set; }
        [Display(Name = "Chi nhánh")]
        public int? OfficeId { get; set; }
        [Display(Name = "Công nợ gốc")]
        public int? DebtId { get; set; }
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
        [Display(Name = "Tiền cọc"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        //[Display(Name = "Tiền đã thanh toán"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DebtMoney { get; set; }
        [Display(Name = "Tiền giảm lộ trình"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DownMoney { get; set; }
        [Display(Name = "Bổ sung phí"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal DebtMoney2 { get; set; }
        [Display(Name = "Tiền còn lại"), DisplayFormat(DataFormatString = "{0:N0}đ")]
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
        [Display(Name = "Nội dung khó khăn"), UIHint("TextArea")]
        public string HardContent { get; set; }
        [Display(Name = "Tình trạng liên hệ khách"), UIHint("TextArea")]
        public string ContactStatus { get; set; }
        [Display(Name = "Hướng xử lý"), UIHint("TextArea")]
        public string HandleWay { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}"), Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        [Display(Name = "Công nợ cần thu (từ các tháng cũ)")]
        public bool PhaiThu { get; set; }
        [Display(Name = "Cập nhật tự động")]
        public bool Auto { get; set; }
        [Display(Name = "BM đã sửa")]
        public bool EditUser { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        public TypeData TypeData { get; set; }
        public virtual User User { get; set; }
        public virtual User UserOrigin { get; set; }
        public virtual Office Office { get; set; }
        public virtual ICollection<DownPathway> DownPathways { get; set; }
        public virtual ICollection<Debt> Debts { get; set; }
        public virtual Debt DebtParent { get; set; }

        public Debt()
        {
            CreateDate = DateTime.Now;
            Active = true;
        }
    }
    public enum TypeData
    {
        [Display(Name = "Dữ liệu cũ")]
        Old,
        [Display(Name = "Dữ liệu mới")]
        New,
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
        [Display(Name = "Công nợ hoàn thành")]
        Type6,
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
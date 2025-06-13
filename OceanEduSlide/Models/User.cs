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
        [Display(Name = "Mã nhân viên", Description = "Mã nhân viên"), UIHint("TextBox")]
        public string MaNhanVien { get; set; }
        [Display(Name = "Họ và tên", Description = "Họ và tên"), UIHint("TextBox")]
        public string Fullname { get; set; }
        [DisplayName("Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu"), StringLength(60, ErrorMessage = "Tối đa 60 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 1 (%)"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal Confirm1 { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 2 (%)"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal Confirm2 { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 3 (%)"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal Confirm3 { get; set; }
        [DisplayName("Tỉ lệ số lượng khách hàng chuyển đổi check in (%)"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal CI { get; set; }
        [DisplayName("Tỉ lệ số lượng khách hàng chuyển đổi ra cọc/ doanh thu (%)"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal DT { get; set; }
        [DisplayName("Doanh thu trung bình trên một khách hàng"), DisplayFormat(DataFormatString = "{0:N0}đ")]
        public decimal RevenueAverage { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "Vùng")]
        public int? ZoneId { get; set; }
        [Display(Name = "Phân quyền")]
        public TypeUser? TypeUser { get; set; }
        [Display(Name = "Các chi nhánh quản lý")]
        public string OfficeIds { get; set; }
        public virtual Office Office { get; set; }
        public virtual Zone Zone { get; set; }
        public virtual ICollection<RevenueUser_Month> RevenueUser_Months { get; set; }
        public virtual ICollection<RevenueUser_Month_BM> GetRevenueUser_Month_BMs { get; set; }
        public virtual ICollection<RevenueUser_Week> RevenueUser_Weeks { get; set; }
        public virtual ICollection<RevenueUser_Week_Real> RevenueUser_Week_Reals { get; set; }
        public virtual ICollection<RevenueUser_DayOfWeek> RevenueUser_DayOfWeeks { get; set; }
        public virtual ICollection<Proposal> Proposals { get; set; }
        public User()
        {
            Active = true;
        }
    }
    public enum TypeUser
    {
        [Display(Name = "User")]
        User,
        [Display(Name = "HO")]
        HO,
        [Display(Name = "ASM")]
        ASM,
        [Display(Name = "BM")]
        BM,
        [Display(Name = "EC")]
        EC,
        [Display(Name = "SL")]
        SL,
        [Display(Name = "SAB")]
        SAB,
        [Display(Name = "CM")]
        CM,
        [Display(Name = "Chuyên viên")]
        CV,

    }
}
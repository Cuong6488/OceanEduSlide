using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using OceanEduSlide.Models;

namespace OceanEduSlide.ViewModels
{
    public class InfoAdminViewModel
    {
        //public IEnumerable<Article> Articles { get; set; }
        public IEnumerable<Admin> Admins { get; set; }
        //public IEnumerable<Banner> Banners { get; set; }
        //public IEnumerable<Product> Products { get; set; }
        //public IEnumerable<Person> People { get; set; }
        //public IEnumerable<ContactOffice> Contacts { get; set; }
        //public IEnumerable<Store> Stores { get; set; }
    }
    public class LoginAdminViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy nhập tên đăng nhập")]
        public string Username { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu")]
        public string Password { get; set; }
    }
    public class ChangePassWordViewModel
    {
        [Display(Name = "Mật khẩu hiện tại"), Required(ErrorMessage = "Hãy nhập mật khẩu hiện tại"), UIHint("Password")]
        public string OldPassword { get; set; }
        [Display(Name = "Mật khẩu mới"), Required(ErrorMessage = "Hãy nhập mật khẩu mới"),
         StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }
    }
    public class CreateAdminViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"),
            RegularExpression(@"[a-z0-9]{4,10}", ErrorMessage = "Chỉ nhập chữ thường và số 0-9, từ 4-10 ký tự"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu mới"),
         StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Hoạt động", Description = "Hoạt động")]
        public bool Active { get; set; }
        public RoleAdmin RoleAdmin { get; set; }
        public CreateAdminViewModel()
        {
            Active = true;
        }
        public IEnumerable<Admin> Admins { get; set; }
    }
    public class CreateUserViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Họ và tên"), UIHint("TextBox")]
        public string Fullname { get; set; }
        [Display(Name = "Mã nhân viên"), UIHint("TextBox"), Required(ErrorMessage = "Hãy nhập Mã nhân viên")]
        public string MaNhanVien { get; set; }
        [Display(Name = "Mật khẩu"), Required(ErrorMessage = "Hãy nhập mật khẩu mới"),
         StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Hoạt động", Description = "Hoạt động")]
        public bool Active { get; set; }

        [Display(Name = "Tài khoản SaleKit", Description = "Hoạt động")]
        public bool SaleKit { get; set; }
        [Display(Name = "Chi nhánh")]
        public int? OfficeId { get; set; }
        [Display(Name = "Các chi nhánh")]
        public string OfficeIds { get; set; }
        [Display(Name = "Các vùng quản lý")]
        public string ZoneIds { get; set; }
        [Display(Name = "Các chi nhánh")]
        public string OfficeNames { get; set; }
        [Display(Name = "Chức danh"), UIHint("TextBox"), Required(ErrorMessage = "Hãy nhập CDCM")]
        public string CDCM { get; set; }
        [Display(Name = "Vùng")]
        public int? ZoneId { get; set; }
        [Display(Name = "Phân quyền"), Required(ErrorMessage = "Hãy chọn phân quyền")]
        public TypeUser? TypeUser { get; set; }
        public SelectList SelectOffices { get; set; }
        public SelectList SelectZones { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public List<int> CatIds { get; set; }
        public CreateUserViewModel()
        {
            Active = true;
        }
        public IEnumerable<User> Users { get; set; }
    }
    public class UpdateUserViewModel
    {
        [Display(Name = "Tên đăng nhập"), Required(ErrorMessage = "Hãy điền tên đăng nhập"), UIHint("TextBox")]
        public string Username { get; set; }
        [Display(Name = "Họ và tên"), UIHint("TextBox")]
        public string Fullname { get; set; }
        [Display(Name = "Mã nhân viên"), UIHint("TextBox"), Required(ErrorMessage = "Hãy nhập Mã nhân viên")]
        public string MaNhanVien { get; set; }
        [Display(Name = "Mật khẩu (không bắt buộc)"), StringLength(16, MinimumLength = 4, ErrorMessage = "Mật khẩu từ 4, 16 ký tự"), UIHint("Password")]
        public string Password { get; set; }
        [Display(Name = "Nhập lại mật khẩu"), System.ComponentModel.DataAnnotations.Compare("Password", ErrorMessage = "Nhập lại mật khẩu không chính xác"),
         UIHint("Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Hoạt động", Description = "Hoạt động")]
        public bool Active { get; set; }

        [Display(Name = "Tài khoản SaleKit", Description = "Hoạt động")]
        public bool SaleKit { get; set; }
        [Display(Name = "Chi nhánh")]
        public int? OfficeId { get; set; }
        [Display(Name = "Vùng")]
        public int? ZoneId { get; set; }
        [Display(Name = "Phân quyền"), Required(ErrorMessage = "Hãy chọn phân quyền")]
        public TypeUser? TypeUser { get; set; }
        [Display(Name = "Chức danh"), UIHint("TextBox"), Required(ErrorMessage = "Hãy nhập CDCM")]
        public string CDCM { get; set; }
        public SelectList SelectOffices { get; set; }
        public SelectList SelectZones { get; set; }
        public List<int> CatIds { get; set; }
        public List<string> CatIds2 { get; set; }
        [Display(Name = "Các vùng quản lý")]
        public string ZoneIds { get; set; }
        [Display(Name = "Các chi nhánh")]
        public string OfficeIds { get; set; }
        [Display(Name = "Các chi nhánh")]
        public string OfficeNames { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public UpdateUserViewModel()
        {
            Active = true;
        }
        public IEnumerable<User> Users { get; set; }
    }

    public class CreateTargetViewModel
    {
        public SelectList SelectUsers { get; set; }
        public RevenueUser_Month Revenue { get; set; }
        public IEnumerable<User> Users { get; set; }
    }
    public class ListUserViewModel
    {
        public PagedList.IPagedList<User> Users { get; set; }
        public SelectList SelectOffices { get; set; }
        public SelectList SelectZones { get; set; }
        public int? officeId { get; set; }
        public int? TypeUser { get; set; }
        public int? ZoneId { get; set; }
        public int? active { get; set; }
        public string Username { get; set; }
        public IEnumerable<MemberCredential> MemberCredentials { get; set; }
    }
    public class ListHistoryUserViewModel
    {
        public PagedList.IPagedList<HistoryUser> HistoryUsers { get; set; }
        public SelectList SelectOffices { get; set; }
        public SelectList SelectZones { get; set; }
        public int? officeId { get; set; }
        public int? TypeUser { get; set; }
        public int? ZoneId { get; set; }
        public int? active { get; set; }
        public int? month { get; set; }
        public int? year { get; set; }
        public string Username { get; set; }
    }
    public class ListHistoryOfficeViewModel
    {
        public PagedList.IPagedList<HistoryOffice> HistoryOffices { get; set; }
        public SelectList SelectZones { get; set; }
        public int? Group { get; set; }
        public int? ZoneId { get; set; }
        public int? month { get; set; }
        public int? year { get; set; }
        public string Name { get; set; }
    }

    public class ListDiscountViewModel
    {
        public PagedList.IPagedList<Discount> Discounts { get; set; }
        public string Name { get; set; }
        public string Cth { get; set; }
        public string officeId { get; set; }
        [Display(Name = "Ngày hiệu lực"), UIHint("DateTimePicker")]
        public string StartDate { get; set; }
        [Display(Name = "Ngày hết hạn"), UIHint("DateTimePicker")]
        public string EndDate { get; set; }
        public SelectList SelectOffices { get; set; }
    }
    public class CreateDiscountViewModel
    {
        public Discount Discount { get; set; }
        [Display(Name = "Ưu đãi tiền mặt"), UIHint("MoneyBox")]
        public string MoneyDiscount { get; set; }
        [Display(Name = "Ngày hiệu lực"), UIHint("DateTimePicker")]
        public string StartDate { get; set; }
        [Display(Name = "Ngày hết hạn"), UIHint("DateTimePicker")]
        public string EndDate { get; set; }
        [Display(Name = "Chương trình học"), Required(ErrorMessage = "Hãy chọn Chương trình học")]
        public string Cth { get; set; }
        //public SelectList SelectPathway { get; set; }
        public SelectList SelectCth { get; set; }
        public CreateDiscountViewModel()
        {
            //var listgroup = new Dictionary<int, string>
            //{
            //    { 3, "3 tháng" },
            //    { 6, "6 tháng" },
            //    { 12, "12 tháng" },
            //    { 18, "18 tháng" },
            //    { 24, "24 tháng" },
            //    { 36, "36 tháng" },
            //    { 48, "48 tháng" },
            //    { 72, "72 tháng" },
            //};
            //SelectPathway = new SelectList(listgroup, "Key", "Value");
            SelectCth = new SelectList(new List<string>
            {
                "Anh văn nhi đồng",
                "Anh văn thiếu nhi",
                "T.A học thuật Trung học",
                "Luyện thi IELTS",
                "T.A giao tiếp quốc tế TOEIC"
            });

        }
    }
}
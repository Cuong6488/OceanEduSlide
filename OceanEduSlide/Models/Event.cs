using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class Event
    {
        public int Id { get; set; }
        [Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy chọn chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "Năm"), Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Tháng"), Required(ErrorMessage = "Hãy chọn tháng")]
        public int Month { get; set; }
        [Display(Name = "Tuần")]
        public WeekNumber WeekNumber { get; set; }
        [Display(Name = "Thứ")]
        public DayofWeek DayofWeek { get; set; }
        [Display(Name = "Loại hoạt động"), Required(ErrorMessage = "Hãy chọn loại hoạt động")]
        public TypeEvent TypeEvent { get; set; }
        [Display(Name = "Đối tượng tham gia"), Required(ErrorMessage = "Hãy chọn đối tượng tham gia")]
        public TypeJoin TypeJoin { get; set; }
        [Display(Name = "Lứa tuổi"), Required(ErrorMessage = "Hãy nhập lừa tuổi"), StringLength(15, ErrorMessage = "Tối đa 15 ký tự"), UIHint("TextBox")]
        public string Ages { get; set; }
        [Display(Name = "Quy mô số lượng dự kiến"), Required(ErrorMessage = "Hãy nhập quy mô số lượng dự kiến"), UIHint("NumberBox")]
        public int Range { get; set; }
        [Display(Name = "Thời gian từ"), Required(ErrorMessage = "Hãy chọn mục này")]
        public string TimeFrom { get; set; }
        [Display(Name = "Thời gian đến"), Required(ErrorMessage = "Hãy chọn mục này")]
        public string TimeTo { get; set; }
        [Display(Name = "Tên đường dẫn")]
        public string LinkName { get; set; }
        [Display(Name = "Đường dẫn")]
        public string LinkUrl { get; set; }

        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual Office Office { get; set; }
        public Event()
        {
            Active = true;
            CreateDate = DateTime.Now;
        }
    }

    public enum TypeEvent
    {
        [Display(Name = "Hoạt động của Đào tạo")]
        HDDT,
        [Display(Name = "Sự kiện của Đào tạo")]
        SKDT,
        [Display(Name = "Hoạt động của sale")]
        HDSale,
        [Display(Name = "Sự kiện của sale")]
        SKSale,
    }
    public enum TypeJoin
    {
        [Display(Name = "Học viên")]
        HV,
        [Display(Name = "Khách hàng mới")]
        KH,
        [Display(Name = "Học viên & khách hàng mới")]
        HV_KH,
    }
}
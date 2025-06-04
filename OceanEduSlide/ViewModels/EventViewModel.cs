using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class EventViewModel
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Week { get; set; }
        public int? OfficeId { get; set; }
        public IEnumerable<UserItem> UserItems { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Event> Events { get; set; }
        public IEnumerable<User> Users { get; set; }
        public User User { get; set; }
        public class UserItem
        {
            public User User { get; set; }
            public IEnumerable<RevenueUser_DayOfWeek> Revenues { get; set; }

        }

    }
    public class UpdatePercentViewModel
    {
        public int UserId { get; set; }
        public string Fullname { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 1 (%)"), Required(ErrorMessage = "Hãy nhập tỉ lệ"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal CF1 { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 2 (%)"), Required(ErrorMessage = "Hãy nhập tỉ lệ"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal CF2 { get; set; }
        [DisplayName("Tỉ lệ số lượng data confirm lần 3 (%)"), Required(ErrorMessage = "Hãy nhập tỉ lệ"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal CF3 { get; set; }
        [DisplayName("Tỉ lệ số lượng khách hàng chuyển đổi check in (%)"), Required(ErrorMessage = "Hãy nhập tỉ lệ"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal CI { get; set; }
        [DisplayName("Tỉ lệ số lượng khách hàng chuyển đổi ra cọc/ doanh thu (%)"), Required(ErrorMessage = "Hãy nhập tỉ lệ"), Range(0.0, 100.0, ErrorMessage = "Giá trị phải nằm trong khoảng từ 0 đến 100.")]
        public decimal DT { get; set; }
    }
    public class LoadHistoryRevenueUser_DayViewModel
    {
        public User User { get; set; }
        public IEnumerable<RevenueUser_DayOfWeek> Revenues { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public WeekNumber WeekNumber { get; set; }
        public DayofWeek DayofWeek { get; set; }
    }
}

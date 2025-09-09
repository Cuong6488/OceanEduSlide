using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class WorkingDay
    {
        public int Id { get; set; }
        [Display(Name = "Năm"),Required(ErrorMessage = "Hãy chọn năm")]
        public int Year { get; set; }
        [Display(Name = "Số ngày công tháng 1"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth1 { get; set; }
        [Display(Name = "Số ngày công tháng 2"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth2 { get; set; }
        [Display(Name = "Số ngày công tháng 3"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth3 { get; set; }
        [Display(Name = "Số ngày công tháng 4"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth4 { get; set; }
        [Display(Name = "Số ngày công tháng 5"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth5 { get; set; }
        [Display(Name = "Số ngày công tháng 6"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth6 { get; set; }
        [Display(Name = "Số ngày công tháng 7"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth7 { get; set; }
        [Display(Name = "Số ngày công tháng 8"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth8 { get; set; }
        [Display(Name = "Số ngày công tháng 9"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth9 { get; set; }
        [Display(Name = "Số ngày công tháng 10"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth10 { get; set; }
        [Display(Name = "Số ngày công tháng 11"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth11 { get; set; }
        [Display(Name = "Số ngày công tháng 12"), Required(ErrorMessage = "Hãy nhập số ngày công"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int WorkingDayMonth12 { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
    }
}
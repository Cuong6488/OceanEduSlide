using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class HistoryUser
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OfficeId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public TypeUser TypeUser { get; set; }
        public StatusUser Status { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày vào làm")]
        public DateTime DayStart { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày nghỉ/ điều chuyển")]
        public DateTime? DayEnd { get; set; }
        public WeekNumber? WeekNumber { get; set; }
        public int Sort { get; set; } = 1;
        public bool Active { get; set; } = true;
        public virtual User User { get; set; }
        public virtual Office Office { get; set; }
    }
    public enum StatusUser
    {
        [Display(Name = "Đang làm việc")]
        Active,
        [Display(Name = "Điều chuyển")]
        Transfer,
        [Display(Name = "Nghỉ việc/ TS")]
        InActive
    }
}
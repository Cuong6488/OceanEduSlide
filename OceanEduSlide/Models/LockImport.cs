using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class LockImport
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public TypeLock TypeLock { get; set; }
        public bool Active { get; set; } = true;
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày khóa")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
    }
    public enum TypeLock
    {
        [Display(Name = "User tháng")]
        HistoryUser,
        [Display(Name = "Báo cáo")]
        ReportData,
    }
}
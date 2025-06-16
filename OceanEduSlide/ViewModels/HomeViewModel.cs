using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OceanEduSlide.ViewModels
{

    public class UserHomeViewModel
    {
        public IEnumerable<Debt> Debts { get; set; }
        public IEnumerable<RevenueUser_DayOfWeek> Revenues { get; set; }
        public User User { get; set; }
        public decimal TMonth { get; set; }
        public decimal RevenueMonthNow { get; set; }
        public decimal TargetMonthPercent { get; set; }
        public decimal RemainPercent { get; set; }
        public decimal TWeek { get; set; }
        public decimal RevenueWeekNow { get; set; }
        public decimal TargetWeekPercent { get; set; }
        public UserHomeViewModel()
        {
            TargetMonthPercent = RevenueMonthNow / TMonth * 100;
            RemainPercent = 100 - TargetMonthPercent;
            TargetWeekPercent = RevenueWeekNow / TWeek * 100;
        }
    }
    
}
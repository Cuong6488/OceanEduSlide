using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.ViewModels
{

    public class ListReportHomeViewModel
    {
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<ReportCategory> ReportCategories { get; set; }
        public IEnumerable<ReportData> ReportDatas { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        //public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public User User { get; set; }
    }
}
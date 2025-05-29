using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class RevenueViewModel
    {
        public int? Month { get; set; }
        public int? Year { get; set; }
        public int? OfficeId { get; set; }
        public SelectList SelectOffices { get; set; }
        public IEnumerable<User> Users { get; set; }
        public RevenueOffice RevenueOffice { get; set; }
        public IEnumerable<RevenueOffice> RevenueOffice_BM { get; set; }
        public RevenueUser_Month RevenueUser_Month { get; set; }
        public IEnumerable<RevenueUser_Month_BM> RevenueUser_Month_BMs { get; set; }
        public IEnumerable<RevenueUser_Week> RevenueUser_Weeks { get; set; }
    }
    public class RevenueOfficeViewModel
    {
        public RevenueOffice RevenueOffice { get; set; }
        public SelectList SelectOffices { get; set; }
    }
}

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
        public IEnumerable<UserItem> UserItems { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public RevenueOffice RevenueOffice { get; set; }
        public IEnumerable<RevenueOffice_BM> RevenueOffice_BMs { get; set; }
        public User User { get; set; }
        public class UserItem
        {
            public User User { get; set; }
            public RevenueUser_Month RevenueUser_Month { get; set; }
            public IEnumerable<RevenueUser_Month_BM> RevenueUser_Month_BMs { get; set; }
            public IEnumerable<RevenueUser_Week> RevenueUser_Weeks { get; set; }

        }

    }
    public class RevenueOfficeViewModel
    {
        public RevenueOffice RevenueOffice { get; set; }
        public SelectList SelectOffices { get; set; }
    }
    public class ListRevenueOfficeViewModel
    {
        public PagedList.IPagedList<RevenueOffice> RevenueOffices { get; set; }
        public SelectList SelectOffices { get; set; }
        public int? OfficeId { get; set; }
    }
}

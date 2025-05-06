using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{

    public class ListOfficeViewModel
    {
        public PagedList.IPagedList<Office> Offices { get; set; }
        public SelectList SelectCities { get; set; }
        public int? cityId { get; set; }
        public string Name { get; set; }
    }
    public class InsertOfficeViewModel
    {
        public Office Office { get; set; }
        //public SelectList SelectCities { get; set; }
        //public SelectList DistrictSelectList { get; set; }
        //public InsertOfficeViewModel()
        //{
        //    DistrictSelectList = new SelectList(new List<District>(), "Id", "Name");
        //}
    }

    public class ListDiscountViewModel
    {
        public PagedList.IPagedList<Discount> Discounts { get; set; }
        public string Name { get; set; }
    }
}
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
    public class CreateZoneViewModel
    {
        public List<int> CatIds { get; set; }
        public Zone Zone { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        //}
    }

    public class ListDiscountViewModel
    {
        public PagedList.IPagedList<Discount> Discounts { get; set; }
        public string Name { get; set; }
        public string Cth { get; set; }
        public string officeId { get; set; }
        public SelectList SelectOffices { get; set; }
    }
    public class ListGroupDiscountViewModel
    {
        public PagedList.IPagedList<GroupDiscount> GroupDiscounts { get; set; }
        public string Name { get; set; }
        public int? Year { get; set; }
    }
}
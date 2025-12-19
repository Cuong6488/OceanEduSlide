using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OceanEduSlide.Models;

namespace OceanEduSlide.ViewModels
{
    public class BannerViewModel
    {
        public Banner Banner { get; set; }
        public SelectList SelectGroup { get; set; }
        public BannerViewModel()
        {
            var listgroup = new Dictionary<int, string>
            {
                { 1, "Banner chính (1900 x 600)" },
            };
            SelectGroup = new SelectList(listgroup, "Key", "Value");
        }
    }
    public class ListBannerViewModel
    {
        public PagedList.IPagedList<Banner> Banners { get; set; }
        public int? GroupId { get; set; }
        public SelectList SelectGroup { get; set; }
        public ListBannerViewModel()
        {
            var listgroup = new Dictionary<int, string>
            {
                { 1, "Banner chính (1900 x 600)" },
            };
            SelectGroup = new SelectList(listgroup, "Key", "Value");
        }
    }
}
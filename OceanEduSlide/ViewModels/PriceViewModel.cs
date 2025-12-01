using OceanEduSlide.Models;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class PriceViewModel
    {
        public Office Office { get; set; }

        public SelectList SelectDiscounts { get; set; }
        public int DiscountId { get; set; }
    }
}

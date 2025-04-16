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
    public class PriceViewModel
    {
        public Office Office { get; set; }

        public SelectList SelectDiscounts { get; set; }
        public int DiscountId { get; set; }
    }
}

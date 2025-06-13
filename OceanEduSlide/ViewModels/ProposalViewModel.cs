using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class ProposeViewModel
    {
        public SelectList SelectOffices { get; set; }
        public Proposal Proposal { get; set; }

    }
    public class ProposalViewModel
    {
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? OfficeId { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Proposal> Proposals { get; set; }
        public User User { get; set; }
    }
}
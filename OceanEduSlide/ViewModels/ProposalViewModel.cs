using OceanEduSlide.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OceanEduSlide.ViewModels
{
    public class ProposeViewModel
    {
        public SelectList SelectOffices { get; set; }
        public SelectList SelectUsers { get; set; }
        public SelectList SelectProposalTypes { get; set; }
        public Proposal Proposal { get; set; }

    }
    public class ProposalViewModel
    {
        public int Page { get; set; }
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? Notice { get; set; }
        public string MaDeXuat { get; set; }
        public IEnumerable<Office> Offices { get; set; }
        public IEnumerable<Zone> Zones { get; set; }
        public IEnumerable<Proposal> Proposals { get; set; }
        public IEnumerable<ProposalItem> ProposalItems { get; set; }
        public User User { get; set; }
        public List<string> ProposalTypes { get; set; }
        public List<string> Faults { get; set; }
        public string Type { get; set; }
        public string Fault { get; set; }

        [Display(Name = "Từ ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string StartDay { get; set; }
        [Display(Name = " Đến ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string EndDay { get; set; }
        public class ProposalItem
        {
            public List<string> ListCVPhuTrach { get; set; }
            public Proposal Proposal { get; set; }
        }
    }
    public class ApproveViewModel
    {
        public Proposal Proposal { get; set; }

        [UIHint("MoneyBox"), DisplayFormat(DataFormatString = "{0:N0}")]
        public string Number { get; set; }
        public SelectList SelectFault { get; set; }
        public int Page { get; set; }
        public int? OfficeId { get; set; }
        public int? ZoneId { get; set; }
        public int? Notice { get; set; }
        public string MaDeXuat { get; set; }
        public string Type { get; set; }
        public string Fault { get; set; }

        [Display(Name = "Từ ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string StartDay { get; set; }
        [Display(Name = " Đến ngày "), DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), UIHint("DateTimePicker")]
        public string EndDay { get; set; }

    }
}
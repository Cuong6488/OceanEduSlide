using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class HistoryOffice
    {
        public int Id { get; set; }
        public int OfficeId { get; set; }
        public int ZoneId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int DBATL { get; set; }
        public int DBEC { get; set; }
        public int? NVKDOver { get; set; }
        public decimal? TargetReduce { get; set; }
        public decimal BaseTarget { get; set; }
        public StatusOffice Status { get; set; } = StatusOffice.Active;
        public GroupOffice GroupOffice { get; set; }
        public int Sort { get; set; } = 1;
        public bool Active { get; set; } = true;
        public bool QD156 { get; set; }
        public virtual Office Office { get; set; }
        public virtual Zone Zone { get; set; }
    }
    public enum StatusOffice
    {
        [Display(Name = "Đang hoạt động")]
        Active,
        [Display(Name = "Đóng cửa")]
        InActive
    }
    public enum GroupOffice
    {
        [Display(Name = "Nhóm A")]
        A,
        [Display(Name = "Nhóm B")]
        B,
        [Display(Name = "Nhóm C")]
        C,
        [Display(Name = "Nhóm D")]
        D,
        [Display(Name = "Nhóm E")]
        E,
    }
}
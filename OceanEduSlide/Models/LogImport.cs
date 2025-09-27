using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class LogImport
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [Display(Name = "File")]
        public string File { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")] [Display(Name = "Ngày import")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public TypeImport TypeImport { get; set; }
        [Display(Name = "Người import")]
        public string Admin { get; set; }
    }
    public enum TypeImport
    {
        [Display(Name = "File báo cáo TH CN - NV")]
        Type1,
        [Display(Name = "File chỉ tiêu CN - NV")]
        Type2,
        [Display(Name = "File Vùng")]
        Type3,
        [Display(Name = "File Chi nhánh")]
        Type4,
        [Display(Name = "File Tài khoản nhân sự")]
        Type5,
        [Display(Name = "File Nhân sự theo tháng")]
        Type6,
        [Display(Name = "File QĐ ưu đãi")]
        Type7,
        [Display(Name = "Quy định chung/ QĐ PTS")]
        Type8,
        [Display(Name = "File Chi nhánh theo tháng")]
        Type9,
        [Display(Name = "File Phiếu thu đặc biệt")]
        Type10,
        [Display(Name = "File chỉ tiêu CN - NV; Nhân sự theo tháng")]
        Type11,
        [Display(Name = "File chỉ tiêu CN - NV; Chi nhánh theo tháng")]
        Type12,
        [Display(Name = "File chỉ tiêu CN - NV; CN tháng; NS tháng")]
        Type13,

    }
}
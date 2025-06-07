using OceanEduSlide.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;
namespace OceanEduSlide.Models
{
    public class DownPathway
    {
        public int Id { get; set; }
        [Display(Name = "Công nợ"), Required]
        public int DebtId { get; set; }
        [Display(Name = "Lộ trình giảm còn"), Required(ErrorMessage = "Hãy nhập lộ trình")]
        public decimal Pathway { get; set; }
        [Display(Name = "Số tiền giảm sau khi điều chỉnh lộ trình"), DisplayFormat(DataFormatString = "{0:N0}đ"), Required(ErrorMessage = "Hãy nhập số tiền")]
        public decimal Money { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual Debt Debt { get; set; }
        public DownPathway()
        {
            CreateDate = DateTime.Now;
            Active = true;
        }
    }
}
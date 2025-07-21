using OceanEduSlide.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class ReportCategory
    {
        public int Id { get; set; }
        [Display(Name = "Tên cột"), Required(ErrorMessage = "Hãy nhập tên cột"), StringLength(50, ErrorMessage = "Tối đa 50 ký tự"), UIHint("TextBox")]
        public string Name { get; set; }
        [Display(Name = "Thứ tự"), Required(ErrorMessage = "Hãy nhập số thứ tự"), RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int Sort { get; set; }
        [Display(Name = "Nhóm"), Required, RegularExpression(@"\d+", ErrorMessage = "Chỉ nhập số nguyên dương"), UIHint("NumberBox")]
        public int Group { get; set; }
        [Display(Name = "Số cột con")]
        public int? Count { get; set; }
        [Display(Name = "Hoạt động")]
        public bool Active { get; set; }
        [Display(Name = "Cột cha")]
        public int? ReportCategoryId { get; set; }
        [Display(Name = "Loại cột")]
        public TypeCat TypeCat { get; set; }
        public virtual ICollection<ReportData> ReportDatas { get; set; }
        public virtual ReportCategory CategoryParent { get; set; }
        public virtual ICollection<ReportCategory> ReportCategories { get; set; }

        public ReportCategory()
        {
            Active = true;
        }
    }
    public enum TypeCat
    {
        [Display(Name = "Báo cáo tĩnh 1")]
        Type1 = 1,
        [Display(Name = "Báo cáo tĩnh 2")]
        Type2,
    }
}
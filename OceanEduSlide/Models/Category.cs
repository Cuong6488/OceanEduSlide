using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Display(Name = "Mục lục")]
        public string Index { get; set; }

        [Display(Name = "Nội dung")]
        public string Content { get; set; }

        [Display(Name = "Quy định")]
        public string Regulation { get; set; }

        [Display(Name = "Link đề xuất")]
        public string ProposalLink { get; set; }

        [Display(Name = "Link theo dõi")]
        public string FollowLink { get; set; }

        [Display(Name = "Bảng tính/ Ghi chú")]
        public string Note { get; set; }

        [Display(Name = "Link QĐ")]
        public string QDLink { get; set; }

        [Display(Name = "Số QĐ")]
        public string QDNumber { get; set; }

        [Display(Name = "Nhóm/ Vùng")]
        public string Zone { get; set; }

        [Display(Name = "Tháng")]
        public int? Month { get; set; }
        [Display(Name = "Loại danh mục"),Required]
        public TypeCategory TypeCategory { get; set; }
    }

    public enum TypeCategory
    {
        [Display(Name = "Quy định chung - Đề xuất - Báo cáo line Tuyển sinh")]
        Type1 = 1,
        [Display(Name = "Danh mục Quyết định PTS - Cả năm")]
        Type2,
        [Display(Name = "Danh mục Quyết định PTS - Hàng tháng")]
        Type3,
    }
}
using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class Proposal
    {
        public int Id { get; set; }
        [Display(Name = "Nhân sự đề xuất"), Required]
        public int UserId { get; set; }
        [Display(Name = "Nhân sự theo dõi")]
        public int? UserId2 { get; set; }
        [Display(Name = "Chi nhánh"), Required(ErrorMessage = "Hãy chọn chi nhánh")]
        public int OfficeId { get; set; }
        [Display(Name = "Vùng"), Required(ErrorMessage = "Hãy chọn vùng")]
        public int ZoneId { get; set; }
        [Display(Name = "Chuyên viên phụ trách")]
        public string CVName { get; set; }
        [Display(Name = "Nội dung và lý do đề xuất"), UIHint("EditorBox")]
        public string Body { get; set; }
        [StringLength(500), Display(Name = "Hồ sơ, tài liệu mình chứng kèm theo")]
        public string Url { get; set; }
        [Display(Name = "Phản hồi của phòng tuyển sinh"), UIHint("EditorBox")]
        public string CVFeedBack { get; set; }
        [Display(Name = "Mã đề xuất")]
        public string MaDeXuat { get; set; }
        [Display(Name = "Người phản hồi")]
        public string CVFbName { get; set; }
        [Display(Name = "Note")]
        public string Note { get; set; }
        [Display(Name = "Tổng hợp lỗi")]
        public int? TypeFaultId { get; set; }
        [Display(Name = "Tổng hợp lỗi")]
        public string TypeFaults { get; set; }
        [Display(Name = "Phân loại đề xuất")]
        public int? ProposalTypeId { get; set; }
        public int? FaultNumber { get; set; }
        [Display(Name = "Kết luận")]
        public TypeApprove? TypeApprove { get; set; }
        [Display(Name = "Cho phép CN bổ sung hồ sơ")]
        public bool BMEdit { get; set; }
        public bool CVSeen { get; set; }
        public bool NSSeen { get; set; }
        [Display(Name = "BM Duyệt")]
        public bool Active { get; set; } = true;
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}"), Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public virtual User User { get; set; }
        public virtual User User2 { get; set; }
        public virtual TypeFault TypeFault { get; set; }
        public virtual ProposalType ProposalType { get; set; }
        public virtual Office Office { get; set; }
        public virtual Zone Zone { get; set; }
        public Proposal()
        {
            //Active = true;
            CreateDate= DateTime.Now;
            NSSeen = true;
        }
    }
    public enum TypeProposal
    {
        [Display(Name = "Tăng/ Giảm lộ trình")]
        Type1 = 1,
        [Display(Name = "Gia hạn phiếu cọc")]
        Type2,
        [Display(Name = "Gia hạn ngày áp dụng quyết định UD")]
        Type3,
        [Display(Name = "Thay đổi quyết định UD trên đơn hàng")]
        Type4,
        [Display(Name = "Xuất bù tháng tặng/ quà tặng")]
        Type5,
        [Display(Name = "Chuyển cọc thành học phí")]
        Type6,
        [Display(Name = "Chuyển cọc giữa các học viên")]
        Type7,
        [Display(Name = "Thay đổi nhân sự chốt sale")]
        Type8,
        [Display(Name = "Thay đổi kỳ hạn trả góp")]
        Type9,
        [Display(Name = "Ghi nhận doanh thu bổ sung")]
        Type10,
        [Display(Name = "Trình ý kiến BLĐ")]
        Type11,
        [Display(Name = "Đối tượng áp dụng UD ngoại lệ")]
        Type12,
        [Display(Name = "Khác")]
        Type13,
    }
    public enum TypeApprove
    {
        [Display(Name = "Phê duyệt")]
        Type1 = 1,
        [Display(Name = "Không phê duyệt")]
        Type2,
        [Display(Name = "Đang xử lý")]
        Type3,
    }
}
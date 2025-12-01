using System;
using System.ComponentModel.DataAnnotations;

namespace OceanEduSlide.Models
{
    public class BC_PhieuThu_DB
    {
        public int Id { get; set; }
        public long? PhieuThuKeToan { get; set; }
        public string ChiNhanh { get; set; }
        public string MaNVChotSale { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}"), Display(Name = "Ngày thanh toán")]
        public Nullable<System.DateTime> NgayThanhToan { get; set; }
        [Display(Name = "Tháng ghi nhận Doanh thu")]
        public int? ThangTinhDThu { get; set; }
        [Display(Name = "Năm ghi nhận Doanh thu")]
        public int? NamTinhDThu { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}đ")]
        public Nullable<decimal> SUD { get; set; }
        [DisplayFormat(DataFormatString = "{0:N0}đ")]
        public Nullable<decimal> TUD { get; set; }
        public string Loai { get; set; }
        public string ReceiptCode { get; set; }
        public string MaHV { get; set; }
        public string TenHV { get; set; }
        public Nullable<double> PhanTramUD { get; set; }
        public string GioiTinh { get; set; }
        public string HinhThucThanhToan { get; set; }
        public string Notes { get; set; }
        public string DangKy { get; set; }
        public Nullable<System.DateTime> GioTao { get; set; }
        public string ChotSale { get; set; }
        public string CongTacVien { get; set; }
        public Nullable<int> ThangHocDuKien { get; set; }
        public decimal? ThangHocDuKienDecimal { get; set; }
        public string UD_FINAL { get; set; }
        public string LoaiCTH { get; set; }
        public string ChuongTrinhHoc { get; set; }
        public string CapDo { get; set; }
        public string Modun { get; set; }
        public string UD_NhomUDFINAL { get; set; }
        public Nullable<long> HDBH { get; set; }
        public string DonHang { get; set; }
        public string UDPhieuThu { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}"), Display(Name = "Ngày tạo")]
        public Nullable<System.DateTime> CreateDate { get; set; } = DateTime.Now;
        public string TrangThai { get; set; }
        public bool THDB { get; set; }
    }

}
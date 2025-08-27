using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OceanEduSlide.Models
{
    public class BC_PhieuThu_THDB
    {
        public long PhieuThuKeToan { get; set; }
        public string ChiNhanh { get; set; }
        public string MaNVChotSale { get; set; }
        public Nullable<System.DateTime> NgayThanhToan { get; set; }
        public Nullable<decimal> TUD { get; set; }
        public Nullable<decimal> SUD { get; set; }
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
        public string UD_FINAL { get; set; }
        public string LoaiCTH { get; set; }
        public string ChuongTrinhHoc { get; set; }
        public string CapDo { get; set; }
        public string Modun { get; set; }
        public string UD_NhomUDFINAL { get; set; }
        public Nullable<long> HDBH { get; set; }
        public string DonHang { get; set; }
        public string UDPhieuThu { get; set; }
        public Nullable<System.DateTime> ThoiGianCapNhat { get; set; }
    }

}
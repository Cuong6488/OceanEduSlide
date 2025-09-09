namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class phieuthu : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BC_PhieuThu_DB",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PhieuThuKeToan = c.Long(),
                        ChiNhanh = c.String(),
                        MaNVChotSale = c.String(),
                        NgayThanhToan = c.DateTime(),
                        SUD = c.Decimal(precision: 18, scale: 2),
                        TUD = c.Decimal(precision: 18, scale: 2),
                        Loai = c.String(),
                        ReceiptCode = c.String(),
                        MaHV = c.String(),
                        TenHV = c.String(),
                        PhanTramUD = c.Double(),
                        GioiTinh = c.String(),
                        HinhThucThanhToan = c.String(),
                        Notes = c.String(),
                        DangKy = c.String(),
                        GioTao = c.DateTime(),
                        ChotSale = c.String(),
                        CongTacVien = c.String(),
                        ThangHocDuKien = c.Int(),
                        UD_FINAL = c.String(),
                        LoaiCTH = c.String(),
                        ChuongTrinhHoc = c.String(),
                        CapDo = c.String(),
                        Modun = c.String(),
                        UD_NhomUDFINAL = c.String(),
                        HDBH = c.Long(),
                        DonHang = c.String(),
                        UDPhieuThu = c.String(),
                        CreateDate = c.DateTime(),
                        THDB = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.BC_PhieuThu_DB");
        }
    }
}

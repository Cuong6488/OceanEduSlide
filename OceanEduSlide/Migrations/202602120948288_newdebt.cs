namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class newdebt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ConfigSites", "AutoDebt", c => c.Boolean(nullable: false));
            AddColumn("dbo.Debts", "NgayPhatSinhCoc", c => c.DateTime());
            AddColumn("dbo.Debts", "NgayLenDon", c => c.DateTime());
            AddColumn("dbo.Debts", "MaDonHang", c => c.String());
            AddColumn("dbo.Debts", "OfficeId", c => c.Int());
            AddColumn("dbo.Debts", "PhaiThu", c => c.Boolean(nullable: false));
            AddColumn("dbo.Debts", "Auto", c => c.Boolean(nullable: false));
            AddColumn("dbo.Debts", "TypeData", c => c.Int(nullable: false));
            CreateIndex("dbo.Debts", "OfficeId");
            AddForeignKey("dbo.Debts", "OfficeId", "dbo.Offices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Debts", "OfficeId", "dbo.Offices");
            DropIndex("dbo.Debts", new[] { "OfficeId" });
            DropColumn("dbo.Debts", "TypeData");
            DropColumn("dbo.Debts", "Auto");
            DropColumn("dbo.Debts", "PhaiThu");
            DropColumn("dbo.Debts", "OfficeId");
            DropColumn("dbo.Debts", "MaDonHang");
            DropColumn("dbo.Debts", "NgayLenDon");
            DropColumn("dbo.Debts", "NgayPhatSinhCoc");
            DropColumn("dbo.ConfigSites", "AutoDebt");
        }
    }
}

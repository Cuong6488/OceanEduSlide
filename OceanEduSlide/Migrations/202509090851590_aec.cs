namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class aec : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ReportDatas", "OfficeId", "dbo.Offices");
            DropIndex("dbo.ReportDatas", new[] { "OfficeId" });
            AddColumn("dbo.HistoryUsers", "ZoneId", c => c.Int());
            AddColumn("dbo.HistoryUsers", "CreateDate", c => c.DateTime());
            AddColumn("dbo.ReportDatas", "ZoneId", c => c.Int());
            AlterColumn("dbo.ReportDatas", "OfficeId", c => c.Int());
            CreateIndex("dbo.HistoryUsers", "ZoneId");
            CreateIndex("dbo.ReportDatas", "OfficeId");
            AddForeignKey("dbo.HistoryUsers", "ZoneId", "dbo.Zones", "Id");
            AddForeignKey("dbo.ReportDatas", "OfficeId", "dbo.Offices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportDatas", "OfficeId", "dbo.Offices");
            DropForeignKey("dbo.HistoryUsers", "ZoneId", "dbo.Zones");
            DropIndex("dbo.ReportDatas", new[] { "OfficeId" });
            DropIndex("dbo.HistoryUsers", new[] { "ZoneId" });
            AlterColumn("dbo.ReportDatas", "OfficeId", c => c.Int(nullable: false));
            DropColumn("dbo.ReportDatas", "ZoneId");
            DropColumn("dbo.HistoryUsers", "CreateDate");
            DropColumn("dbo.HistoryUsers", "ZoneId");
            CreateIndex("dbo.ReportDatas", "OfficeId");
            AddForeignKey("dbo.ReportDatas", "OfficeId", "dbo.Offices", "Id", cascadeDelete: true);
        }
    }
}

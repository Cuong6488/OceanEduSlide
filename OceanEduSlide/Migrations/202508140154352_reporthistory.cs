namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class reporthistory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportDatas", "HistoryUserId", c => c.Int());
            CreateIndex("dbo.ReportDatas", "HistoryUserId");
            AddForeignKey("dbo.ReportDatas", "HistoryUserId", "dbo.HistoryUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportDatas", "HistoryUserId", "dbo.HistoryUsers");
            DropIndex("dbo.ReportDatas", new[] { "HistoryUserId" });
            DropColumn("dbo.ReportDatas", "HistoryUserId");
        }
    }
}

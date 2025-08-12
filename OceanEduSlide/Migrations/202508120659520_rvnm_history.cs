namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rvnm_history : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RevenueUser_Month", "HistoryUserId", c => c.Int());
            CreateIndex("dbo.RevenueUser_Month", "HistoryUserId");
            AddForeignKey("dbo.RevenueUser_Month", "HistoryUserId", "dbo.HistoryUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_Month", "HistoryUserId", "dbo.HistoryUsers");
            DropIndex("dbo.RevenueUser_Month", new[] { "HistoryUserId" });
            DropColumn("dbo.RevenueUser_Month", "HistoryUserId");
        }
    }
}

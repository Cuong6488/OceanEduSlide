namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class historyUCallLog : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.CallLogs", "HistoryUserId", c => c.Int());
            CreateIndex("dbo.CallLogs", "HistoryUserId");
            AddForeignKey("dbo.CallLogs", "HistoryUserId", "dbo.HistoryUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CallLogs", "HistoryUserId", "dbo.HistoryUsers");
            DropIndex("dbo.CallLogs", new[] { "HistoryUserId" });
            DropColumn("dbo.CallLogs", "HistoryUserId");
        }
    }
}

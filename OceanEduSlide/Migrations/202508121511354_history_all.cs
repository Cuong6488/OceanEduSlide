namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class history_all : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RevenueUser_Month_BM", "HistoryUserId", c => c.Int());
            AddColumn("dbo.RevenueUser_DayOfWeek", "HistoryUserId", c => c.Int());
            AddColumn("dbo.RevenueUser_Week_Real", "HistoryUserId", c => c.Int());
            AddColumn("dbo.RevenueUser_Week", "HistoryUserId", c => c.Int());
            AddColumn("dbo.RevenueUser_DayOfWeek_Real", "HistoryUserId", c => c.Int());
            AddColumn("dbo.RevenueUser_Month_BM_real", "HistoryUserId", c => c.Int());
            CreateIndex("dbo.RevenueUser_Month_BM", "HistoryUserId");
            CreateIndex("dbo.RevenueUser_DayOfWeek", "HistoryUserId");
            CreateIndex("dbo.RevenueUser_Week_Real", "HistoryUserId");
            CreateIndex("dbo.RevenueUser_Week", "HistoryUserId");
            CreateIndex("dbo.RevenueUser_DayOfWeek_Real", "HistoryUserId");
            CreateIndex("dbo.RevenueUser_Month_BM_real", "HistoryUserId");
            AddForeignKey("dbo.RevenueUser_Month_BM", "HistoryUserId", "dbo.HistoryUsers", "Id");
            AddForeignKey("dbo.RevenueUser_DayOfWeek", "HistoryUserId", "dbo.HistoryUsers", "Id");
            AddForeignKey("dbo.RevenueUser_Week_Real", "HistoryUserId", "dbo.HistoryUsers", "Id");
            AddForeignKey("dbo.RevenueUser_Week", "HistoryUserId", "dbo.HistoryUsers", "Id");
            AddForeignKey("dbo.RevenueUser_DayOfWeek_Real", "HistoryUserId", "dbo.HistoryUsers", "Id");
            AddForeignKey("dbo.RevenueUser_Month_BM_real", "HistoryUserId", "dbo.HistoryUsers", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_Month_BM_real", "HistoryUserId", "dbo.HistoryUsers");
            DropForeignKey("dbo.RevenueUser_DayOfWeek_Real", "HistoryUserId", "dbo.HistoryUsers");
            DropForeignKey("dbo.RevenueUser_Week", "HistoryUserId", "dbo.HistoryUsers");
            DropForeignKey("dbo.RevenueUser_Week_Real", "HistoryUserId", "dbo.HistoryUsers");
            DropForeignKey("dbo.RevenueUser_DayOfWeek", "HistoryUserId", "dbo.HistoryUsers");
            DropForeignKey("dbo.RevenueUser_Month_BM", "HistoryUserId", "dbo.HistoryUsers");
            DropIndex("dbo.RevenueUser_Month_BM_real", new[] { "HistoryUserId" });
            DropIndex("dbo.RevenueUser_DayOfWeek_Real", new[] { "HistoryUserId" });
            DropIndex("dbo.RevenueUser_Week", new[] { "HistoryUserId" });
            DropIndex("dbo.RevenueUser_Week_Real", new[] { "HistoryUserId" });
            DropIndex("dbo.RevenueUser_DayOfWeek", new[] { "HistoryUserId" });
            DropIndex("dbo.RevenueUser_Month_BM", new[] { "HistoryUserId" });
            DropColumn("dbo.RevenueUser_Month_BM_real", "HistoryUserId");
            DropColumn("dbo.RevenueUser_DayOfWeek_Real", "HistoryUserId");
            DropColumn("dbo.RevenueUser_Week", "HistoryUserId");
            DropColumn("dbo.RevenueUser_Week_Real", "HistoryUserId");
            DropColumn("dbo.RevenueUser_DayOfWeek", "HistoryUserId");
            DropColumn("dbo.RevenueUser_Month_BM", "HistoryUserId");
        }
    }
}

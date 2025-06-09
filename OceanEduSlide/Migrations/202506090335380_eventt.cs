namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class eventt : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "RevenueAverage", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Events", "UserIds", c => c.String());
            AddColumn("dbo.Events", "Days", c => c.String());
            AddColumn("dbo.RevenueUser_DayOfWeek", "EventId", c => c.Int());
            CreateIndex("dbo.RevenueUser_DayOfWeek", "EventId");
            AddForeignKey("dbo.RevenueUser_DayOfWeek", "EventId", "dbo.Events", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_DayOfWeek", "EventId", "dbo.Events");
            DropIndex("dbo.RevenueUser_DayOfWeek", new[] { "EventId" });
            DropColumn("dbo.RevenueUser_DayOfWeek", "EventId");
            DropColumn("dbo.Events", "Days");
            DropColumn("dbo.Events", "UserIds");
            DropColumn("dbo.Users", "RevenueAverage");
        }
    }
}

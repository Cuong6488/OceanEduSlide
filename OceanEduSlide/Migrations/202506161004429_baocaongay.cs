namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class baocaongay : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RevenueUser_DayOfWeek_Real",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        EventId = c.Int(),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        WeekNumber = c.Int(nullable: false),
                        DayofWeek = c.Int(nullable: false),
                        TargetBM = c.Decimal(precision: 18, scale: 2),
                        DataQuantity = c.Decimal(precision: 18, scale: 2),
                        Confirm1 = c.Decimal(precision: 18, scale: 2),
                        Confirm2 = c.Decimal(precision: 18, scale: 2),
                        Confirm3 = c.Decimal(precision: 18, scale: 2),
                        CI = c.Decimal(precision: 18, scale: 2),
                        DT = c.Decimal(precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Events", t => t.EventId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.EventId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_DayOfWeek_Real", "UserId", "dbo.Users");
            DropForeignKey("dbo.RevenueUser_DayOfWeek_Real", "EventId", "dbo.Events");
            DropIndex("dbo.RevenueUser_DayOfWeek_Real", new[] { "EventId" });
            DropIndex("dbo.RevenueUser_DayOfWeek_Real", new[] { "UserId" });
            DropTable("dbo.RevenueUser_DayOfWeek_Real");
        }
    }
}

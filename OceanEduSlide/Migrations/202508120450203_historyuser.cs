namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class historyuser : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HistoryUsers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        OfficeId = c.Int(),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        TypeUser = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        DayStart = c.DateTime(nullable: false),
                        DayEnd = c.DateTime(),
                        WeekNumber = c.Int(),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.OfficeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HistoryUsers", "UserId", "dbo.Users");
            DropForeignKey("dbo.HistoryUsers", "OfficeId", "dbo.Offices");
            DropIndex("dbo.HistoryUsers", new[] { "OfficeId" });
            DropIndex("dbo.HistoryUsers", new[] { "UserId" });
            DropTable("dbo.HistoryUsers");
        }
    }
}

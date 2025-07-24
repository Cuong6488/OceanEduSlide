namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class calllog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CallLogs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UniqueId = c.String(maxLength: 100),
                        CallDate = c.DateTime(nullable: false),
                        CallDateString = c.String(),
                        UserId = c.Int(nullable: false),
                        Phone = c.String(maxLength: 20),
                        Exten = c.String(maxLength: 20),
                        Duration = c.Int(nullable: false),
                        BillSec = c.Int(nullable: false),
                        Disposition = c.String(maxLength: 20),
                        RecordingFile = c.String(maxLength: 300),
                        CNam = c.String(maxLength: 50),
                        Type = c.String(maxLength: 10),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.CallLogs", "UserId", "dbo.Users");
            DropIndex("dbo.CallLogs", new[] { "UserId" });
            DropTable("dbo.CallLogs");
        }
    }
}

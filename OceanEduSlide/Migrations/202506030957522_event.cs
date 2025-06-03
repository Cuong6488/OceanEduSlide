namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _event : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Events",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OfficeId = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        WeekNumber = c.Int(nullable: false),
                        DayofWeek = c.Int(nullable: false),
                        TypeEvent = c.Int(nullable: false),
                        TypeJoin = c.Int(nullable: false),
                        Ages = c.String(nullable: false, maxLength: 15),
                        Range = c.Int(nullable: false),
                        TimeFrom = c.String(nullable: false),
                        TimeTo = c.String(nullable: false),
                        LinkName = c.String(),
                        LinkUrl = c.String(),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .Index(t => t.OfficeId);
            
            CreateTable(
                "dbo.RevenueUser_Week_Real",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 100),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        TargetBM = c.Decimal(nullable: false, precision: 18, scale: 2),
                        WeekNumber = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            AddColumn("dbo.Users", "MaNhanVien", c => c.String());
            AddColumn("dbo.Users", "Confirm1", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Users", "Confirm2", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Users", "Confirm3", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Users", "CI", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Users", "DT", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_Week_Real", "UserId", "dbo.Users");
            DropForeignKey("dbo.Events", "OfficeId", "dbo.Offices");
            DropIndex("dbo.RevenueUser_Week_Real", new[] { "UserId" });
            DropIndex("dbo.Events", new[] { "OfficeId" });
            DropColumn("dbo.Users", "DT");
            DropColumn("dbo.Users", "CI");
            DropColumn("dbo.Users", "Confirm3");
            DropColumn("dbo.Users", "Confirm2");
            DropColumn("dbo.Users", "Confirm1");
            DropColumn("dbo.Users", "MaNhanVien");
            DropTable("dbo.RevenueUser_Week_Real");
            DropTable("dbo.Events");
        }
    }
}

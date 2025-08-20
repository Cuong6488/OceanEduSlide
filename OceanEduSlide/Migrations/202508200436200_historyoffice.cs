namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class historyoffice : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.HistoryOffices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OfficeId = c.Int(nullable: false),
                        ZoneId = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        DBATL = c.Int(nullable: false),
                        DBEC = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        GroupOffice = c.Int(nullable: false),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .ForeignKey("dbo.Zones", t => t.ZoneId, cascadeDelete: true)
                .Index(t => t.OfficeId)
                .Index(t => t.ZoneId);
            
            CreateTable(
                "dbo.TargetGroups",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        Target_A = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Target_B = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Target_C = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Target_D = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.WorkingDays",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Year = c.Int(nullable: false),
                        WorkingDayMonth1 = c.Int(nullable: false),
                        WorkingDayMonth2 = c.Int(nullable: false),
                        WorkingDayMonth3 = c.Int(nullable: false),
                        WorkingDayMonth4 = c.Int(nullable: false),
                        WorkingDayMonth5 = c.Int(nullable: false),
                        WorkingDayMonth6 = c.Int(nullable: false),
                        WorkingDayMonth7 = c.Int(nullable: false),
                        WorkingDayMonth8 = c.Int(nullable: false),
                        WorkingDayMonth9 = c.Int(nullable: false),
                        WorkingDayMonth10 = c.Int(nullable: false),
                        WorkingDayMonth11 = c.Int(nullable: false),
                        WorkingDayMonth12 = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.HistoryUsers", "NewUser", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.HistoryOffices", "ZoneId", "dbo.Zones");
            DropForeignKey("dbo.HistoryOffices", "OfficeId", "dbo.Offices");
            DropIndex("dbo.HistoryOffices", new[] { "ZoneId" });
            DropIndex("dbo.HistoryOffices", new[] { "OfficeId" });
            DropColumn("dbo.HistoryUsers", "NewUser");
            DropTable("dbo.WorkingDays");
            DropTable("dbo.TargetGroups");
            DropTable("dbo.HistoryOffices");
        }
    }
}

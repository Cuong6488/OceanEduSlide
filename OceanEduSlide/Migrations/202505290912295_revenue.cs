namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class revenue : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RevenueUser_Month_BM",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        TargetBM = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.RevenueOffice_BM",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        OfficeId = c.Int(nullable: false),
                        TargetBM_TS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TargetBM_HV = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TargetBM_SAB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TargetBM_New = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .Index(t => t.OfficeId);
            
            CreateTable(
                "dbo.RevenueOffices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        OfficeId = c.Int(nullable: false),
                        Target_TS = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Target_HV = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Target_SAB = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .Index(t => t.OfficeId);
            
            CreateTable(
                "dbo.RevenueUser_Month",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        Target = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.RevenueUser_Week",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        UserId = c.Int(nullable: false),
                        TargetBM = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            AddColumn("dbo.Users", "TypeUser", c => c.Int());
            AddColumn("dbo.Users", "OfficeIds", c => c.String());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_Week", "UserId", "dbo.Users");
            DropForeignKey("dbo.RevenueUser_Month", "UserId", "dbo.Users");
            DropForeignKey("dbo.RevenueOffices", "OfficeId", "dbo.Offices");
            DropForeignKey("dbo.RevenueOffice_BM", "OfficeId", "dbo.Offices");
            DropForeignKey("dbo.RevenueUser_Month_BM", "UserId", "dbo.Users");
            DropIndex("dbo.RevenueUser_Week", new[] { "UserId" });
            DropIndex("dbo.RevenueUser_Month", new[] { "UserId" });
            DropIndex("dbo.RevenueOffices", new[] { "OfficeId" });
            DropIndex("dbo.RevenueOffice_BM", new[] { "OfficeId" });
            DropIndex("dbo.RevenueUser_Month_BM", new[] { "UserId" });
            DropColumn("dbo.Users", "OfficeIds");
            DropColumn("dbo.Users", "TypeUser");
            DropTable("dbo.RevenueUser_Week");
            DropTable("dbo.RevenueUser_Month");
            DropTable("dbo.RevenueOffices");
            DropTable("dbo.RevenueOffice_BM");
            DropTable("dbo.RevenueUser_Month_BM");
        }
    }
}

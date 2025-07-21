namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class reportdt : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ReportCategories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                        ReportCategoryId = c.Int(),
                        TypeCat = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ReportCategories", t => t.ReportCategoryId)
                .Index(t => t.ReportCategoryId);
            
            CreateTable(
                "dbo.ReportDatas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        OfficeId = c.Int(nullable: false),
                        ReportCategoryId = c.Int(nullable: false),
                        Data = c.String(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                        Active = c.Boolean(nullable: false),
                        Sort = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .ForeignKey("dbo.ReportCategories", t => t.ReportCategoryId, cascadeDelete: true)
                .Index(t => t.OfficeId)
                .Index(t => t.ReportCategoryId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportDatas", "ReportCategoryId", "dbo.ReportCategories");
            DropForeignKey("dbo.ReportDatas", "OfficeId", "dbo.Offices");
            DropForeignKey("dbo.ReportCategories", "ReportCategoryId", "dbo.ReportCategories");
            DropIndex("dbo.ReportDatas", new[] { "ReportCategoryId" });
            DropIndex("dbo.ReportDatas", new[] { "OfficeId" });
            DropIndex("dbo.ReportCategories", new[] { "ReportCategoryId" });
            DropTable("dbo.ReportDatas");
            DropTable("dbo.ReportCategories");
        }
    }
}

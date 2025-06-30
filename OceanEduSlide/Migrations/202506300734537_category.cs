namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class category : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Index = c.String(),
                        Content = c.String(),
                        Regulation = c.String(),
                        ProposalLink = c.String(),
                        FollowLink = c.String(),
                        Note = c.String(),
                        QDLink = c.String(),
                        QDNumber = c.String(),
                        Zone = c.String(),
                        Month = c.Int(),
                        TypeCategory = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.RankOffices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        OfficeId = c.Int(nullable: false),
                        DSHT = c.String(),
                        PTHT = c.String(),
                        DSDT = c.String(),
                        PTHTDT = c.String(),
                        TopHT = c.String(),
                        TopDT = c.String(),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.OfficeId, cascadeDelete: true)
                .Index(t => t.OfficeId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RankOffices", "OfficeId", "dbo.Offices");
            DropIndex("dbo.RankOffices", new[] { "OfficeId" });
            DropTable("dbo.RankOffices");
            DropTable("dbo.Categories");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class proposal : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Proposals",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        TypeProposal = c.Int(nullable: false),
                        Body = c.String(),
                        Url = c.String(maxLength: 500),
                        CVFeedBack = c.String(maxLength: 500),
                        GDFeedBack = c.String(maxLength: 500),
                        TypeFault = c.Int(),
                        FaultNumber = c.Int(),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proposals", "UserId", "dbo.Users");
            DropIndex("dbo.Proposals", new[] { "UserId" });
            DropTable("dbo.Proposals");
        }
    }
}

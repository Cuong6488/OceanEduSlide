namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class typeproposal : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProposalTypes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Content = c.String(maxLength: 100),
                        Active = c.Boolean(nullable: false),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.User_Id);
            
            AddColumn("dbo.Proposals", "ProposalTypeId", c => c.Int());
            CreateIndex("dbo.Proposals", "ProposalTypeId");
            AddForeignKey("dbo.Proposals", "ProposalTypeId", "dbo.ProposalTypes", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proposals", "ProposalTypeId", "dbo.ProposalTypes");
            DropForeignKey("dbo.ProposalTypes", "User_Id", "dbo.Users");
            DropIndex("dbo.ProposalTypes", new[] { "User_Id" });
            DropIndex("dbo.Proposals", new[] { "ProposalTypeId" });
            DropColumn("dbo.Proposals", "ProposalTypeId");
            DropTable("dbo.ProposalTypes");
        }
    }
}

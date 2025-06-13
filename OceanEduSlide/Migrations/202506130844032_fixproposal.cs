namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fixproposal : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TypeFaults",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Content = c.String(maxLength: 100),
                        Active = c.Boolean(nullable: false),
                        Office_Id = c.Int(),
                        User_Id = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Offices", t => t.Office_Id)
                .ForeignKey("dbo.Users", t => t.User_Id)
                .Index(t => t.Office_Id)
                .Index(t => t.User_Id);
            
            AddColumn("dbo.Proposals", "TypeFaultId", c => c.Int());
            AddColumn("dbo.Proposals", "TypeApprove", c => c.Int());
            CreateIndex("dbo.Proposals", "TypeFaultId");
            AddForeignKey("dbo.Proposals", "TypeFaultId", "dbo.TypeFaults", "Id");
            DropColumn("dbo.Proposals", "TypeProposal");
            DropColumn("dbo.Proposals", "GDFeedBack");
            DropColumn("dbo.Proposals", "TypeFault");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Proposals", "TypeFault", c => c.Int());
            AddColumn("dbo.Proposals", "GDFeedBack", c => c.String(maxLength: 500));
            AddColumn("dbo.Proposals", "TypeProposal", c => c.Int(nullable: false));
            DropForeignKey("dbo.Proposals", "TypeFaultId", "dbo.TypeFaults");
            DropForeignKey("dbo.TypeFaults", "User_Id", "dbo.Users");
            DropForeignKey("dbo.TypeFaults", "Office_Id", "dbo.Offices");
            DropIndex("dbo.TypeFaults", new[] { "User_Id" });
            DropIndex("dbo.TypeFaults", new[] { "Office_Id" });
            DropIndex("dbo.Proposals", new[] { "TypeFaultId" });
            DropColumn("dbo.Proposals", "TypeApprove");
            DropColumn("dbo.Proposals", "TypeFaultId");
            DropTable("dbo.TypeFaults");
        }
    }
}

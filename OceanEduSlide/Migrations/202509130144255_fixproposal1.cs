namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fixproposal1 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Proposals", "UserId", "dbo.Users");
            AddColumn("dbo.Proposals", "UserId2", c => c.Int());
            AddColumn("dbo.Proposals", "CVFbName", c => c.String());
            AddColumn("dbo.Proposals", "BMEdit", c => c.Boolean(nullable: false));
            AddColumn("dbo.Proposals", "User_Id", c => c.Int());
            AddColumn("dbo.Proposals", "User2_Id", c => c.Int());
            CreateIndex("dbo.Proposals", "User_Id");
            CreateIndex("dbo.Proposals", "User2_Id");
            AddForeignKey("dbo.Proposals", "User2_Id", "dbo.Users", "Id");
            AddForeignKey("dbo.Proposals", "User_Id", "dbo.Users", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proposals", "User_Id", "dbo.Users");
            DropForeignKey("dbo.Proposals", "User2_Id", "dbo.Users");
            DropIndex("dbo.Proposals", new[] { "User2_Id" });
            DropIndex("dbo.Proposals", new[] { "User_Id" });
            DropColumn("dbo.Proposals", "User2_Id");
            DropColumn("dbo.Proposals", "User_Id");
            DropColumn("dbo.Proposals", "BMEdit");
            DropColumn("dbo.Proposals", "CVFbName");
            DropColumn("dbo.Proposals", "UserId2");
            AddForeignKey("dbo.Proposals", "UserId", "dbo.Users", "Id", cascadeDelete: true);
        }
    }
}

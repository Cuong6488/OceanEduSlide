namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class proposalOffice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "OfficeId", c => c.Int(nullable: false));
            AddColumn("dbo.Proposals", "Office_Id", c => c.Int());
            CreateIndex("dbo.Proposals", "OfficeId");
            CreateIndex("dbo.Proposals", "Office_Id");
            AddForeignKey("dbo.Proposals", "OfficeId", "dbo.Offices", "Id");
            AddForeignKey("dbo.Proposals", "Office_Id", "dbo.Offices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proposals", "Office_Id", "dbo.Offices");
            DropForeignKey("dbo.Proposals", "OfficeId", "dbo.Offices");
            DropIndex("dbo.Proposals", new[] { "Office_Id" });
            DropIndex("dbo.Proposals", new[] { "OfficeId" });
            DropColumn("dbo.Proposals", "Office_Id");
            DropColumn("dbo.Proposals", "OfficeId");
        }
    }
}

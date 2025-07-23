namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class officeidnull : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Users", "OfficeId", "dbo.Offices");
            DropIndex("dbo.Users", new[] { "OfficeId" });
            AlterColumn("dbo.Users", "OfficeId", c => c.Int());
            CreateIndex("dbo.Users", "OfficeId");
            AddForeignKey("dbo.Users", "OfficeId", "dbo.Offices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Users", "OfficeId", "dbo.Offices");
            DropIndex("dbo.Users", new[] { "OfficeId" });
            AlterColumn("dbo.Users", "OfficeId", c => c.Int(nullable: false));
            CreateIndex("dbo.Users", "OfficeId");
            AddForeignKey("dbo.Users", "OfficeId", "dbo.Offices", "Id", cascadeDelete: true);
        }
    }
}

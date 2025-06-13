namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class zoneid : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "ZoneId", c => c.Int(nullable: false));
            CreateIndex("dbo.Proposals", "ZoneId");
            AddForeignKey("dbo.Proposals", "ZoneId", "dbo.Zones", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Proposals", "ZoneId", "dbo.Zones");
            DropIndex("dbo.Proposals", new[] { "ZoneId" });
            DropColumn("dbo.Proposals", "ZoneId");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class autocallog : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ConfigSites", "AutoCallLog", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ConfigSites", "AutoCallLog");
        }
    }
}

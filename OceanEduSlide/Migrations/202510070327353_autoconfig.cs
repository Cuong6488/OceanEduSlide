namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class autoconfig : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ConfigSites", "AutoUser", c => c.Boolean(nullable: false));
            AddColumn("dbo.ConfigSites", "AutoRevenue", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ConfigSites", "AutoRevenue");
            DropColumn("dbo.ConfigSites", "AutoUser");
        }
    }
}

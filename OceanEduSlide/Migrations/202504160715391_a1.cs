namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class a1 : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.ConfigSites", "Offices", c => c.Int(nullable: false));
            //DropColumn("dbo.ConfigSites", "Agencies");
        }
        
        public override void Down()
        {
            //AddColumn("dbo.ConfigSites", "Agencies", c => c.Int(nullable: false));
            //DropColumn("dbo.ConfigSites", "Offices");
        }
    }
}

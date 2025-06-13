namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class zoneids : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "ZoneIds", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "ZoneIds");
        }
    }
}

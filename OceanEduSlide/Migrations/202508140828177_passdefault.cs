namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class passdefault : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ConfigSites", "Password", c => c.String(maxLength: 60));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ConfigSites", "Password");
        }
    }
}

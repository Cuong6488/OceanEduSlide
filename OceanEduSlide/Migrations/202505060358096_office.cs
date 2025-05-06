namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class office : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Offices", "ShortName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Offices", "ShortName");
        }
    }
}

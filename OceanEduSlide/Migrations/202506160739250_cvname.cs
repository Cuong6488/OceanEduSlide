namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class cvname : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "CVName", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proposals", "CVName");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class nameActivity : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Events", "Name", c => c.String(nullable: false, maxLength: 15));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Events", "Name");
        }
    }
}

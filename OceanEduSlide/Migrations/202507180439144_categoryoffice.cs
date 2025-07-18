namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class categoryoffice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Categories", "Offices", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Categories", "Offices");
        }
    }
}

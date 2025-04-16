namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dc : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Discounts", "Pathway", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Discounts", "Pathway");
        }
    }
}

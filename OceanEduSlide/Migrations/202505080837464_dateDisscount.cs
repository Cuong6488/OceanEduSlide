namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dateDisscount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Discounts", "EndDate", c => c.DateTime());
            AddColumn("dbo.Discounts", "StartDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Discounts", "StartDate");
            DropColumn("dbo.Discounts", "EndDate");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class createdatediscount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Discounts", "CreateDate", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Discounts", "CreateDate");
        }
    }
}

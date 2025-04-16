namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class a : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Discounts", "Password");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Discounts", "Password", c => c.String(nullable: false, maxLength: 60));
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rangeauto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Events", "RangeStudent", c => c.Int(nullable: false));
            AddColumn("dbo.Events", "RangeNewCustomer", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Events", "RangeNewCustomer");
            DropColumn("dbo.Events", "RangeStudent");
        }
    }
}

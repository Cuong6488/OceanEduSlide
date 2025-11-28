namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class groupe : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TargetGroups", "Target_E", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TargetGroups", "Target_E");
        }
    }
}

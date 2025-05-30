namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class week : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RevenueUser_Week", "Month", c => c.Int(nullable: false));
            AddColumn("dbo.RevenueUser_Week", "Year", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RevenueUser_Week", "Year");
            DropColumn("dbo.RevenueUser_Week", "Month");
        }
    }
}

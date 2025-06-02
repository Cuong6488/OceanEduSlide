namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class weeknumber : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RevenueUser_Week", "WeekNumber", c => c.Int(nullable: false));
            AlterColumn("dbo.RevenueUser_Week", "Name", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RevenueUser_Week", "Name", c => c.String(nullable: false, maxLength: 100));
            DropColumn("dbo.RevenueUser_Week", "WeekNumber");
        }
    }
}

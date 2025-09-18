namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class auto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportCategories", "Auto", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ReportCategories", "Auto");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rpcount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportCategories", "Count", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ReportCategories", "Count");
        }
    }
}

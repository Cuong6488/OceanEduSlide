namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class historyzone : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryUsers", "ZoneIds", c => c.String());
            AddColumn("dbo.HistoryUsers", "OfficeIds", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryUsers", "OfficeIds");
            DropColumn("dbo.HistoryUsers", "ZoneIds");
        }
    }
}

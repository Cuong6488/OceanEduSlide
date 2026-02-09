namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CNGop : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryOffices", "OfficeCodes", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryOffices", "OfficeCodes");
        }
    }
}

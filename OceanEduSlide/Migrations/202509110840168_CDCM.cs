namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CDCM : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryUsers", "CDCM", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryUsers", "CDCM");
        }
    }
}

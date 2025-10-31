namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dayreduceCG : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryUsers", "DayReduceCG", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryUsers", "DayReduceCG");
        }
    }
}

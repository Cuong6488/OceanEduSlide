namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _259 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryUsers", "DayReduce", c => c.Int());
            AddColumn("dbo.HistoryOffices", "NVKDOver", c => c.Int());
            AddColumn("dbo.HistoryOffices", "TargetReduce", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryOffices", "TargetReduce");
            DropColumn("dbo.HistoryOffices", "NVKDOver");
            DropColumn("dbo.HistoryUsers", "DayReduce");
        }
    }
}

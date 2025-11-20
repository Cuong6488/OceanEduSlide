namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class targerbase : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryOffices", "BaseTarget", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryOffices", "BaseTarget");
        }
    }
}

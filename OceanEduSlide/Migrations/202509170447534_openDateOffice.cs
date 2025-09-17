namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class openDateOffice : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Offices", "OpenDate", c => c.DateTime());
            AddColumn("dbo.ReportDatas", "DataReal", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ReportDatas", "DataReal");
            DropColumn("dbo.Offices", "OpenDate");
        }
    }
}

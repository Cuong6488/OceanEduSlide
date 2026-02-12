namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class datarealpercent : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ReportDatas", "DataReal", c => c.Decimal(precision: 18, scale: 6));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ReportDatas", "DataReal", c => c.Decimal(precision: 18, scale: 2));
        }
    }
}

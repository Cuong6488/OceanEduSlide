namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dataallownull : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.ReportDatas", "Data", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.ReportDatas", "Data", c => c.String(nullable: false));
        }
    }
}

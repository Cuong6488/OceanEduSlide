namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class createdate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.RevenueUser_Month_BM", "CreateDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.RevenueOffice_BM", "CreateDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.RevenueOffices", "CreateDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.RevenueUser_Month", "CreateDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.RevenueUser_Week", "CreateDate", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.RevenueUser_Week", "CreateDate");
            DropColumn("dbo.RevenueUser_Month", "CreateDate");
            DropColumn("dbo.RevenueOffices", "CreateDate");
            DropColumn("dbo.RevenueOffice_BM", "CreateDate");
            DropColumn("dbo.RevenueUser_Month_BM", "CreateDate");
        }
    }
}

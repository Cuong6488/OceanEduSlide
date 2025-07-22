namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class reporttype2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportDatas", "FullName", c => c.String());
            AddColumn("dbo.ReportDatas", "MaNhanVien", c => c.String());
            AddColumn("dbo.ReportDatas", "DayJoin", c => c.String());
            AddColumn("dbo.ReportDatas", "TypeUser", c => c.String());
            AddColumn("dbo.ReportDatas", "DaysWork", c => c.String());
            AddColumn("dbo.ReportDatas", "Note", c => c.String());
            AddColumn("dbo.ReportDatas", "DayOff", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.ReportDatas", "DayOff");
            DropColumn("dbo.ReportDatas", "Note");
            DropColumn("dbo.ReportDatas", "DaysWork");
            DropColumn("dbo.ReportDatas", "TypeUser");
            DropColumn("dbo.ReportDatas", "DayJoin");
            DropColumn("dbo.ReportDatas", "MaNhanVien");
            DropColumn("dbo.ReportDatas", "FullName");
        }
    }
}

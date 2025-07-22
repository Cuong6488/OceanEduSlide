namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class reportuser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ReportDatas", "UserId", c => c.Int());
            CreateIndex("dbo.ReportDatas", "UserId");
            AddForeignKey("dbo.ReportDatas", "UserId", "dbo.Users", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ReportDatas", "UserId", "dbo.Users");
            DropIndex("dbo.ReportDatas", new[] { "UserId" });
            DropColumn("dbo.ReportDatas", "UserId");
        }
    }
}

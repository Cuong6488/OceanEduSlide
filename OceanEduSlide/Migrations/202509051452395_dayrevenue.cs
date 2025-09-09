namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dayrevenue : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BC_PhieuThu_DB", "ThangTinhDThu", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BC_PhieuThu_DB", "ThangTinhDThu");
        }
    }
}

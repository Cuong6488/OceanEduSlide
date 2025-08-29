namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class trangthai : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BC_PhieuThu_DB", "TrangThai", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BC_PhieuThu_DB", "TrangThai");
        }
    }
}

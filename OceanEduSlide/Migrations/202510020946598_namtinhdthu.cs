namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class namtinhdthu : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BC_PhieuThu_DB", "NamTinhDThu", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BC_PhieuThu_DB", "NamTinhDThu");
        }
    }
}

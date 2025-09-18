namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class data189 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BC_PhieuThu_DB", "ThangHocDuKienDecimal", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.Proposals", "CVFeedBack", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Proposals", "CVFeedBack", c => c.String(maxLength: 500));
            DropColumn("dbo.BC_PhieuThu_DB", "ThangHocDuKienDecimal");
        }
    }
}

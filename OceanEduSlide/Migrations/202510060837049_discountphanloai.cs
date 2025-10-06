namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class discountphanloai : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Discounts", "PhanLoai", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Discounts", "PhanLoai");
        }
    }
}

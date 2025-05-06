namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class discountdecimal : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Discounts", "PercentDiscount", c => c.Decimal(precision: 18, scale: 15));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Discounts", "PercentDiscount", c => c.Single());
        }
    }
}

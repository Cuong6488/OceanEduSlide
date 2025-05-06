namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class discountfloat : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Discounts", "PercentDiscount", c => c.Single());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Discounts", "PercentDiscount", c => c.Decimal(precision: 18, scale: 2));
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class nullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.RevenueUser_DayOfWeek", "TargetBM", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "DataQuantity", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm1", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm2", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm3", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "CI", c => c.Decimal(precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "DT", c => c.Decimal(precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.RevenueUser_DayOfWeek", "DT", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "CI", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm3", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm2", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "Confirm1", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "DataQuantity", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AlterColumn("dbo.RevenueUser_DayOfWeek", "TargetBM", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
    }
}

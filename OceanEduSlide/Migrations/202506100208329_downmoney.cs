namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class downmoney : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Debts", "DownMoney", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Debts", "DownMoney");
        }
    }
}

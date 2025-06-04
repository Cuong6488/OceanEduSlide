namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dbset : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RevenueUser_DayOfWeek",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        Month = c.Int(nullable: false),
                        WeekNumber = c.Int(nullable: false),
                        DayofWeek = c.Int(nullable: false),
                        TargetBM = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DataQuantity = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Confirm1 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Confirm2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Confirm3 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        CI = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DT = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_DayOfWeek", "UserId", "dbo.Users");
            DropIndex("dbo.RevenueUser_DayOfWeek", new[] { "UserId" });
            DropTable("dbo.RevenueUser_DayOfWeek");
        }
    }
}

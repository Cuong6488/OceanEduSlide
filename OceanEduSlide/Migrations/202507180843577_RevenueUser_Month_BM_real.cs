namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RevenueUser_Month_BM_real : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.RevenueUser_Month_BM_real",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        TargetBM = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RevenueUser_Month_BM_real", "UserId", "dbo.Users");
            DropIndex("dbo.RevenueUser_Month_BM_real", new[] { "UserId" });
            DropTable("dbo.RevenueUser_Month_BM_real");
        }
    }
}

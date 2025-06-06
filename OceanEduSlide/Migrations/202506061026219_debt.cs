namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class debt : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Debts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DepositDate = c.String(),
                        Month = c.Int(nullable: false),
                        Year = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        StudentName = c.String(),
                        StudentCode = c.String(),
                        Cth = c.String(),
                        DiscountName = c.String(),
                        Pathway = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalMoney = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DebtMoney = c.Decimal(nullable: false, precision: 18, scale: 2),
                        DebtMoney2 = c.Decimal(nullable: false, precision: 18, scale: 2),
                        RemainMoney = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TypeDebt = c.Int(),
                        ChannelPay = c.Int(),
                        TypePay = c.Int(),
                        FileStatus = c.String(),
                        GrossDate = c.String(),
                        HardContent = c.String(),
                        ContactStatus = c.String(),
                        HandleWay = c.String(),
                        CreateDate = c.DateTime(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.DownPathways",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        DebtId = c.Int(nullable: false),
                        Pathway = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Money = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Debts", t => t.DebtId, cascadeDelete: true)
                .Index(t => t.DebtId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Debts", "UserId", "dbo.Users");
            DropForeignKey("dbo.DownPathways", "DebtId", "dbo.Debts");
            DropIndex("dbo.DownPathways", new[] { "DebtId" });
            DropIndex("dbo.Debts", new[] { "UserId" });
            DropTable("dbo.DownPathways");
            DropTable("dbo.Debts");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class debtparent : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Debts", "DebtId", c => c.Int());
            CreateIndex("dbo.Debts", "DebtId");
            AddForeignKey("dbo.Debts", "DebtId", "dbo.Debts", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Debts", "DebtId", "dbo.Debts");
            DropIndex("dbo.Debts", new[] { "DebtId" });
            DropColumn("dbo.Debts", "DebtId");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class a2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Debts", "UserOriginId", c => c.Int());
            AddColumn("dbo.Debts", "EditUser", c => c.Boolean(nullable: false));
            CreateIndex("dbo.Debts", "UserOriginId");
            AddForeignKey("dbo.Debts", "UserOriginId", "dbo.Users", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Debts", "UserOriginId", "dbo.Users");
            DropIndex("dbo.Debts", new[] { "UserOriginId" });
            DropColumn("dbo.Debts", "EditUser");
            DropColumn("dbo.Debts", "UserOriginId");
        }
    }
}

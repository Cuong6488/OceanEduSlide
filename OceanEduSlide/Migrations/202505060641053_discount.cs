namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class discount : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Discounts", "OfficeId", "dbo.Offices");
            DropIndex("dbo.Discounts", new[] { "OfficeId" });
            RenameColumn(table: "dbo.Discounts", name: "OfficeId", newName: "Office_Id");
            AddColumn("dbo.Discounts", "Offices", c => c.String(nullable: false));
            AddColumn("dbo.Discounts", "PathwayTo", c => c.Int(nullable: false));
            AddColumn("dbo.Discounts", "Cth", c => c.String());
            AlterColumn("dbo.Discounts", "Office_Id", c => c.Int());
            CreateIndex("dbo.Discounts", "Office_Id");
            AddForeignKey("dbo.Discounts", "Office_Id", "dbo.Offices", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Discounts", "Office_Id", "dbo.Offices");
            DropIndex("dbo.Discounts", new[] { "Office_Id" });
            AlterColumn("dbo.Discounts", "Office_Id", c => c.Int(nullable: false));
            DropColumn("dbo.Discounts", "Cth");
            DropColumn("dbo.Discounts", "PathwayTo");
            DropColumn("dbo.Discounts", "Offices");
            RenameColumn(table: "dbo.Discounts", name: "Office_Id", newName: "OfficeId");
            CreateIndex("dbo.Discounts", "OfficeId");
            AddForeignKey("dbo.Discounts", "OfficeId", "dbo.Offices", "Id", cascadeDelete: true);
        }
    }
}

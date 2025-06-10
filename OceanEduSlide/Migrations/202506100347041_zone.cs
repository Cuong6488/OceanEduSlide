namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class zone : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Zones",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        ShortCode = c.String(),
                        ShortName = c.String(),
                        OfficeIds = c.String(),
                        Hotline = c.String(maxLength: 20),
                        Email = c.String(maxLength: 50),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Users", "ZoneId", c => c.Int());
            AddColumn("dbo.Offices", "Zone_Id", c => c.Int());
            CreateIndex("dbo.Users", "ZoneId");
            CreateIndex("dbo.Offices", "Zone_Id");
            AddForeignKey("dbo.Offices", "Zone_Id", "dbo.Zones", "Id");
            AddForeignKey("dbo.Users", "ZoneId", "dbo.Zones", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Users", "ZoneId", "dbo.Zones");
            DropForeignKey("dbo.Offices", "Zone_Id", "dbo.Zones");
            DropIndex("dbo.Offices", new[] { "Zone_Id" });
            DropIndex("dbo.Users", new[] { "ZoneId" });
            DropColumn("dbo.Offices", "Zone_Id");
            DropColumn("dbo.Users", "ZoneId");
            DropTable("dbo.Zones");
        }
    }
}

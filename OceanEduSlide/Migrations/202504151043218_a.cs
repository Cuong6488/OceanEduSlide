namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class a : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Admins",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false),
                        Password = c.String(nullable: false, maxLength: 60),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.ConfigSites",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Facebook = c.String(maxLength: 500),
                        Linkedin = c.String(maxLength: 500),
                        TikTok = c.String(maxLength: 500),
                        Instagram = c.String(maxLength: 500),
                        Twitter = c.String(maxLength: 500),
                        Youtube = c.String(maxLength: 500),
                        UrlMessenger = c.String(maxLength: 200),
                        LiveChat = c.String(maxLength: 4000),
                        Title = c.String(nullable: false, maxLength: 200),
                        Description = c.String(maxLength: 500),
                        Years = c.Int(nullable: false),
                        Customers = c.Int(nullable: false),
                        Agencies = c.Int(nullable: false),
                        Slogan = c.String(nullable: false, maxLength: 200),
                        AboutText = c.String(),
                        AboutBody = c.String(),
                        AboutFooter = c.String(),
                        Image = c.String(maxLength: 500),
                        Favicon = c.String(maxLength: 500),
                        GoogleMap = c.String(maxLength: 4000),
                        Place = c.String(maxLength: 1000),
                        Hotline = c.String(nullable: false, maxLength: 20),
                        Email = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Offices",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Place = c.String(nullable: false),
                        ShortCode = c.String(nullable: false),
                        Hotline = c.String(nullable: false, maxLength: 20),
                        Email = c.String(nullable: false, maxLength: 50),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Offices");
            DropTable("dbo.ConfigSites");
            DropTable("dbo.Admins");
        }
    }
}

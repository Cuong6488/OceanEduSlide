namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class banner : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Banners",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Image = c.String(maxLength: 500),
                        ImageMobile = c.String(maxLength: 500),
                        GroupId = c.Int(nullable: false),
                        Slogan = c.String(maxLength: 100),
                        Description = c.String(maxLength: 300),
                        Url = c.String(maxLength: 500),
                        Active = c.Boolean(nullable: false),
                        Sort = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Banners");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class update_Office : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Offices", "Image", c => c.String());
            AlterColumn("dbo.Offices", "Place", c => c.String());
            AlterColumn("dbo.Offices", "ShortCode", c => c.String());
            AlterColumn("dbo.Offices", "Hotline", c => c.String(maxLength: 20));
            AlterColumn("dbo.Offices", "Email", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Offices", "Email", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.Offices", "Hotline", c => c.String(nullable: false, maxLength: 20));
            AlterColumn("dbo.Offices", "ShortCode", c => c.String(nullable: false));
            AlterColumn("dbo.Offices", "Place", c => c.String(nullable: false));
            DropColumn("dbo.Offices", "Image");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rolladmin : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Admins", "RoleAdmin", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Admins", "RoleAdmin");
        }
    }
}

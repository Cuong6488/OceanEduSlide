namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Fullname : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "Fullname", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "Fullname");
        }
    }
}

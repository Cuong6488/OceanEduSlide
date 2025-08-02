namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class oldacount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "OldAcount", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "OldAcount");
        }
    }
}

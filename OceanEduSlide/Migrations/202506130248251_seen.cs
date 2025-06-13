namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class seen : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "CVSeen", c => c.Boolean(nullable: false));
            AddColumn("dbo.Proposals", "NSSeen", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proposals", "NSSeen");
            DropColumn("dbo.Proposals", "CVSeen");
        }
    }
}

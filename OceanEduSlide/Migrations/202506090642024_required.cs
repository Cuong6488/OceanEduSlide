namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class required : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Events", "UserIds", c => c.String(nullable: false));
            AlterColumn("dbo.Events", "Days", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Events", "Days", c => c.String());
            AlterColumn("dbo.Events", "UserIds", c => c.String());
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class CDCMUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "CDCM", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "CDCM");
        }
    }
}

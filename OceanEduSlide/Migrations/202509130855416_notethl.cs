namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class notethl : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "Note", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proposals", "Note");
        }
    }
}

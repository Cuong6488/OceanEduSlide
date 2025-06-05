namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class rangestring : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Events", "Name", c => c.String(nullable: false, maxLength: 35));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Events", "Name", c => c.String(nullable: false, maxLength: 15));
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class sortdexuat : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.TypeFaults", "Sort", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.TypeFaults", "Sort");
        }
    }
}

namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class qd156 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.HistoryOffices", "QD156", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.HistoryOffices", "QD156");
        }
    }
}

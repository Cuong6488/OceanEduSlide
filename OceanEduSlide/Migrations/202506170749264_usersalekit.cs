namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class usersalekit : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "SaleKit", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "SaleKit");
        }
    }
}

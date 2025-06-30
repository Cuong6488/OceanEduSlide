namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class mdx : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "MaDeXuat", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Proposals", "MaDeXuat");
        }
    }
}

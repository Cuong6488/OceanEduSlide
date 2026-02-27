namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class maptype : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MapTypeUsers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CDCM = c.String(),
                        TypeUser = c.Int(nullable: false),
                        Admin = c.String(),
                        LastEdit = c.String(),
                        Edit = c.Boolean(nullable: false),
                        Sort = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                        CreateDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.MapTypeUsers");
        }
    }
}

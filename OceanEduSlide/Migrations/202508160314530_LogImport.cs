namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class LogImport : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LogImports",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        File = c.String(),
                        CreateDate = c.DateTime(nullable: false),
                        TypeImport = c.Int(nullable: false),
                        Admin = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.LogImports");
        }
    }
}

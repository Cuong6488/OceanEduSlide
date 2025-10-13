namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class groupdiscount : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.GroupDiscounts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        SoQD = c.String(nullable: false),
                        NhomQD = c.String(nullable: false),
                        Year = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
                        StartDate = c.DateTime(),
                        EndDate = c.DateTime(),
                        Note = c.String(),
                        PhanLoai = c.String(nullable: false),
                        Content = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.GroupDiscounts");
        }
    }
}

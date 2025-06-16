namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class zoneIdOffice : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Offices", name: "Zone_Id", newName: "ZoneId");
            RenameIndex(table: "dbo.Offices", name: "IX_Zone_Id", newName: "IX_ZoneId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.Offices", name: "IX_ZoneId", newName: "IX_Zone_Id");
            RenameColumn(table: "dbo.Offices", name: "ZoneId", newName: "Zone_Id");
        }
    }
}

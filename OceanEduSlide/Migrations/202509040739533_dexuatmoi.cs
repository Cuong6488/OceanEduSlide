namespace OceanEduSlide.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class dexuatmoi : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Proposals", "TypeFaults", c => c.String());
            AddColumn("dbo.ProposalTypes", "Sort", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ProposalTypes", "Sort");
            DropColumn("dbo.Proposals", "TypeFaults");
        }
    }
}

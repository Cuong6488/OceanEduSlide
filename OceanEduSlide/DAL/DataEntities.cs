using System.Data.Entity;
using System.Web.Services.Description;
using OceanEduSlide.Models;

namespace OceanEduSlide.DAL
{
    public class DataEntities : DbContext
    {
        public DataEntities() : base("name=DataEntities") { }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<ConfigSite> ConfigSites { get; set; }
        public DbSet<Office> Offices { get; set; }
        public DbSet<MemberCredential> MemberCredentials { get; set; }
        public DbSet<RevenueUser_DayOfWeek> RevenueUser_DayOfWeeks { get; set; }
        public DbSet<RevenueUser_DayOfWeek_Real> RevenueUser_DayOfWeek_Reals { get; set; }
        public DbSet<RevenueUser_Month_BM_real> RevenueUser_Month_BM_Reals { get; set; }
        public DbSet<Debt> Debts { get; set; }
        public DbSet<DownPathway> DownPathways { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<TypeFault> TypeFaults { get; set; }
        public DbSet<ProposalType> ProposalTypes { get; set; }
        public DbSet<RankOffice> RankOffices { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ReportCategory> ReportCategories { get; set; }
        public DbSet<ReportData> ReportDatas { get; set; }
        public DbSet<CallLog> CallLogs { get; set; }
        public DbSet<HistoryUser> HistoryUsers { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Proposal>()
                .HasRequired(p => p.Office)
                .WithMany()
                .HasForeignKey(p => p.OfficeId)
                .WillCascadeOnDelete(false);
        }

    }

}
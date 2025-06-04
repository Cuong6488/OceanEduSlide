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
    }
}
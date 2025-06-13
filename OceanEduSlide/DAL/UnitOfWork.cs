using OceanEduSlide.Models;
using System;

namespace OceanEduSlide.DAL
{
    public class UnitOfWork : IDisposable
    {
        private readonly DataEntities _context = new DataEntities();
        private GenericRepository<Admin> _adminRepository;
        private GenericRepository<ConfigSite> _configRepository;
        private GenericRepository<User> _userRepository;
        private GenericRepository<Office> _officeRepository;
        private GenericRepository<Discount> _discountRepository;
        private GenericRepository<MemberCredential> _memberCredentialRepository;
        private GenericRepository<RevenueOffice> _revenueOfficeRepository;
        private GenericRepository<RevenueOffice_BM> _revenueOffice_BMRepository;
        private GenericRepository<RevenueUser_Month> _revenueUser_MonthRepository;
        private GenericRepository<RevenueUser_Month_BM> _revenueUser_Month_BMRepository;
        private GenericRepository<RevenueUser_Week> _revenueUser_WeekRepository;
        private GenericRepository<RevenueUser_Week_Real> _revenueUser_Week_RealRepository;
        private GenericRepository<RevenueUser_DayOfWeek> _revenueUser_DayOfWeekRepository;
        private GenericRepository<Event> _eventRepository;
        private GenericRepository<Debt> _debtRepository;
        private GenericRepository<DownPathway> _downPathwaytRepository;
        private GenericRepository<Zone> _zonetRepository;
        private GenericRepository<Proposal> _proposaltRepository;
        public GenericRepository<Proposal> ProposalRepository =>
           _proposaltRepository ?? (_proposaltRepository = new GenericRepository<Proposal>(_context));
        public GenericRepository<Zone> ZoneRepository =>
           _zonetRepository ?? (_zonetRepository = new GenericRepository<Zone>(_context));
        public GenericRepository<Debt> DebtRepository =>
           _debtRepository ?? (_debtRepository = new GenericRepository<Debt>(_context));
        public GenericRepository<DownPathway> DownPathwayRepository =>
           _downPathwaytRepository ?? (_downPathwaytRepository = new GenericRepository<DownPathway>(_context));
        public GenericRepository<RevenueUser_DayOfWeek> RevenueUser_DayOfWeekRepository =>
           _revenueUser_DayOfWeekRepository ?? (_revenueUser_DayOfWeekRepository = new GenericRepository<RevenueUser_DayOfWeek>(_context));
        public GenericRepository<Event> EventRepository =>
           _eventRepository ?? (_eventRepository = new GenericRepository<Event>(_context));
        public GenericRepository<RevenueOffice> RevenueOfficeRepository =>
           _revenueOfficeRepository ?? (_revenueOfficeRepository = new GenericRepository<RevenueOffice>(_context));
        public GenericRepository<RevenueOffice_BM> RevenueOffice_BMRepository =>
           _revenueOffice_BMRepository ?? (_revenueOffice_BMRepository = new GenericRepository<RevenueOffice_BM>(_context));
        public GenericRepository<RevenueUser_Month> RevenueUser_MonthRepository =>
           _revenueUser_MonthRepository ?? (_revenueUser_MonthRepository = new GenericRepository<RevenueUser_Month>(_context));
        public GenericRepository<RevenueUser_Month_BM> RevenueUser_Month_BMRepository =>
           _revenueUser_Month_BMRepository ?? (_revenueUser_Month_BMRepository = new GenericRepository<RevenueUser_Month_BM>(_context));
        public GenericRepository<RevenueUser_Week> RevenueUser_WeekRepository =>
           _revenueUser_WeekRepository ?? (_revenueUser_WeekRepository = new GenericRepository<RevenueUser_Week>(_context));
        public GenericRepository<RevenueUser_Week_Real> RevenueUser_Week_RealRepository =>
           _revenueUser_Week_RealRepository ?? (_revenueUser_Week_RealRepository = new GenericRepository<RevenueUser_Week_Real>(_context));
        public GenericRepository<Office> OfficeRepository =>
           _officeRepository ?? (_officeRepository = new GenericRepository<Office>(_context));
        public GenericRepository<ConfigSite> ConfigSiteRepository =>
            _configRepository ?? (_configRepository = new GenericRepository<ConfigSite>(_context));
        public GenericRepository<Admin> AdminRepository =>
            _adminRepository ?? (_adminRepository = new GenericRepository<Admin>(_context));
        public GenericRepository<Discount> DiscountRepository =>
            _discountRepository ?? (_discountRepository = new GenericRepository<Discount>(_context));
        public GenericRepository<User> UserRepository =>
            _userRepository ?? (_userRepository = new GenericRepository<User>(_context));
        public GenericRepository<MemberCredential> MemberCredentialRepository =>
            _memberCredentialRepository ?? (_memberCredentialRepository = new GenericRepository<MemberCredential>(_context));
        public void Save()
        {
            _context.SaveChanges();
        }
        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
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
using OceanEduSlide.Models;
using System;

namespace OceanEduSlide.DAL
{
    public class UnitOfWork : IDisposable
    {
        private readonly DataEntities _context = new DataEntities();
        private GenericRepository<Admin> _adminRepository;
        private GenericRepository<ConfigSite> _configRepository;
        private GenericRepository<Office> _warrantyStationRepository;
        public GenericRepository<Office> WarrantyStationRepository =>
           _warrantyStationRepository ?? (_warrantyStationRepository = new GenericRepository<Office>(_context));
        public GenericRepository<ConfigSite> ConfigSiteRepository =>
            _configRepository ?? (_configRepository = new GenericRepository<ConfigSite>(_context));
        public GenericRepository<Admin> AdminRepository =>
            _adminRepository ?? (_adminRepository = new GenericRepository<Admin>(_context));
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
using OceanEduSlide.Controllers;
using OceanEduSlide.DAL;
using OceanEduSlide.Migrations;
using System;
using System.Collections.Generic;
//using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Hangfire;
using Hangfire.SqlServer;
using static OceanEduSlide.Controllers.ReportHomeController;
using System.Timers;

namespace OceanEduSlide
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static Timer _timer;

        protected void Application_Start()
        {
            ViewEngines.Engines.Clear();
            ViewEngines.Engines.Add(new RazorViewEngine());

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataEntities, Configuration>());
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            using (var unitofWork = new UnitOfWork())
            {
                Application["ConfigSite"] = unitofWork.ConfigSiteRepository.GetQuery().FirstOrDefault();
            }

            _timer = new Timer(21600000);
            _timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            _timer.Start();
            Task.Run(() => TriggerCallLogSync());
            //Task.Run(async () =>
            //{
            //    var controller = new ReportHomeController();
            //    await controller.Sync();
            //});
        }

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Task.Run(() => TriggerCallLogSync());

        }

        private async Task TriggerCallLogSync()
        {
            try
            {
                var callLogService = new CallLogService();

                // Chỉ đồng bộ ngày hôm trước
                await callLogService.SyncYesterdayAsync();
            }
            catch (Exception ex)
            {
                // Ghi log nếu cần
                System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
            }

        }

        protected void Application_End()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}

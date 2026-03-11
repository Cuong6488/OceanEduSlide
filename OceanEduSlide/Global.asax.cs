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
using FluentScheduler;

namespace OceanEduSlide
{
    public class MvcApplication : System.Web.HttpApplication
    {

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


            JobManager.Initialize();
            // đồng bộ cuộc gọi 7 ngày trước
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var callLogService = new CallLogService();
                            await callLogService.SyncRecentlyAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(22, 40)
            );
            // đồng bộ cuộc gọi 7 ngày trước
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var callLogService = new CallLogService();
                            await callLogService.SyncRecentlyAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(12, 40)
            );
            // chuyển cuộc gọi giữa các nhân sự tháng (do cập nhật điều chuyển chậm)
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var callLogService = new CallLogService();
                            await callLogService.SyncDuplicateAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(13, 15)
            );
            // chuyển cuộc gọi giữa các nhân sự tháng (do cập nhật điều chuyển chậm)
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var callLogService = new CallLogService();
                            await callLogService.SyncDuplicateAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(23, 30)
            );

            // đồng bộ User
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var userService = new UserService();
                            await userService.SyncUserAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                //s => s.ToRunEvery(1).Days().At(2, 20)
                s => s.ToRunEvery(1).Days().At(3, 0)
            );
            // đồng bộ User
            JobManager.AddJob(
                () =>
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var userService = new UserService();
                            await userService.SyncUserAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(13, 25)
            );
            for (int h = 7; h < 24; h += 2)
            {
                // đồng bộ cuộc gọi ngày hôm nay
                JobManager.AddJob(
                    () =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var callLogService = new CallLogService();
                                await callLogService.SyncTodayAsync();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                            }
                        });
                    },
                    s => s.ToRunEvery(1).Days().At(h, 30)
                );
                // đồng bộ phiếu thu
                JobManager.AddJob(
                    () =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var phieuThuService = new PhieuThuService();
                                await phieuThuService.SyncPhieuThuAsync();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"✗ SyncPhieuThu error: {ex.Message}");
                            }
                        });
                    },
                    s => s.ToRunEvery(1).Days().At(h, 35)
                );
                // đồng bộ công nợ
                JobManager.AddJob(
                    () =>
                    {
                        Task.Run(async () =>
                        {
                            try
                            {
                                var congNoService = new CongNoService();
                                await congNoService.SyncCongNoAsync();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"✗ SyncCongNo error: {ex.Message}");
                            }
                        });
                    },
                    s => s.ToRunEvery(1).Days().At(h, 50)
                );
            }

            // đồng bộ User tháng trước
            JobManager.AddJob(
                () =>
                {
                    var today = DateTime.Now.Day;
                    if (today > 3)
                        return;
                    Task.Run(async () =>
                    {
                        try
                        {
                            var userService = new UserService();
                            await userService.SyncUserLastMonthAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(2, 30)
            );
            // đồng bộ User tháng trước
            JobManager.AddJob(
                () =>
                {
                    var today = DateTime.Now.Day;
                    if (today > 3)
                        return;
                    Task.Run(async () =>
                    {
                        try
                        {
                            var userService = new UserService();
                            await userService.SyncUserLastMonthAsync();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"✗ Timer error: {ex.Message}");
                        }
                    });
                },
                s => s.ToRunEvery(1).Days().At(13, 15)
            );

        }

    }
}

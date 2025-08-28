using Antlr.Runtime.Misc;
using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.Services.Description;
using Z.EntityFramework.Plus;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using static System.Data.Entity.Infrastructure.Design.Executor;
using System.Data.Entity;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using ImageResizer.ExtensionMethods;
using Microsoft.IdentityModel.Tokens;

namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class VcmsController : BaseController
    {
        //private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private IEnumerable<Admin> Admins => _unitOfWork.AdminRepository.Get();
        private RoleAdmin Role => (RoleAdmin)Enum.Parse(typeof(RoleAdmin), RouteData.Values["Role"].ToString());
        private string Fullname => RouteData.Values["Fullname"].ToString();
        public ConfigSite Config => (ConfigSite)HttpContext.Application["ConfigSite"];

        #region Admin
        public ActionResult Index(string roll = "")
        {
            ViewBag.Role = roll;
            var model = new InfoAdminViewModel
            {
                Admins = Admins,
                //Banners = Banners,
                //Products = Products,
                //Articles = Articles,
                //Contacts = ContactOffices,
            };
            return View(model);
        }

        public ActionResult ConfigSite(string result = "")
        {
            var model = _unitOfWork.ConfigSiteRepository.Get().FirstOrDefault();
            ViewBag.Result = result;
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult ConfigSite(ConfigSite config)
        {
            if (ModelState.IsValid)
            {
                if (!_unitOfWork.ConfigSiteRepository.Get().Any())
                {
                    _unitOfWork.ConfigSiteRepository.Insert(config);
                }
                else
                {
                    var model = _unitOfWork.ConfigSiteRepository.GetById(config.Id);
                    var file = Request.Files["Image"];
                    if (file != null && file.ContentLength > 0)
                    {
                        if (!HtmlHelpers.CheckFileExt(file.FileName, "jpg|jpeg|png|gif"))
                        {
                            ModelState.AddModelError("", @"Chỉ chấp nhận định dạng ảnh jpg|jpeg|png|gif");
                            return View(model);
                        }
                        if (file.ContentLength > 1024 * 1024 * 4)
                        {
                            ModelState.AddModelError("", @"Chỉ chấp nhận định dạng ảnh dung lượng dưới 4gb");
                            return View(model);
                        }
                        var imgFileName = HtmlHelpers.ConvertToUnSign(null, Path.GetFileNameWithoutExtension(file.FileName)) +
                        "-" + DateTime.Now.Millisecond + Path.GetExtension(file.FileName);
                        var imgPath = "/images/configs/" + DateTime.Now.ToString("yyyy/MM/dd");
                        HtmlHelpers.CreateFolder(Server.MapPath(imgPath));
                        var imgFile = DateTime.Now.ToString("yyyy/MM/dd") + "/" + imgFileName;
                        var newImage = Image.FromStream(file.InputStream);
                        var fixSizeImage = HtmlHelpers.FixedSize(newImage, 1000, 1000, false);
                        HtmlHelpers.SaveJpeg(Server.MapPath(Path.Combine(imgPath, imgFileName)), fixSizeImage, 90);
                        model.Image = imgFile;
                    }
                    var favicon = Request.Files["Favicon"];
                    if (favicon != null && favicon.ContentLength > 0)
                    {
                        if (!HtmlHelpers.CheckFileExt(favicon.FileName, "jpg|jpeg|png|gif"))
                        {
                            ModelState.AddModelError("", @"Chỉ chấp nhận định dạng ảnh jpg|jpeg|png|gif");
                            return View(model);
                        }
                        if (favicon.ContentLength > 1024 * 1024 * 4)
                        {
                            ModelState.AddModelError("", @"Chỉ chấp nhận định dạng ảnh dung lượng dưới 4gb");
                            return View(model);
                        }
                        var imgFileName = HtmlHelpers.ConvertToUnSign(null, Path.GetFileNameWithoutExtension(favicon.FileName)) +
                        "-" + DateTime.Now.Millisecond + Path.GetExtension(favicon.FileName);
                        var imgPath = "/images/configs/" + DateTime.Now.ToString("yyyy/MM/dd");
                        HtmlHelpers.CreateFolder(Server.MapPath(imgPath));
                        var imgFile = DateTime.Now.ToString("yyyy/MM/dd") + "/" + imgFileName;
                        var newImage = Image.FromStream(favicon.InputStream);
                        var fixSizeImage = HtmlHelpers.FixedSize(newImage, 1000, 1000, false);
                        HtmlHelpers.SaveJpeg(Server.MapPath(Path.Combine(imgPath, imgFileName)), fixSizeImage, 90);
                        model.Favicon = imgFile;
                    }
                    model.Title = config.Title;
                    model.Password = config.Password;
                    model.Slogan = config.Slogan;
                    model.Description = config.Description;
                    model.Place = config.Place;
                    model.Hotline = config.Hotline;
                    model.Email = config.Email;
                    model.Facebook = config.Facebook;
                    model.Instagram = config.Instagram;
                    model.Youtube = config.Youtube;
                    model.UrlMessenger = config.UrlMessenger;
                    model.TikTok = config.TikTok;
                    model.LiveChat = config.LiveChat;
                    model.GoogleMap = config.GoogleMap;
                    model.AboutFooter = config.AboutFooter;
                    //model.Agencies = config.Agencies;
                    _unitOfWork.Save();

                    HttpContext.Application["ConfigSite"] = model;
                }
                return RedirectToAction("ConfigSite", new { result = "success" });
            }

            return HttpNotFound();
        }
        [AllowAnonymous]
        public ActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginAdminViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var admin = _unitOfWork.AdminRepository.Get(a => a.Username == model.Username && a.Active).SingleOrDefault();
                if (admin != null && HtmlHelpers.VerifyHash(model.Password, "SHA256", admin.Password))
                {
                    //var ticket = new FormsAuthenticationTicket(1, model.Username.ToLower(), DateTime.Now, DateTime.Now.AddDays(30), true,
                    //    admin.ToString(), FormsAuthentication.FormsCookiePath);
                    var userData = $"{admin.RoleAdmin.ToString()}|{admin.Username}";
                    var ticket = new FormsAuthenticationTicket(1, model.Username.ToLower(), DateTime.Now, DateTime.Now.AddDays(30), true, userData, FormsAuthentication.FormsCookiePath);
                    //var ticket = new FormsAuthenticationTicket(1, model.Username.ToLower(), DateTime.Now, DateTime.Now.AddDays(30), true,
                    //    admin.RoleAdmin.ToString(), FormsAuthentication.FormsCookiePath);
                    var encTicket = FormsAuthentication.Encrypt(ticket);
                    // Create the cookie.
                    Response.Cookies.Add(new HttpCookie(FormsAuthentication.FormsCookieName, encTicket));
                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Vcms");
                }
                ModelState.AddModelError("", @"Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Vcms");
        }
        public ActionResult ChangePassword(int result = 0)
        {
            ViewBag.Result = result;
            return View();
        }
        [HttpPost]
        public ActionResult ChangePassWord(ChangePassWordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = Admins.FirstOrDefault(z => z.Username.Equals(User.Identity.Name, StringComparison.OrdinalIgnoreCase));
                if (admin != null)
                {
                    if (HtmlHelpers.VerifyHash(model.OldPassword, "SHA256", admin.Password))
                    {
                        admin.Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null);
                        _unitOfWork.Save();
                        return RedirectToAction("ChangePassword", new { result = 1 });
                    }
                    ModelState.AddModelError("", @"Mật khẩu hiện tại không đúng, vui lòng nhập lại");
                }
                else
                {
                    return HttpNotFound();
                }
            }
            return View();
        }
        public PartialViewResult ListAdmin()
        {
            var model = Admins;
            return PartialView(model);
        }
        public ActionResult CreateAdmin(string result = "")
        {
            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            ViewBag.Result = result;
            var model = new CreateAdminViewModel
            {
                Admins = Admins,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateAdmin(CreateAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var exist = Admins.Any(z => z.Username.Equals(model.Username));
                if (exist)
                {
                    ModelState.AddModelError("", @"Tên đăng nhập này đã tồn tại");
                    return View();
                }
                else
                {
                    var m = new Admin
                    {
                        Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null),
                        Username = model.Username,
                        Active = model.Active,
                        RoleAdmin = model.RoleAdmin,
                    };
                    _unitOfWork.AdminRepository.Insert(m);
                    _unitOfWork.Save();
                    return RedirectToAction("CreateAdmin", new { result = "add" });
                }
            }
            else
            {
                return HttpNotFound();
            }
        }

        [AllowAnonymous]
        public ActionResult CreateAdmin2()
        {
            if (!Admins.Any(a => a.Username == "admin"))
            {
                var m = new Admin
                {
                    Password = HtmlHelpers.ComputeHash("vico@123", "SHA256", null),
                    Username = "admin",
                    Active = true,
                };
                _unitOfWork.AdminRepository.Insert(m);
                _unitOfWork.Save();
            }

            return RedirectToAction("Login", new { result = "add" });

        }
        public ActionResult UpdateAdmin(int id)
        {
            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            var model = new CreateAdminViewModel
            {
                Admins = Admins.Where(z => z.Id == id),
                RoleAdmin = Admins.FirstOrDefault(z => z.Id == id)?.RoleAdmin ?? RoleAdmin.Admin,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateAdmin(CreateAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = Admins.Where(z => z.Username == model.Username).FirstOrDefault();
                if (admin != null)
                {
                    admin.Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null);
                    admin.Active = model.Active;
                    admin.RoleAdmin = model.RoleAdmin;
                    _unitOfWork.Save();
                    return RedirectToAction("CreateAdmin", new { result = "update" });
                }
                else
                {
                    return HttpNotFound();
                }
            }
            else
            {
                return HttpNotFound();
            }
        }
        [HttpPost]
        public JsonResult DeleteAdmin(string username)
        {
            if (Admins.Count() > 1)
            {
                var admin = Admins.Where(z => z.Username == username).FirstOrDefault();
                _unitOfWork.AdminRepository.Delete(admin);
                _unitOfWork.Save();
                return Json(new { status = true, msg = "Xóa quản trị viên thành công" });
            }
            else
            {
                return Json(new { status = false, msg = "Phải có ít nhất một quản trị viên" });
            }

        }
        #endregion

        #region User
        public ActionResult CreateCV()
        {
            if (!_unitOfWork.UserRepository.GetQuery(a => a.Username == "testCV").Any())
            {
                var zones = _unitOfWork.ZoneRepository.Get();
                var office = _unitOfWork.OfficeRepository.GetQuery(a => a.Name.Contains("Nguyễn Trãi")).FirstOrDefault();

                if (zones.Any() && office != null)
                {
                    var zoneIds = ",";
                    foreach (var item in zones)
                    {
                        zoneIds += item.Id + ",";
                    }

                    var m = new User
                    {
                        Password = HtmlHelpers.ComputeHash("vico@123", "SHA256", null),
                        Username = "testCV",
                        TypeUser = TypeUser.CV,
                        ZoneIds = zoneIds,
                        OfficeId = office.Id,
                        Active = true,
                        SaleKit = false,
                        Fullname = "Test Chuyên Viên"
                    };
                    _unitOfWork.UserRepository.Insert(m);
                    _unitOfWork.Save();
                }

            }

            return RedirectToAction("ListUser");
        }
        //public ActionResult CreateTarget(string result = "")
        //{
        //    ViewBag.Result = result;
        //    var model = new CreateTargetViewModel
        //    {
        //        SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.TTL), "Id", "Username"),
        //        Revenue = new RevenueUser_Month()
        //        {
        //            Month = DateTime.Now.Month,
        //            Year = DateTime.Now.Year,
        //        }
        //    };
        //    return View(model);
        //}
        //[HttpPost]
        //public ActionResult CreateTarget(CreateTargetViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        //var m = new RevenueUser_Month
        //        //{
        //        //    Target = model.Revenue.Target,
        //        //    UserId = model.Revenue.UserId,
        //        //    Year = model.Revenue.Year,
        //        //    Month = model.Revenue.Month,
        //        //};
        //        _unitOfWork.RevenueUser_MonthRepository.Insert(model.Revenue);
        //        _unitOfWork.Save();
        //        //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");

        //        return RedirectToAction("CreateTarget", new { result = "add" });

        //    }
        //    else
        //    {
        //        return HttpNotFound();
        //    }
        //}
        public ActionResult CreateUser(string result = "")
        {

            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            ViewBag.Result = result;
            var model = new CreateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active)
                //Users = Users,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateUser(CreateUserViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                var exist = _unitOfWork.UserRepository.GetQuery().Any(z => z.Username.Equals(model.Username));
                if (exist)
                {
                    ModelState.AddModelError("", @"Tên đăng nhập này đã tồn tại");
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");
                    model.SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name");
                    return View(model);
                }
                var exist2 = _unitOfWork.UserRepository.GetQuery().Any(z => !string.IsNullOrEmpty(model.MaNhanVien) && z.MaNhanVien.Equals(model.MaNhanVien));
                if (exist2)
                {
                    ModelState.AddModelError("", @"Đã tồn tại nhân sự có mã nhân viên " + model.MaNhanVien);
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");
                    model.SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name");
                    return View(model);
                }
                else
                {
                    var catIds = fc.GetValues("CatIDs");
                    if (catIds != null)
                    {
                        foreach (var item in catIds)
                        {
                            model.OfficeIds += (item + ",");
                            var office = _unitOfWork.OfficeRepository.GetById(int.Parse(item));
                            if (office != null)
                                model.OfficeNames += office.ShortCode + ",";
                        }
                        model.OfficeIds = "," + model.OfficeIds;
                        model.OfficeNames = model.OfficeNames.Trim(',');

                    }
                    _unitOfWork.Save();
                    var m = new User
                    {
                        Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null),
                        Username = model.Username,
                        Fullname = model.Fullname,
                        MaNhanVien = model.MaNhanVien,
                        OfficeId = model.OfficeId,
                        ZoneId = model.ZoneId,
                        Active = model.Active,
                        SaleKit = model.SaleKit,
                        TypeUser = model.TypeUser,
                        OfficeIds = model.OfficeIds,
                        OfficeNames = model.OfficeNames,
                    };
                    _unitOfWork.UserRepository.Insert(m);
                    _unitOfWork.Save();
                    //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");

                    return RedirectToAction("ListUser", new { result = "add" });
                }
            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult ListUser(int? page, string username, int? zoneId, int? officeId, int? UserType, int? trung, int? active, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var users = _unitOfWork.UserRepository.GetQuery(orderBy: l => l.OrderByDescending(a => a.Id));
            if (zoneId.HasValue)
            {
                users = users.Where(l => l.Office != null && l.Office.ZoneId == zoneId);
            }

            if (officeId.HasValue)
            {
                users = users.Where(l => l.OfficeId == officeId);
            }
            if (UserType.HasValue)
            {
                users = users.Where(l => (int)l.TypeUser == UserType);
            }
            if (active == 1)
            {
                users = users.Where(l => l.Active);
            }
            if (active == 2)
            {
                users = users.Where(l => !l.Active);
            }
            if (username != null)
            {
                var newkey = username.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    users = users.Where(l => l.Username.Contains(newkey) || l.Fullname.Contains(newkey) || l.MaNhanVien.Contains(newkey));
                }
            }
            if (trung == 1)
            {
                var duplicatedMaNhanViens = users
                    .GroupBy(u => u.MaNhanVien)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                users = users.Where(u => duplicatedMaNhanViens.Contains(u.MaNhanVien) && u.MaNhanVien != null);
            }


            var model = new ListUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Users = users.ToPagedList(pageNumber, pageSize),
                officeId = officeId,

                ZoneId = zoneId,
                Username = username,
                active = active,
                TypeUser = UserType,
                MemberCredentials = _unitOfWork.MemberCredentialRepository.GetQuery(),
            };

            if (model.ZoneId > 0)
            {
                model.SelectOffices = OfficeSelectList(model.ZoneId);
            }
            return View(model);
        }
        public ActionResult ListFaceId(int userId)
        {
            var model = _unitOfWork.MemberCredentialRepository.Get(a => a.UserId == userId);
            return View(model);
        }
        public ActionResult UpdateMaNhanVien()
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => string.IsNullOrEmpty(a.MaNhanVien) && a.TypeUser != null);
            foreach (var u in users)
            {
                u.MaNhanVien = u.Username;
            }
            _unitOfWork.Save();
            return RedirectToAction("ListUser");
        }
        public ActionResult UpdateUser(int id)
        {
            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            var users = _unitOfWork.UserRepository.Get(z => z.Id == id);
            var user = users.FirstOrDefault();
            if (user == null)
                return RedirectToAction("ListUser");
            var model = new UpdateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Users = users,
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active)
            };

            if (!string.IsNullOrEmpty(user.OfficeIds))
            {
                model.CatIds = user.OfficeIds
                                        .Split(',')
                                        .Select(x =>
                                        {
                                            int.TryParse(x, out int value);
                                            return value;
                                        })
                                        .ToList();
            }
            model.OfficeIds = user.OfficeIds;
            var zId = user.ZoneId;
            if (zId != null)
                model.SelectOffices = OfficeSelectList(zId);

            model.OfficeId = user.OfficeId ?? 0;
            model.ZoneId = user.ZoneId ?? 0;
            model.TypeUser = user.TypeUser ?? null;
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateUser(UpdateUserViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                var catIds = fc.GetValues("CatIDs");
                if (catIds != null)
                {
                    foreach (var item in catIds)
                    {
                        model.OfficeIds += (item + ",");
                        var office = _unitOfWork.OfficeRepository.GetById(int.Parse(item));
                        if (office != null)
                            model.OfficeNames += office.ShortCode + ",";
                    }
                    model.OfficeIds = "," + model.OfficeIds;
                    model.OfficeNames = model.OfficeNames.Trim(',');

                }
                var user = _unitOfWork.UserRepository.GetQuery(z => z.Username == model.Username).FirstOrDefault();
                if (user != null)
                {
                    if (model.Password != null)
                        user.Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null);
                    user.OfficeId = model.OfficeId;
                    user.ZoneId = model.ZoneId;
                    user.Active = model.Active;
                    user.SaleKit = model.SaleKit;
                    user.TypeUser = model.TypeUser;
                    user.Fullname = model.Fullname;
                    user.MaNhanVien = model.MaNhanVien;
                    user.OfficeIds = model.OfficeIds;
                    user.OfficeNames = model.OfficeNames;
                    _unitOfWork.Save();
                    return RedirectToAction("ListUser", new { result = "update" });
                }
            }
            return HttpNotFound();

        }
        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            if (Role != RoleAdmin.Admin)
                return Json(new { status = false, msg = "Bạn không có quyền xóa" });
            var user = _unitOfWork.UserRepository.GetById(userId);
            _unitOfWork.UserRepository.Delete(user);
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa tài khoản thành công" });

        }
        public ActionResult ChangeZoneIdsASM()
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.ASM && a.ZoneId != null);
            var zones = _unitOfWork.ZoneRepository.GetQuery();
            foreach (var item in users)
            {
                var zone = zones.FirstOrDefault(a => a.Id == item.ZoneId);
                if (!string.IsNullOrEmpty(zone?.ShortCode))
                    item.ZoneIds = "," + zone.ShortCode + ",";
            }
            _unitOfWork.Save();
            return Content("Chuyển ZoneIds thành công");
        }
        public ActionResult ChangeOfficeIdsUser()
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => !string.IsNullOrEmpty(a.OfficeIds));
            foreach (var item in users)
            {
                item.OfficeIds = null;
            }
            _unitOfWork.Save();
            return Content("ChangeOfficeIdsUser null thành công");
        }
        public ActionResult ChangeOfficeIdsBM()
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.BM && a.OfficeId != null);
            var offices = _unitOfWork.OfficeRepository.GetQuery();
            foreach (var item in users)
            {
                var office = offices.FirstOrDefault(a => a.Id == item.OfficeId);
                item.OfficeIds = "," + office.Id + ",";
            }
            _unitOfWork.Save();
            return Content("Chuyển OfficeIds thành công");
        }
        public ActionResult InsertUserExcel()
        {
            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            return View();
        }
        [HttpPost]
        public ActionResult InsertUserExcel(FormCollection fc)
        {
            var file = Request.Files["UserFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type5,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var users = _unitOfWork.UserRepository.GetQuery();
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officecode = tbl.Rows[i][0].ToString().Trim();
                    var offices = _unitOfWork.OfficeRepository.GetQuery();
                    Office office = null;
                    if (!string.IsNullOrEmpty(officecode))
                    {
                        office = offices.Where(a => a.ShortCode == officecode).FirstOrDefault();
                        if (office == null) continue;
                    }
                    var username = tbl.Rows[i][4].ToString().Trim();
                    if (username == "") continue;
                    var user = users.Where(a => a.Username == username).FirstOrDefault();
                    //var password = tbl.Rows[i][5].ToString().Trim();
                    //if (password == "") continue;
                    var password2 = HtmlHelpers.ComputeHash(Config.Password ?? "AUG2025@#", "SHA256", null);
                    var fullname = tbl.Rows[i][6].ToString().Trim();
                    var maxnhanvien = tbl.Rows[i][7].ToString().Trim();
                    var phanquyen = tbl.Rows[i][9].ToString().Trim();
                    var zones = tbl.Rows[i][10].ToString().Trim();
                    var salekit = tbl.Rows[i][11].ToString().Trim();
                    if (user != null)
                    {
                        //user.Password = password2;
                        user.Active = true;
                        user.OfficeId = office?.Id ?? null;
                        user.Fullname = fullname;
                        user.SaleKit = (salekit == "1" ? true : false);
                        switch (phanquyen)
                        {
                            case "ASM":
                                user.TypeUser = TypeUser.ASM;
                                user.ZoneIds = "," + zones + ",";
                                var zonef = zones.Split(',')[0];
                                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zonef).FirstOrDefault();
                                if (z != null)
                                {
                                    user.ZoneId = z.Id;
                                    user.Zone = z;
                                }
                                break;
                            case "GĐTS":
                                user.TypeUser = TypeUser.HO;
                                break;
                            case "EC":
                                if (!string.IsNullOrEmpty(zones))
                                {
                                    user.OfficeIds = ",";
                                    user.OfficeNames = "";
                                    foreach (var item in zones.Split(','))
                                    {
                                        var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                        if (o != null)
                                        {
                                            user.OfficeIds += o.Id + ",";
                                            user.OfficeNames += o.ShortCode + ",";
                                        }
                                    }
                                    user.OfficeNames = user.OfficeNames.Trim(',');
                                }
                                user.TypeUser = TypeUser.EC;
                                break;
                            case "BM":
                                if (!string.IsNullOrEmpty(zones))
                                {
                                    user.OfficeIds = ",";
                                    user.OfficeNames = "";
                                    foreach (var item in zones.Split(','))
                                    {
                                        var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                        if (o != null)
                                        {
                                            user.OfficeIds += o.Id + ",";
                                            user.OfficeNames += o.ShortCode + ",";
                                        }
                                    }
                                    user.OfficeNames = user.OfficeNames.Trim(',');
                                }

                                user.TypeUser = TypeUser.BM;
                                break;
                            case "BSA":
                                user.TypeUser = TypeUser.SAB;
                                break;
                            case "SAB":
                                user.TypeUser = TypeUser.SAB;
                                break;
                            case "ATL":
                                user.TypeUser = TypeUser.ALT;
                                break;
                            case "CM":
                                user.TypeUser = TypeUser.CM;
                                break;
                            case "TTL":
                                user.TypeUser = TypeUser.TTL;
                                break;
                            case "Chuyên viên":
                                user.TypeUser = TypeUser.CV;
                                user.ZoneIds = "," + zones + ",";
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(maxnhanvien))
                        {
                            var u = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == maxnhanvien).FirstOrDefault();
                            if (u != null) continue;
                        }
                        user = new User
                        {
                            Username = username,
                            Password = password2,
                            Active = true,
                            OfficeId = office?.Id ?? null,
                            MaNhanVien = maxnhanvien,
                            Fullname = fullname,
                            SaleKit = (salekit == "1" ? true : false),
                        };
                        switch (phanquyen)
                        {
                            case "ASM":
                                user.TypeUser = TypeUser.ASM;
                                user.ZoneIds = "," + zones + ",";
                                var zonef = zones.Split(',')[0];
                                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zonef).FirstOrDefault();
                                if (z != null)
                                {
                                    user.ZoneId = z.Id;
                                    user.Zone = z;
                                }

                                break;
                            case "GĐTS":
                                user.TypeUser = TypeUser.HO;
                                break;
                            case "EC":
                                if (!string.IsNullOrEmpty(zones))
                                {
                                    user.OfficeIds = ",";
                                    user.OfficeNames = "";
                                    foreach (var item in zones.Split(','))
                                    {
                                        var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                        if (o != null)
                                        {
                                            user.OfficeIds += o.Id + ",";
                                            user.OfficeNames += o.ShortCode + ",";
                                        }
                                    }
                                    user.OfficeNames = user.OfficeNames.Trim(',');
                                }
                                user.TypeUser = TypeUser.EC;
                                break;
                            case "BM":
                                if (!string.IsNullOrEmpty(zones))
                                {
                                    user.OfficeIds = ",";
                                    user.OfficeNames = "";
                                    foreach (var item in zones.Split(','))
                                    {
                                        var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                        if (o != null)
                                        {
                                            user.OfficeIds += o.Id + ",";
                                            user.OfficeNames += o.ShortCode + ",";
                                        }
                                    }
                                    user.OfficeNames = user.OfficeNames.Trim(',');
                                }
                                user.TypeUser = TypeUser.BM;
                                break;
                            case "BSA":
                                user.TypeUser = TypeUser.SAB;
                                break;
                            case "ATL":
                                user.TypeUser = TypeUser.ALT;
                                break;
                            case "CM":
                                user.TypeUser = TypeUser.CM;
                                break;
                            case "TTL":
                                user.TypeUser = TypeUser.TTL;
                                break;
                            case "Chuyên viên":
                                user.TypeUser = TypeUser.CV;
                                user.ZoneIds = "," + zones + ",";
                                break;
                            default:
                                break;
                        }
                        _unitOfWork.UserRepository.Insert(user);
                    }
                }
                _unitOfWork.Save();
            }
            return RedirectToAction("ListUser");
        }

        public ActionResult InsertHistoryUser()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertHistoryUser(FormCollection fc)
        {
            var file = Request.Files["UserFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type6,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl2 = result.Tables[0];
                var historyUserList = new List<HistoryUser>();
                var reportDataList = new List<ReportData>();
                var monthStr = tbl2.Rows[1][8].ToString().Trim();
                if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt))
                {
                    ModelState.AddModelError("", @"Kiểm tra lại cột tháng");
                    return View();
                }

                var yearStr = tbl2.Rows[1][9].ToString().Trim();
                if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt))
                {
                    ModelState.AddModelError("", @"Kiểm tra lại cột năm");
                    return View();
                }
                var reportCallOffices = _unitOfWork.ReportDataRepository.GetQuery(a => a.Active && a.Month == monthInt && a.Year == yearInt && (a.ReportCategoryId == 26 || a.ReportCategoryId == 27));
                foreach (var item in reportCallOffices)
                {
                    item.Data = "0";
                }
                for (var i = 1; i < tbl2.Rows.Count; i++)
                {
                    var manhanvien = tbl2.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository
                        .GetQuery(a => a.MaNhanVien == manhanvien)
                        .FirstOrDefault();

                    var officeShortName = tbl2.Rows[i][1].ToString().Trim();
                    var office = _unitOfWork.OfficeRepository
                        .GetQuery(a => a.ShortName == officeShortName)
                        .FirstOrDefault();
                    //if (office == null)
                    //    continue;

                    var typeUser = tbl2.Rows[i][10].ToString().Trim();
                    if (string.IsNullOrEmpty(typeUser))
                        continue;
                    TypeUser type = new TypeUser();
                    switch (typeUser)
                    {
                        case "ASM":
                            type = TypeUser.ASM;
                            break;
                        case "GĐTS":
                            type = TypeUser.HO;
                            break;
                        case "EC":
                            type = TypeUser.EC;
                            break;
                        case "BM":
                            type = TypeUser.BM;
                            break;
                        case "BSA":
                            type = TypeUser.SAB;
                            break;
                        case "SAB":
                            type = TypeUser.SAB;
                            break;
                        case "ATL":
                            type = TypeUser.ALT;
                            break;
                        case "CM":
                            type = TypeUser.CM;
                            break;
                        case "TTL":
                            type = TypeUser.TTL;
                            break;
                        case "Chuyên viên":
                            user.TypeUser = TypeUser.CV;
                            break;
                        default:
                            break;
                    }


                    var status = tbl2.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(status))
                        continue;

                    StatusUser statusUser = new StatusUser();
                    switch (status)
                    {
                        case "Đang làm việc":
                            statusUser = StatusUser.Active;
                            break;
                        case "Nghỉ thai sản":
                            statusUser = StatusUser.InActive;
                            break;
                        case "Đã nghỉ":
                            statusUser = StatusUser.InActive;
                            break;
                        case "Nghỉ việc":
                            statusUser = StatusUser.InActive;
                            break;
                        case "Miễn nhiệm":
                            statusUser = StatusUser.InActive;
                            break;
                        case "Điều chuyển":
                            statusUser = StatusUser.Transfer;
                            break;
                        case "Bổ nhiệm":
                            statusUser = StatusUser.Transfer;
                            break;
                        default:
                            break;
                    }
                    var password = HtmlHelpers.ComputeHash(Config.Password ?? "AUG2025@#", "SHA256", null);
                    var fullname = tbl2.Rows[i][3].ToString().Trim();
                    var zones = tbl2.Rows[i][12].ToString().Trim();
                    var sort = tbl2.Rows[i][11].ToString().Trim();
                    if (user == null)
                    {
                        if (statusUser == StatusUser.Active)
                        {
                            var newUser = new User
                            {
                                Username = manhanvien,
                                MaNhanVien = manhanvien,
                                Password = password,
                                Active = true,
                                OfficeId = office?.Id,
                                Fullname = fullname,
                                SaleKit = true,
                                TypeUser = type,
                                //ZoneIds = type == TypeUser.CV ? "," + zones + "," : null,
                            };
                            switch (type)
                            {
                                case TypeUser.ASM:
                                    newUser.ZoneIds = "," + zones + ",";
                                    var zonef = zones.Split(',')[0];
                                    var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zonef).FirstOrDefault();
                                    if (z != null)
                                    {
                                        newUser.ZoneId = z.Id;
                                        newUser.Zone = z;
                                    }
                                    break;
                                case TypeUser.EC:
                                    if (!string.IsNullOrEmpty(zones))
                                    {
                                        newUser.OfficeIds = ",";
                                        newUser.OfficeNames = "";
                                        foreach (var item in zones.Split(','))
                                        {
                                            var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                            if (o != null)
                                            {
                                                newUser.OfficeIds += o.Id + ",";
                                                newUser.OfficeNames += o.ShortCode + ",";
                                            }
                                        }
                                        newUser.OfficeNames = newUser.OfficeNames.Trim(',');
                                    }
                                    break;
                                case TypeUser.BM:
                                    if (!string.IsNullOrEmpty(zones))
                                    {
                                        newUser.OfficeIds = ",";
                                        newUser.OfficeNames = "";
                                        foreach (var item in zones.Split(','))
                                        {
                                            var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                            if (o != null)
                                            {
                                                newUser.OfficeIds += o.Id + ",";
                                                newUser.OfficeNames += o.ShortCode + ",";
                                            }
                                        }
                                        newUser.OfficeNames = newUser.OfficeNames.Trim(',');
                                    }

                                    break;
                                case TypeUser.CV:
                                    user.ZoneIds = "," + zones + ",";
                                    break;
                                default:
                                    break;
                            }

                            try
                            {
                                _unitOfWork.UserRepository.Insert(newUser);
                                _unitOfWork.Save();
                                user = newUser;
                            }
                            catch (Exception e)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (statusUser == StatusUser.Active)
                        {
                            user.Active = true;
                            user.SaleKit = true;
                            if (user.OfficeId != office?.Id || user.TypeUser != type)
                            {
                                user.OfficeId = office?.Id;
                                user.TypeUser = type;
                                try
                                {
                                    _unitOfWork.Save();
                                }
                                catch (Exception e)
                                {
                                    continue;
                                }
                            }

                            switch (type)
                            {
                                case TypeUser.ASM:
                                    user.ZoneIds = "," + zones + ",";
                                    var zonef = zones.Split(',')[0];
                                    var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zonef).FirstOrDefault();
                                    if (z != null)
                                    {
                                        user.ZoneId = z.Id;
                                        user.Zone = z;
                                    }
                                    break;
                                case TypeUser.EC:
                                    if (!string.IsNullOrEmpty(zones))
                                    {
                                        user.OfficeIds = ",";
                                        user.OfficeNames = "";
                                        foreach (var item in zones.Split(','))
                                        {
                                            var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                            if (o != null)
                                            {
                                                user.OfficeIds += o.Id + ",";
                                                user.OfficeNames += o.ShortCode + ",";
                                            }
                                        }
                                        user.OfficeNames = user.OfficeNames.Trim(',');
                                    }
                                    break;
                                case TypeUser.BM:
                                    if (!string.IsNullOrEmpty(zones))
                                    {
                                        user.OfficeIds = ",";
                                        user.OfficeNames = "";
                                        foreach (var item in zones.Split(','))
                                        {
                                            var o = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item && a.Active).FirstOrDefault();
                                            if (o != null)
                                            {
                                                user.OfficeIds += o.Id + ",";
                                                user.OfficeNames += o.ShortCode + ",";
                                            }
                                        }
                                        user.OfficeNames = user.OfficeNames.Trim(',');
                                    }

                                    break;
                                case TypeUser.CV:
                                    user.ZoneIds = "," + zones + ",";
                                    break;
                                default:
                                    break;
                            }
                        }
                        else if (statusUser == StatusUser.InActive)
                        {
                            user.Active = false;
                            try
                            {
                                _unitOfWork.Save();
                            }
                            catch (Exception e)
                            {
                                continue;
                            }
                        }
                    }

                    var dayStart = tbl2.Rows[i][6].ToString().Trim().Replace("'", "");
                    if (string.IsNullOrEmpty(dayStart))
                        continue;
                    var dayEnd = tbl2.Rows[i][7].ToString().Trim().Replace("'", "");
                    var startDate = new DateTime();
                    var endDate = new DateTime();

                    if (DateTime.TryParse(dayStart, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                        startDate = new DateTime(cd.Year, cd.Month, cd.Day, 0, 0, 0);
                    else
                        continue;
                    if (!string.IsNullOrEmpty(dayEnd))
                        if (DateTime.TryParse(dayEnd, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd2))
                            endDate = new DateTime(cd2.Year, cd2.Month, cd2.Day, 0, 0, 0);
                        else
                            continue;

                    //var monthStr = tbl2.Rows[i][8].ToString().Trim();
                    //if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt))
                    //    continue;

                    //var yearStr = tbl2.Rows[i][9].ToString().Trim();
                    //if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt))
                    //    continue;


                    var query = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == user.Id && a.Month == monthInt && a.Year == yearInt && a.TypeUser == type && a.DayStart == startDate);
                    if (office != null)
                    {
                        query = query.Where(a => a.OfficeId == office.Id);
                    }
                    else
                    {
                        query = query.Where(a => a.OfficeId == null);
                    }
                    var historyUser = query.FirstOrDefault();

                    if (historyUser != null)
                    {
                        historyUser.Status = statusUser;
                        //historyUser.DayStart = startDate;
                        if (!string.IsNullOrEmpty(dayEnd))
                            historyUser.DayEnd = endDate;
                        if (!string.IsNullOrEmpty(sort))
                            historyUser.Sort = int.Parse(sort);
                        //Tính chỉ tiêu - TĐ - HT cuộc gọi
                        if (office != null)
                        {
                            // cuộc gọi thực đạt
                            var countTD = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.CallDate.Year == yearInt && a.CallDate.Month == monthInt && a.BillSec >= 60).Count();

                            if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT || countTD > 0)
                            {
                                var reportDataCallTD = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 100).FirstOrDefault();
                                if (reportDataCallTD == null)
                                {
                                    reportDataCallTD = new ReportData()
                                    {
                                        Data = countTD.ToString("N0"),
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 100,
                                        OfficeId = office.Id,
                                        Sort = 19,
                                    };
                                    reportDataList.Add(reportDataCallTD);
                                }
                                else
                                {
                                    reportDataCallTD.Data = countTD.ToString("N0");
                                }
                            }

                            if (historyUser.TypeUser == TypeUser.EC || historyUser.TypeUser == TypeUser.ALT)
                            {
                                HistoryUser oldPosittion = null;
                                var startDateReal = historyUser.DayStart;
                                if (historyUser.Status == StatusUser.Active)
                                {
                                    oldPosittion = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == historyUser.UserId && a.Month == monthInt && a.Year == yearInt && a.Status == StatusUser.Transfer, q => q.OrderByDescending(a => a.DayEnd)).FirstOrDefault();
                                    if (oldPosittion != null)
                                    {
                                        if (oldPosittion.DayEnd == null)
                                        {
                                            ModelState.AddModelError("", @"Nhân sự điều chuyển " + oldPosittion.User.MaNhanVien + " không có ngày điều chuyển");
                                            return View();
                                        }
                                        startDateReal = oldPosittion.DayEnd.Value;
                                    }
                                }
                                var workingDay = _unitOfWork.WorkingDayRepository.GetQuery(a => a.Year == yearInt).FirstOrDefault();
                                if (workingDay == null)
                                {
                                    ModelState.AddModelError("", @"Chưa có dữ liệu bảng số ngày công năm " + yearInt);
                                    return View();
                                }
                                int workingDayFull = 1;

                                switch (monthInt)
                                {
                                    case 1:
                                        workingDayFull = workingDay.WorkingDayMonth1;
                                        break;
                                    case 2:
                                        workingDayFull = workingDay.WorkingDayMonth2;
                                        break;
                                    case 3:
                                        workingDayFull = workingDay.WorkingDayMonth3;
                                        break;
                                    case 4:
                                        workingDayFull = workingDay.WorkingDayMonth4;
                                        break;
                                    case 5:
                                        workingDayFull = workingDay.WorkingDayMonth5;
                                        break;
                                    case 6:
                                        workingDayFull = workingDay.WorkingDayMonth6;
                                        break;
                                    case 7:
                                        workingDayFull = workingDay.WorkingDayMonth7;
                                        break;
                                    case 8:
                                        workingDayFull = workingDay.WorkingDayMonth8;
                                        break;
                                    case 9:
                                        workingDayFull = workingDay.WorkingDayMonth9;
                                        break;
                                    case 10:
                                        workingDayFull = workingDay.WorkingDayMonth10;
                                        break;
                                    case 11:
                                        workingDayFull = workingDay.WorkingDayMonth11;
                                        break;
                                    case 12:
                                        workingDayFull = workingDay.WorkingDayMonth12;
                                        break;
                                    default:
                                        break;
                                }
                                int workingDayTT = 0;
                                int lastMonth = 0;
                                int yearLastMonth = 0;
                                if (monthInt == 1)
                                {
                                    lastMonth = 12;
                                    yearLastMonth = yearInt - 1;
                                }
                                else
                                {
                                    lastMonth = monthInt - 1;
                                    yearLastMonth = yearInt;
                                }
                                DateTime endDayLastMonth = new DateTime(yearLastMonth, lastMonth, DateTime.DaysInMonth(yearLastMonth, lastMonth));
                                if ((startDateReal.Year < yearInt || (startDateReal.Year == yearInt && startDateReal.Month < monthInt)) && (historyUser.DayEnd == null || (historyUser.DayEnd != null && historyUser.DayEnd.Value.Month > monthInt)))
                                {
                                    workingDayTT = workingDayFull;
                                }
                                else
                                {
                                    DateTime ngayBatDau = historyUser.DayStart.Year < yearInt || (historyUser.DayStart.Year == yearInt && historyUser.DayStart.Month < monthInt) ? new DateTime(yearInt, monthInt, 1) : historyUser.DayStart;
                                    if (oldPosittion != null)
                                    {
                                        var oldDayFull = (oldPosittion.DayEnd.Value - ngayBatDau).Days;
                                        var oldDayWork = oldDayFull - (oldDayFull / 7);
                                        workingDayTT = Math.Max(workingDayFull - oldDayWork, 0);
                                    }
                                    else
                                    {
                                        DateTime ngayKetThuc = historyUser.DayEnd != null ? historyUser.DayEnd.Value.AddDays(-1) : new DateTime(yearInt, monthInt, DateTime.DaysInMonth(yearInt, monthInt));
                                        int soNgayLamViec = (ngayKetThuc - ngayBatDau).Days + 1;
                                        if (soNgayLamViec < 0)
                                        {
                                            ModelState.AddModelError("", @"Nhân viên " + historyUser.User.MaNhanVien + " có ngày vào làm > ngày nghỉ việc");
                                            return View();
                                        }
                                        int soNgayNghi = soNgayLamViec / 7;
                                        workingDayTT = Math.Min(soNgayLamViec - soNgayNghi, workingDayFull);
                                    }

                                }
                                int callTarget = 0;
                                if (historyUser.DayStart.Month == monthInt || historyUser.DayStart.Month == lastMonth)
                                {
                                    //Số ngày làm việc tháng trước
                                    var dayFree = (endDayLastMonth - historyUser.DayStart).Days + 1;
                                    if (dayFree < 0)
                                        dayFree = 0;
                                    if (dayFree <= 5)
                                    {
                                        workingDayTT = Math.Max(0, workingDayTT - (5 - dayFree));
                                    }
                                }
                                callTarget = 12 * workingDayTT;
                                // Chỉ tiêu báo cáo nhân sự
                                var reportDataCall = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 99).FirstOrDefault();
                                if (reportDataCall == null)
                                {
                                    reportDataCall = new ReportData()
                                    {
                                        Data = callTarget.ToString("N0"),
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 99,
                                        //check null
                                        OfficeId = office.Id,
                                        Sort = 18,
                                    };
                                    reportDataList.Add(reportDataCall);

                                }
                                else
                                {
                                    reportDataCall.Data = callTarget.ToString("N0");
                                }

                                // % Hoàn thành
                                var ht = ((double)countTD / callTarget * 100).ToString("F2") + "%";
                                var reportDataCallHT = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == historyUser.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 101).FirstOrDefault();
                                if (reportDataCallHT == null)
                                {
                                    reportDataCallHT = new ReportData()
                                    {
                                        Data = ht,
                                        UserId = historyUser.UserId,
                                        HistoryUserId = historyUser.Id,
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 100,
                                        OfficeId = office.Id,
                                        Sort = 20,
                                    };
                                    reportDataList.Add(reportDataCallHT);
                                }
                                else
                                {
                                    reportDataCallHT.Data = ht;
                                }

                                //Chỉ tiêu DS - thực đạt chi nhánh
                                var reportCallOfficeTarget = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                                var reportCallOfficeTD = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                                if (reportCallOfficeTarget == null)
                                {
                                    reportCallOfficeTarget = new ReportData()
                                    {
                                        Data = callTarget.ToString("N0"),
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 26,
                                        //check null
                                        OfficeId = office.Id,
                                        Sort = 7,
                                    };
                                    reportDataList.Add(reportCallOfficeTarget);

                                }
                                else
                                {
                                    int oldData;
                                    var cleanedData = reportCallOfficeTarget.Data.Replace(",", "").Replace(".", "");

                                    if (!int.TryParse(cleanedData, out oldData))
                                    {
                                        ModelState.AddModelError("", @"Có lỗi xảy ra. Mã lỗi: Error 3947bn06");
                                        return View();
                                    }
                                    int newData = oldData + callTarget;
                                    reportCallOfficeTarget.Data = newData.ToString("N0");
                                }
                                if (reportCallOfficeTD == null)
                                {
                                    reportCallOfficeTD = new ReportData()
                                    {
                                        Data = countTD.ToString("N0"),
                                        Month = monthInt,
                                        Year = yearInt,
                                        ReportCategoryId = 27,
                                        //check null
                                        OfficeId = office.Id,
                                        Sort = 8,
                                    };
                                    reportDataList.Add(reportCallOfficeTD);

                                }
                                else
                                {
                                    int oldData;
                                    var cleanedData = reportCallOfficeTD.Data.Replace(",", "").Replace(".", "");

                                    if (!int.TryParse(cleanedData, out oldData))
                                    {
                                        ModelState.AddModelError("", @"Có lỗi xảy ra. Mã lỗi: Error 3947bn07");
                                        return View();
                                    }
                                    int newData = oldData + countTD;
                                    reportCallOfficeTD.Data = newData.ToString("N0");
                                }
                            }
                        }
                    }
                    else
                    {
                        var newhistoryUser = new HistoryUser
                        {
                            UserId = user.Id,
                            Month = monthInt,
                            Year = yearInt,
                            TypeUser = type,
                            OfficeId = office?.Id,
                            Status = statusUser,
                            DayStart = startDate,
                            Active = true
                        };

                        if (!string.IsNullOrEmpty(dayEnd))
                            newhistoryUser.DayEnd = endDate;
                        if (!string.IsNullOrEmpty(sort))
                            newhistoryUser.Sort = int.Parse(sort);
                        historyUserList.Add(newhistoryUser);

                    }
                }

                var offices = _unitOfWork.OfficeRepository.GetQuery();
                foreach (var office in offices)
                {
                    var callTarget = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 26);
                    var callTD = reportCallOffices.FirstOrDefault(a => a.OfficeId == office.Id && a.ReportCategoryId == 27);
                    var callHT = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == office.Id && a.Month == monthInt && a.Year == yearInt && a.ReportCategoryId == 28).FirstOrDefault();
                    if (callTarget != null && callTD != null)
                    {
                        int callTargetInt;
                        if (!int.TryParse(callTarget.Data.Replace(",", "").Replace(".", ""), out callTargetInt))
                        {
                            ModelState.AddModelError("", @"Có lỗi xảy ra. Mã lỗi: Error 3947bn08");
                            return View();
                        }
                        int callTDInt;
                        if (!int.TryParse(callTD.Data.Replace(",", "").Replace(".", ""), out callTDInt))
                        {
                            ModelState.AddModelError("", @"Có lỗi xảy ra. Mã lỗi: Error 3947bn09");
                            return View();
                        }
                        var ht = ((double)callTDInt / callTargetInt * 100).ToString("F2") + "%";
                        if (callHT != null)
                        {
                            callHT.Data = ht;
                        }
                        else
                        {
                            callHT = new ReportData()
                            {
                                Data = ht,
                                Month = monthInt,
                                Year = yearInt,
                                ReportCategoryId = 28,
                                //check null
                                OfficeId = office.Id,
                                Sort = 9,
                            };

                            reportDataList.Add(callHT);
                        }
                    }
                }
                if (historyUserList.Any())
                    _unitOfWork.HistoryUserRepository.InsertRange(historyUserList);
                if (reportDataList.Any())
                    _unitOfWork.ReportDataRepository.InsertRange(reportDataList);
                _unitOfWork.Save();
                return RedirectToAction("ListHistoryUser", "Vcms", new { result = "update" });
            }

            return RedirectToAction("Index", "Vcms");
        }
        public ActionResult ListHistoryUser(int? page, string username, int? zoneId, int? officeId, int? month, int? year, int? UserType, int? trung, int? active, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var users = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.OfficeId).ThenBy(a => a.TypeUser));
            if (zoneId.HasValue)
            {
                users = users.Where(l => l.Office != null && l.Office.ZoneId == zoneId);
            }
            if (officeId.HasValue)
            {
                users = users.Where(l => l.OfficeId == officeId);
            }
            if (month == null)
                month = DateTime.Now.Month;
            users = users.Where(l => l.Month == month);

            if (year == null)
                year = DateTime.Now.Year;
            users = users.Where(l => l.Year == year);

            if (UserType.HasValue)
            {
                users = users.Where(l => (int)l.TypeUser == UserType);
            }
            if (active == 1)
            {
                users = users.Where(l => l.Status == StatusUser.Active);
            }
            if (active == 2)
            {
                users = users.Where(l => l.Status == StatusUser.Transfer);
            }
            if (active == 3)
            {
                users = users.Where(l => l.Status == StatusUser.InActive);
            }
            if (username != null)
            {
                var newkey = username.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    users = users.Where(l => l.User.Username.Contains(newkey) || l.User.Fullname.Contains(newkey) || l.User.MaNhanVien.Contains(newkey));
                }
            }


            var model = new ListHistoryUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                HistoryUsers = users.ToPagedList(pageNumber, pageSize),
                officeId = officeId,
                ZoneId = zoneId,
                Username = username,
                active = active,
                year = year,
                month = month,
                TypeUser = UserType,
            };

            if (model.ZoneId > 0)
            {
                model.SelectOffices = OfficeSelectList(model.ZoneId);
            }
            return View(model);
        }

        [HttpPost]
        public JsonResult DeleteHistoryUser(int userId)
        {
            var user = _unitOfWork.HistoryUserRepository.GetById(userId);
            user.Active = false;
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa thành công" });
        }
        public ActionResult UnActiveUserExcel()
        {
            return View();
        }
        [HttpPost]
        public ActionResult UnActiveUserExcel(FormCollection fc)
        {
            var file = Request.Files["UserFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var manhanvien = tbl.Rows[i][2].ToString().Trim();
                    var user = _unitOfWork.UserRepository.GetQuery(a => a.MaNhanVien == manhanvien).FirstOrDefault();
                    if (user != null)
                        user.Active = false;
                }
                _unitOfWork.Save();
            }
            return RedirectToAction("ListUser");
        }
        public ActionResult DeleteUserWrong()
        {
            var users = _unitOfWork.UserRepository.GetQuery(a => !string.IsNullOrEmpty(a.MaNhanVien) && (a.MaNhanVien.Length < 8 || a.Username.Length < 8));
            foreach (var user in users)
            {
                user.Active = false;
            }
            var userNoOffices = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.HO || a.TypeUser == TypeUser.CV || a.TypeUser == TypeUser.ASM);
            foreach (var user in userNoOffices)
            {
                user.OfficeId = null;
            }
            _unitOfWork.Save();
            return RedirectToAction("ListUser");
        }
        #endregion

        #region Category
        public ActionResult Category(int type)
        {
            ViewBag.Type = type;
            return View();
        }
        [HttpPost]
        public ActionResult Category(FormCollection fc, int type)
        {
            var file = Request.Files["CategoryFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type8,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                if (type == 1)
                {
                    var categories = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type1);
                    categories.Delete();
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var index = tbl.Rows[i][0].ToString().Trim();
                        var content = tbl.Rows[i][1].ToString().Trim();
                        var regulation = tbl.Rows[i][2].ToString().Trim();
                        var proposalLink = tbl.Rows[i][3].ToString().Trim();
                        var followLink = tbl.Rows[i][4].ToString().Trim();
                        var category = new Category
                        {
                            TypeCategory = TypeCategory.Type1,
                            Index = index,
                            Content = content,
                            Regulation = regulation,
                            ProposalLink = proposalLink,
                            FollowLink = followLink
                        };
                        _unitOfWork.CategoryRepository.Insert(category);
                        _unitOfWork.Save();
                    }
                }
                else if (type == 2)
                {
                    var categories = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type2);
                    categories.Delete();
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var index = tbl.Rows[i][0].ToString().Trim();
                        var content = tbl.Rows[i][1].ToString().Trim();
                        var qdNumber = tbl.Rows[i][2].ToString().Trim();
                        var qdLink = tbl.Rows[i][3].ToString().Trim();
                        var note = tbl.Rows[i][4].ToString().Trim();
                        var category = new Category
                        {
                            TypeCategory = TypeCategory.Type2,
                            Index = index,
                            Content = content,
                            QDNumber = qdNumber,
                            QDLink = qdLink,
                            Note = note
                        };
                        _unitOfWork.CategoryRepository.Insert(category);
                        _unitOfWork.Save();
                    }
                }
                else if (type == 3)
                {
                    var categories = _unitOfWork.CategoryRepository.GetQuery(a => a.TypeCategory == TypeCategory.Type3);
                    categories.Delete();
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var index = tbl.Rows[i][0].ToString().Trim();
                        var month = tbl.Rows[i][1].ToString().Trim();
                        var zone = tbl.Rows[i][2].ToString().Trim();
                        var officescode = tbl.Rows[i][3].ToString().Trim();
                        var content = tbl.Rows[i][4].ToString().Trim();
                        var qdNumber = tbl.Rows[i][5].ToString().Trim();
                        var qdLink = tbl.Rows[i][6].ToString().Trim();
                        var note = tbl.Rows[i][7].ToString().Trim();
                        var category = new Category
                        {
                            TypeCategory = TypeCategory.Type3,
                            Index = index,
                            Content = content,
                            QDNumber = qdNumber,
                            QDLink = qdLink,
                            Note = note,
                            Month = int.Parse(month),
                            Zone = zone,
                            Offices = officescode
                        };
                        _unitOfWork.CategoryRepository.Insert(category);
                        _unitOfWork.Save();
                    }
                }
            }
            return RedirectToAction("Index");
        }
        #endregion

        #region Office
        public void ExportOffice()
        {

            var offices = _unitOfWork.OfficeRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.ZoneId));
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Tên đầy đủ");
            dt.Columns.Add("Tên ngắn");
            dt.Columns.Add("Tên viết tắt");

            var filename = $"danh-sach-chi-nhanh.xlsx";
            var i = 1;
            foreach (var item in offices)
            {
                dt.Rows.Add(i, item.Name, item.ShortName, item.ShortCode);
                i++;
            }
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách chi nhánh");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportHistoryOffice()
        {

            var offices = _unitOfWork.HistoryOfficeRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenByDescending(a => a.ZoneId));
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Định biên ATL");
            dt.Columns.Add("Định biên EC");
            dt.Columns.Add("Nhóm chi nhánh");
            dt.Columns.Add("Áp dụng QĐ 156");

            var filename = $"danh-sach-chi-nhanh-theo-thang.xlsx";
            var i = 1;
            foreach (var item in offices)
            {
                var qd156 = item.QD156 ? "x" : "";
                dt.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.Office.ShortName, item.Zone?.Name, item.DBATL, item.DBEC, item.GroupOffice, qd156);
                i++;
            }
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách chi nhánh theo tháng");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportHistoryUser()
        {
            var users = _unitOfWork.HistoryUserRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenBy(a => a.OfficeId == null).ThenByDescending(a => a.Office.ZoneId).ThenBy(a => a.OfficeId));
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Họ và tên");
            dt.Columns.Add("Mã nhân viên");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Ngày vào làm");
            dt.Columns.Add("Ngày nghỉ/ điều chuyển");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Phân quyền");

            var filename = $"danh-sach-nhan-su-theo-thang.xlsx";
            var i = 1;
            foreach (var item in users)
            {
                dt.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.User.Fullname, item.User.MaNhanVien, GetEnumDisplayName(item.Status), item.DayStart.ToString("dd/MM/yyyy"), item.DayEnd == null ? "" : item.DayEnd.Value.ToString("dd/MM/yyyy"), item.Office?.Name, GetEnumDisplayName(item.TypeUser));
                i++;
            }
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách nhân sự theo tháng");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public void ExportTargetCN_NV()
        {
            var revenueOffices = _unitOfWork.RevenueOfficeRepository.GetQuery(orderBy: q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenByDescending(a => a.Office.ZoneId));
            var revenueUsers = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.HistoryUserId != null, q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenByDescending(a => a.HistoryUser.Office.ZoneId).ThenBy(a => a.HistoryUser.OfficeId).ThenBy(a => a.HistoryUser.Sort));
            var dt = new DataTable();
            dt.Columns.Add("STT");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Chỉ tiêu DS tuyển sinh");
            dt.Columns.Add("Chỉ tiêu DS học vụ");
            dt.Columns.Add("Chỉ tiêu DS kế toán");


            var filename = $"chi-tieu-CN-NV.xlsx";
            var i = 1;
            foreach (var item in revenueOffices)
            {
                dt.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.Office.ShortName,item.Office.Zone?.Name, item.Target_TS, item.Target_HV, item.Target_SAB);
                i++;
            }
            var dt2 = new DataTable();
            dt2.Columns.Add("STT");
            dt2.Columns.Add("Tháng");
            dt2.Columns.Add("Vùng");
            dt2.Columns.Add("Chi nhánh");
            dt2.Columns.Add("Họ tên NS");
            dt2.Columns.Add("Chức vụ");
            dt2.Columns.Add("Ngày vào làm");
            dt2.Columns.Add("Ngày nghỉ/ điều chuyển");
            dt2.Columns.Add("Trạng thái");
            dt2.Columns.Add("Chỉ tiêu doanh số");
            i = 1;
            foreach (var item in revenueUsers)
            {
                dt2.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.HistoryUser.Office?.Zone?.Name, item.HistoryUser.Office?.ShortName, item.HistoryUser.User.Fullname, GetEnumDisplayName(item.HistoryUser.TypeUser), item.HistoryUser.DayStart, item.HistoryUser.DayEnd, GetEnumDisplayName(item.HistoryUser.Status),item.Target);

            }

            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws1 = pck.Workbook.Worksheets.Add("Chỉ tiêu CN");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws1.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws1.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }
                var ws2 = pck.Workbook.Worksheets.Add("Chỉ tiêu NV");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws2.Cells["A1"].LoadFromDataTable(dt2, true);

                //Format the header for column 1-14
                using (var rng = ws2.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public ActionResult ListOffice(int? page, string name, int? trung, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var offices = _unitOfWork.OfficeRepository.GetQuery(orderBy: l => l.OrderBy(a => a.ZoneId).ThenBy(a => a.Sort));

            //if (cityId.HasValue)
            //{
            //    offices = offices.Where(l => l.CityId == cityId);
            //}
            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    offices = offices.Where(l => l.Name.Contains(newkey));
                }
            }
            if (trung == 1)
            {
                var duplicatedMaChiNhanh = offices
                    .GroupBy(u => u.ShortCode)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                offices = offices.Where(u => duplicatedMaChiNhanh.Contains(u.ShortCode));
            }
            var model = new ListOfficeViewModel
            {
                //SelectCities = new SelectList(_unitOfWork.CityRepository.Get(a => a.Active), "Id", "Name"),
                Offices = offices.ToPagedList(pageNumber, pageSize),
                //cityId = cityId,
                Name = name
            };
            return View(model);
        }
        public ActionResult Office()
        {
            var model = new InsertOfficeViewModel
            {
                //SelectCities = new SelectList(_unitOfWork.CityRepository.Get(a => a.Active), "Id", "Name"),
                Office = new Office { Active = true }
            };
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult Office(InsertOfficeViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.OfficeRepository.Insert(model.Office);
                _unitOfWork.Save();
                return RedirectToAction("ListOffice", new { result = "success" });

            }
            //model.SelectCities = new SelectList(_unitOfWork.CityRepository.Get(a => a.Active), "Id", "Name");
            return View(model);
        }
        public ActionResult UpdateOffice(int officeId = 0)
        {
            var office = _unitOfWork.OfficeRepository.GetById(officeId);
            if (office == null)
            {
                return RedirectToAction("ListOffice");
            }
            var model = new InsertOfficeViewModel
            {
                Office = office,
                //SelectCities = new SelectList(_unitOfWork.CityRepository.Get(a => a.Active), "Id", "Name"),
                //DistrictSelectList = DistrictSelectList(Office.CityId)
            };
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult UpdateOffice(InsertOfficeViewModel model)
        {
            var Office = _unitOfWork.OfficeRepository.GetById(model.Office.Id);
            if (Office == null)
            {
                return RedirectToAction("ListOffice");
            }
            if (ModelState.IsValid)
            {
                //Office.CityId = model.Office.CityId;
                Office.Name = model.Office.Name;
                //Office.Infor = model.Office.Infor;
                Office.Active = model.Office.Active;
                Office.Sort = model.Office.Sort;
                Office.Email = model.Office.Email;
                Office.Hotline = model.Office.Hotline;
                Office.ShortCode = model.Office.ShortCode;
                Office.Place = model.Office.Place;
                //Office.DistrictId = model.Office.DistrictId;
                //Office.GoogleMap = model.Office.GoogleMap;
                _unitOfWork.Save();
                return RedirectToAction("ListOffice", new { result = "update" });
            }
            //model.SelectCities = new SelectList(_unitOfWork.CityRepository.Get(a => a.Active), "Id", "Name");
            //model.DistrictSelectList = DistrictSelectList(Office.CityId);
            return View(model);
        }
        [HttpPost]
        public bool DeleteOffice(int OfficeId = 0)
        {
            var Office = _unitOfWork.OfficeRepository.GetById(OfficeId);
            if (Office == null)
            {
                return false;
            }
            _unitOfWork.OfficeRepository.Delete(Office);
            _unitOfWork.Save();
            return true;
        }
        [HttpPost]
        public bool QuickUpdateOffice(int? quantity, bool? status, bool active, int sort = 0, int OfficeId = 0)
        {
            var Office = _unitOfWork.OfficeRepository.GetById(OfficeId);
            if (Office == null)
            {
                return false;
            }
            if (status != null)
            {
                Office.Active = Convert.ToBoolean(status);
            }
            if (sort >= 0)
            {
                Office.Sort = sort;
            }
            Office.Active = active;
            _unitOfWork.Save();
            return true;
        }
        public ActionResult InsertOfficeExcel()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertOfficeExcel(FormCollection fc)
        {
            var file = Request.Files["OfficeFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type4,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Sort));
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    //var username = tbl.Rows[i][3].ToString().Trim();
                    //var countUser = members.Count(a => a.Username == username);
                    //if (countUser > 0) continue;

                    var shortcode = tbl.Rows[i][1].ToString().Trim();
                    var shortname = tbl.Rows[i][2].ToString().Trim();
                    var fullname = tbl.Rows[i][3].ToString().Trim();
                    if (fullname == "") continue;
                    var countOffice = offices.Count(a => a.Name == fullname);
                    if (countOffice > 0) continue;
                    var office = new Office
                    {
                        Name = fullname,
                        ShortCode = shortcode,
                        ShortName = shortname,
                        Active = true,
                        Sort = i,
                    };
                    _unitOfWork.OfficeRepository.Insert(office);
                    _unitOfWork.Save();
                }
            }
            return RedirectToAction("ListOffice");
        }
        public ActionResult InsertHistoryOfficeExcel()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertHistoryOfficeExcel(FormCollection fc)
        {
            var file = Request.Files["OfficeFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type9,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var historyOfficeList = new List<HistoryOffice>();
                var offices = _unitOfWork.OfficeRepository.GetQuery();
                var zones = _unitOfWork.ZoneRepository.GetQuery();
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var shortname = tbl.Rows[i][0].ToString().Trim();
                    if (string.IsNullOrEmpty(shortname))
                        continue;
                    if (shortname == "OE Phan Văn Trị")
                    {

                    }
                    var office = offices.FirstOrDefault(a => a.ShortName == shortname);
                    if (office == null)
                        continue;
                    var zonename = tbl.Rows[i][1].ToString().Trim();
                    if (string.IsNullOrEmpty(zonename))
                        continue;
                    var zone = zones.FirstOrDefault(a => a.Name == zonename || a.ShortCode == zonename);
                    if (zone == null)
                        continue;
                    var monthStr = tbl.Rows[i][2].ToString().Trim();
                    if (string.IsNullOrEmpty(monthStr) || !int.TryParse(monthStr, out var monthInt))
                        continue;
                    var yearStr = tbl.Rows[i][3].ToString().Trim();
                    if (string.IsNullOrEmpty(yearStr) || !int.TryParse(yearStr, out var yearInt))
                        continue;


                    var dbECStr = tbl.Rows[i][4].ToString().Trim();
                    if (string.IsNullOrEmpty(dbECStr) || !int.TryParse(dbECStr, out var dbECInt))
                        continue;
                    var dbATLStr = tbl.Rows[i][5].ToString().Trim();
                    if (string.IsNullOrEmpty(dbATLStr) || !int.TryParse(dbATLStr, out var dbATLInt))
                        continue;
                    var group = tbl.Rows[i][6].ToString().Trim();
                    if (string.IsNullOrEmpty(group))
                        continue;
                    GroupOffice groupOffice = new GroupOffice();
                    switch (group)
                    {
                        case "A":
                            groupOffice = GroupOffice.A;
                            break;
                        case "B":
                            groupOffice = GroupOffice.B;
                            break;
                        case "C":
                            groupOffice = GroupOffice.C;
                            break;
                        case "D":
                            groupOffice = GroupOffice.D;
                            break;
                        default:
                            continue;
                    }
                    var qd156 = tbl.Rows[i][7].ToString().Trim();
                    var historyOffice = _unitOfWork.HistoryOfficeRepository.GetQuery(a => a.Month == monthInt && a.Year == yearInt && a.OfficeId == office.Id).FirstOrDefault();

                    if (historyOffice != null)
                    {
                        historyOffice.GroupOffice = groupOffice;
                        historyOffice.ZoneId = zone.Id;
                        historyOffice.DBATL = dbATLInt;
                        historyOffice.DBEC = dbECInt;
                        historyOffice.QD156 = string.IsNullOrEmpty(qd156) ? false : true;
                    }
                    else
                    {
                        var newhistoryOffice = new HistoryOffice
                        {
                            Month = monthInt,
                            Year = yearInt,
                            OfficeId = office.Id,
                            GroupOffice = groupOffice,
                            ZoneId = zone.Id,
                            DBATL = dbATLInt,
                            DBEC = dbECInt,
                            QD156 = string.IsNullOrEmpty(qd156) ? false : true
                        };
                        historyOfficeList.Add(newhistoryOffice);
                    }
                }

                if (historyOfficeList.Any())
                    _unitOfWork.HistoryOfficeRepository.InsertRange(historyOfficeList);
                _unitOfWork.Save();
                return RedirectToAction("ListHistoryOffice", new { result = "add" });
            }
            return RedirectToAction("ListHistoryOffice");
        }
        public ActionResult ListHistoryOffice(int? page, string name, int? zoneId, int? month, int? year, int? Group, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var offices = _unitOfWork.HistoryOfficeRepository.GetQuery(a => a.Active, q => q.OrderByDescending(a => a.ZoneId));
            if (zoneId.HasValue)
            {
                offices = offices.Where(l => l.ZoneId == zoneId);
            }
            if (month == null)
                month = DateTime.Now.Month;
            offices = offices.Where(l => l.Month == month);

            if (year == null)
                year = DateTime.Now.Year;
            offices = offices.Where(l => l.Year == year);

            if (Group.HasValue)
            {
                offices = offices.Where(l => (int)l.GroupOffice == Group);
            }

            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    offices = offices.Where(l => l.Office.Name.Contains(newkey) || l.Office.ShortName.Contains(newkey) || l.Office.ShortCode.Contains(newkey));
                }
            }

            var model = new ListHistoryOfficeViewModel
            {
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                HistoryOffices = offices.ToPagedList(pageNumber, pageSize),
                ZoneId = zoneId,
                Name = name,
                year = year,
                month = month,
                Group = Group,
            };
            return View(model);
        }

        [HttpPost]
        public JsonResult DeleteHistoryOffice(int officeId)
        {
            var office = _unitOfWork.HistoryOfficeRepository.GetById(officeId);
            office.Active = false;
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa thành công" });
        }
        #endregion

        #region Zone
        public ActionResult InsertZoneExcel()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertZoneExcel(FormCollection fc)
        {
            var file = Request.Files["ZoneFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type3,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                var tbl = result.Tables[0];
                var zones = _unitOfWork.ZoneRepository.GetQuery();
                for (var i = 1; i < tbl.Rows.Count; i++)
                {
                    var officeIds = "";
                    var officescode = tbl.Rows[i][0].ToString().Trim();

                    var fullname = tbl.Rows[i][1].ToString().Trim();
                    if (fullname == "") continue;
                    var shortcode = tbl.Rows[i][2].ToString().Trim();
                    if (shortcode == "") continue;
                    var zoneOld = zones.Where(a => a.Name == fullname || a.ShortCode == shortcode).FirstOrDefault();
                    if (zoneOld != null)
                    {
                        zoneOld.Name = fullname;
                        zoneOld.ShortCode = shortcode;
                        zoneOld.ShortName = officescode;
                        zoneOld.Sort = i;
                        zoneOld.Active = true;
                        if (!string.IsNullOrEmpty(officescode))
                        {
                            var listcode = officescode.Split(',');
                            foreach (var item in listcode)
                            {
                                var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                if (office != null)
                                {
                                    officeIds += office.Id + ",";
                                    office.ZoneId = zoneOld.Id;
                                    office.Zone = zoneOld;
                                }
                            }
                        }
                        zoneOld.OfficeIds = officeIds == "" ? null : "," + officeIds;
                    }
                    else
                    {
                        var zone = new Zone
                        {
                            Name = fullname,
                            ShortCode = shortcode,
                            Active = true,
                            ShortName = officescode,
                            Sort = i,
                        };
                        _unitOfWork.ZoneRepository.Insert(zone);
                        if (!string.IsNullOrEmpty(officescode))
                        {
                            var listcode = officescode.Split(',');
                            foreach (var item in listcode)
                            {
                                var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                                if (office != null)
                                {
                                    officeIds += office.Id + ",";
                                    office.ZoneId = zone.Id;
                                    office.Zone = zone;
                                }
                            }
                        }
                        zone.OfficeIds = officeIds == "" ? null : "," + officeIds;
                    }

                }
                _unitOfWork.Save();
            }
            return RedirectToAction("CreateZone");
        }
        public PartialViewResult ListZone()
        {
            var model = _unitOfWork.ZoneRepository.Get();
            return PartialView(model);
        }
        public ActionResult CreateZone(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateZoneViewModel
            {
                Zone = new Zone(),
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active)
                //SelectOffice = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name")
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateZone(CreateZoneViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.ZoneRepository.Insert(model.Zone);
                var catIds = fc.GetValues("CatIDs");
                if (catIds != null)
                {
                    foreach (var item in catIds)
                    {
                        model.Zone.OfficeIds += (item + ",");
                    }
                    model.Zone.OfficeIds = "," + model.Zone.OfficeIds;
                }
                _unitOfWork.Save();
                var zone = _unitOfWork.ZoneRepository.Get(a => a.OfficeIds == model.Zone.OfficeIds, q => q.OrderByDescending(a => a.Id)).FirstOrDefault();
                if (zone?.OfficeIds != null)
                {
                    string[] a = zone.OfficeIds.Trim(',').Split(',');
                    zone.ShortName = "";
                    foreach (var item in a)
                    {
                        int officeId = int.Parse(item.ToString());
                        var office = _unitOfWork.OfficeRepository.GetById(officeId);
                        if (office != null)
                        {
                            office.ZoneId = zone.Id;
                            zone.ShortName += "," + office.ShortCode;
                        }
                        zone.ShortName = zone.ShortName.Trim(',');
                    }
                    _unitOfWork.Save();
                }
                return RedirectToAction("CreateZone", new { result = "add" });

            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult UpdateZone(int zoneId)
        {
            var zone = _unitOfWork.ZoneRepository.GetById(zoneId);
            if (zone == null)
            {
                return RedirectToAction("CreateZone");
            }
            var model = new CreateZoneViewModel
            {
                Zone = zone,
                Offices = _unitOfWork.OfficeRepository.Get()
            };
            if (!string.IsNullOrEmpty(zone.OfficeIds))
            {
                model.CatIds = zone.OfficeIds
                                        .Split(',')
                                        .Select(x =>
                                        {
                                            int.TryParse(x, out int value);
                                            return value;
                                        })
                                        .ToList();
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult UpdateZone(CreateZoneViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                var zone = _unitOfWork.ZoneRepository.GetById(model.Zone.Id);
                if (zone != null)
                {

                    var catIds = fc.GetValues("CatIDs");
                    if (catIds != null)
                    {
                        zone.OfficeIds = "";
                        foreach (var item in catIds)
                        {
                            zone.OfficeIds += (item + ",");
                        }
                        zone.OfficeIds = "," + zone.OfficeIds;
                    }
                    zone.Name = model.Zone.Name;
                    zone.ShortCode = model.Zone.ShortCode;
                    zone.Active = model.Zone.Active;
                    _unitOfWork.Save();
                    string[] a = zone.OfficeIds.Trim(',').Split(',');
                    zone.ShortName = "";
                    foreach (var item in a)
                    {
                        int officeId = int.Parse(item);
                        var office = _unitOfWork.OfficeRepository.GetById(officeId);
                        if (office != null)
                        {
                            office.ZoneId = zone.Id;
                            zone.ShortName += "," + office.ShortCode;
                        }
                    }
                    zone.ShortName = zone.ShortName.Trim(',');
                    _unitOfWork.Save();

                    return RedirectToAction("CreateZone", new { result = "update" });
                }
            }
            return HttpNotFound();

        }
        [HttpPost]
        public bool DeleteZone(int zoneId = 0)
        {
            var zone = _unitOfWork.ZoneRepository.GetById(zoneId);
            if (zone == null)
            {
                return false;
            }
            zone.Active = false;
            _unitOfWork.Save();
            return true;
        }
        #endregion

        #region Discount

        public ActionResult ClearDiscount()
        {
            var discounts = _unitOfWork.DiscountRepository.GetQuery();
            discounts.Delete();
            return RedirectToAction("ListDiscount");
        }
        public ActionResult ListDiscount(int? page, string name, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var discounts = _unitOfWork.DiscountRepository.GetQuery(orderBy: l => l.OrderBy(a => a.Id));

            //if (cityId.HasValue)
            //{
            //    discounts = discounts.Where(l => l.CityId == cityId);
            //}
            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    discounts = discounts.Where(l => l.Username.Contains(newkey));
                }
            }
            var model = new ListDiscountViewModel
            {
                Discounts = discounts.ToPagedList(pageNumber, pageSize),
                Name = name
            };
            return View(model);
        }
        public ActionResult DeleteDiscount()
        {

            var model = _unitOfWork.DiscountRepository.Get(a => a.Username == "QĐ 200" || a.Username == "QĐ 400" || a.Username == "QĐ 300");
            foreach (var item in model)
            {
                _unitOfWork.DiscountRepository.Delete(item);
            }
            _unitOfWork.Save();


            return RedirectToAction("Index");
        }
        public ActionResult CreateDiscount(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateDiscountViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                //Discount = new Discount(),
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateDiscount(CreateDiscountViewModel model)
        {
            if (ModelState.IsValid)
            {

                var m = new Discount
                {
                    Username = model.Name,
                    //Offices = model.Offices,
                    Active = model.Active,
                    PercentDiscount = model.PercentDiscount,
                    Pathway = model.Pathway,
                    Gift = model.Gift

                };
                if (model.MoneyDiscount != null)
                {
                    m.MoneyDiscount = Convert.ToInt32(model.MoneyDiscount.Replace(",", ""));
                }
                _unitOfWork.DiscountRepository.Insert(m);
                _unitOfWork.Save();
                //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),

                return RedirectToAction("CreateDiscount", new { result = "add" });

            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult InsertDiscountExcel()
        {
            return View();
        }
        [HttpPost]
        //public ActionResult InsertDiscountExcel(FormCollection fc)
        //{
        //    var file = Request.Files["DiscountFile"];
        //    if (file != null && file.ContentLength > 0)
        //    {
        //        var stream = file.InputStream;
        //        IExcelDataReader reader;
        //        if (file.FileName.EndsWith(".xls"))
        //        {
        //            reader = ExcelReaderFactory.CreateBinaryReader(stream);
        //        }
        //        else if (file.FileName.EndsWith(".xlsx"))
        //        {
        //            reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        //        }
        //        else
        //        {
        //            ModelState.AddModelError("File", @"This file format is not supported");
        //            return View();
        //        }
        //        var result = reader.AsDataSet();
        //        reader.Close();

        //        //var tbl = result.Tables[0];
        //        var discounts = _unitOfWork.DiscountRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Id));

        //        foreach (DataTable tbl in result.Tables)
        //        {
        //            for (var i = 1; i < tbl.Rows.Count; i++)
        //            {
        //                //var username = tbl.Rows[i][3].ToString().Trim();
        //                //var countUser = members.Count(a => a.Username == username);
        //                //if (countUser > 0) continue;
        //                var fullname = tbl.Rows[i][0].ToString().Trim();
        //                if (fullname == null) continue;
        //                int? moneyDiscount = int.TryParse(tbl.Rows[i][2].ToString().Trim(), out var r) ? (int?)r : null;
        //                double? percentDiscount = double.TryParse(tbl.Rows[i][3].ToString().Trim(), out var r2) ? (double?)r2 : null;

        //                var startDateStr = tbl.Rows[i][4].ToString().Trim();
        //                var endDateStr = tbl.Rows[i][5].ToString().Trim();
        //                var gift = tbl.Rows[i][7].ToString().Trim();
        //                var offices = tbl.Rows[i][9].ToString().Trim();
        //                if (offices == null) continue;
        //                int pathway = int.TryParse(tbl.Rows[i][10].ToString().Trim(), out var r3) ? r3 : 0;
        //                int pathwayTo = int.TryParse(tbl.Rows[i][11].ToString().Trim(), out var r4) ? r4 : 0;
        //                var cth = tbl.Rows[i][12].ToString().Trim();
        //                //var countDiscount = discounts.Count(a => a.Username == fullname);
        //                //if (countDiscount > 0) continue;
        //                var discount = new Discount
        //                {
        //                    Username = fullname,
        //                    MoneyDiscount = moneyDiscount,
        //                    PercentDiscount = percentDiscount,
        //                    Gift = gift,
        //                    Offices = offices,
        //                    Pathway = pathway,
        //                    PathwayTo = pathwayTo,
        //                    Cth = cth,
        //                    StartDate = DateTime.TryParse(startDateStr, out var sDate) ? sDate : (DateTime?)null,
        //                    EndDate = DateTime.TryParse(endDateStr, out var eDate) ? eDate : (DateTime?)null,
        //                    Active = true
        //                };
        //                _unitOfWork.DiscountRepository.Insert(discount);

        //            }
        //        }
        //        _unitOfWork.Save();

        //    }
        //    return RedirectToAction("ListDiscount");
        //}
        public ActionResult InsertDiscountExcel(FormCollection fc)
        {
            var file = Request.Files["DiscountFile"];
            if (file != null && file.ContentLength > 0)
            {
                var stream = file.InputStream;
                IExcelDataReader reader;
                if (file.FileName.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (file.FileName.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else
                {
                    ModelState.AddModelError("File", @"This file format is not supported");
                    return View();
                }
                var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                var logImport = new Models.LogImport
                {
                    Admin = Fullname,
                    Name = Path.GetFileName(file.FileName),
                    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                    TypeImport = TypeImport.Type7,
                };
                _unitOfWork.LogImportRepository.Insert(logImport);
                _unitOfWork.Save();
                // Lưu tệp tài liệu
                var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                //var tbl = result.Tables[0];
                //var discounts = _unitOfWork.DiscountRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Id));
                var listDiscount = new List<Discount>();
                foreach (DataTable tbl in result.Tables)
                {
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        //var username = tbl.Rows[i][3].ToString().Trim();
                        //var countUser = members.Count(a => a.Username == username);
                        //if (countUser > 0) continue;
                        var fullname = tbl.Rows[i][0].ToString().Trim();
                        if (fullname == null) continue;
                        int? moneyDiscount = int.TryParse(tbl.Rows[i][2].ToString().Trim(), out var r) ? (int?)r : null;
                        double? percentDiscount = double.TryParse(tbl.Rows[i][3].ToString().Trim(), out var r2) ? (double?)r2 : null;

                        var startDateStr = tbl.Rows[i][4].ToString().Trim();
                        var endDateStr = tbl.Rows[i][5].ToString().Trim();
                        var gift = tbl.Rows[i][7].ToString().Trim();
                        var offices = tbl.Rows[i][9].ToString().Trim();
                        if (offices == null) continue;
                        int pathway = int.TryParse(tbl.Rows[i][10].ToString().Trim(), out var r3) ? r3 : 0;
                        int pathwayTo = int.TryParse(tbl.Rows[i][11].ToString().Trim(), out var r4) ? r4 : 0;
                        var cth = tbl.Rows[i][12].ToString().Trim();
                        //var countDiscount = discounts.Count(a => a.Username == fullname);
                        //if (countDiscount > 0) continue;
                        var discount = new Discount
                        {
                            Username = fullname,
                            MoneyDiscount = moneyDiscount,
                            PercentDiscount = percentDiscount,
                            Gift = gift,
                            Offices = offices,
                            Pathway = pathway,
                            PathwayTo = pathwayTo,
                            Cth = cth,
                            StartDate = DateTime.TryParse(startDateStr, out var sDate) ? sDate : (DateTime?)null,
                            EndDate = DateTime.TryParse(endDateStr, out var eDate) ? eDate : (DateTime?)null,
                            Active = true
                        };
                        //_unitOfWork.DiscountRepository.Insert(discount);
                        listDiscount.Add(discount);

                    }
                }
                if (listDiscount.Any())
                {
                    _unitOfWork.DiscountRepository.InsertRange(listDiscount);
                }

                _unitOfWork.Save();

            }
            return RedirectToAction("ListDiscount");
        }

        #endregion

        #region Tong_hop_loi
        public PartialViewResult ListTypeFault()
        {
            var model = _unitOfWork.TypeFaultRepository.Get();
            return PartialView(model);
        }
        public ActionResult CreateTypeFault(string result = "")
        {
            ViewBag.Result = result;
            //var model = new CreateTypeFaultViewModel
            //{
            //    TypeFault = new TypeFault(),
            //    Offices = _unitOfWork.OfficeRepository.Get()
            //    //SelectOffice = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name")
            //};
            return View(new TypeFault());
        }
        [HttpPost]
        public ActionResult CreateTypeFault(TypeFault model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.TypeFaultRepository.Insert(model);
                _unitOfWork.Save();
                return RedirectToAction("CreateTypeFault", new { result = "add" });
            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult UpdateTypeFault(int typeFaultId)
        {
            var typeFault = _unitOfWork.TypeFaultRepository.GetById(typeFaultId);
            if (typeFault == null)
            {
                return RedirectToAction("CreateTypeFault");
            }
            //var model = new CreateTypeFaultViewModel
            //{
            //    TypeFault = typeFault,
            //    Offices = _unitOfWork.OfficeRepository.Get()
            //};
            return View(typeFault);
        }

        [HttpPost]
        public ActionResult UpdateTypeFault(TypeFault model)
        {
            if (ModelState.IsValid)
            {
                var typeFault = _unitOfWork.TypeFaultRepository.GetById(model.Id);
                if (typeFault != null)
                {

                    typeFault.Content = model.Content;
                    typeFault.Active = model.Active;
                    _unitOfWork.Save();
                    return RedirectToAction("CreateTypeFault", new { result = "add" });
                }
            }
            return HttpNotFound();

        }
        [HttpPost]
        public bool DeleteTypeFault(int typeFaultId = 0)
        {
            var typeFault = _unitOfWork.TypeFaultRepository.GetById(typeFaultId);
            if (typeFault == null)
            {
                return false;
            }
            typeFault.Active = false;
            _unitOfWork.Save();
            return true;
        }
        #endregion

        #region Loai_de_xuat
        public PartialViewResult ListProposalType()
        {
            var model = _unitOfWork.ProposalTypeRepository.Get();
            return PartialView(model);
        }
        public ActionResult CreateProposalType(string result = "")
        {
            ViewBag.Result = result;
            //var model = new CreateProposalTypeViewModel
            //{
            //    ProposalType = new ProposalType(),
            //    Offices = _unitOfWork.OfficeRepository.Get()
            //    //SelectOffice = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name")
            //};
            return View(new ProposalType());
        }
        [HttpPost]
        public ActionResult CreateProposalType(ProposalType model)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.ProposalTypeRepository.Insert(model);
                _unitOfWork.Save();
                return RedirectToAction("CreateProposalType", new { result = "add" });
            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult UpdateProposalType(int proposalTypeId)
        {
            var proposalType = _unitOfWork.ProposalTypeRepository.GetById(proposalTypeId);
            if (proposalType == null)
            {
                return RedirectToAction("CreateProposalType");
            }
            //var model = new CreateProposalTypeViewModel
            //{
            //    ProposalType = proposalType,
            //    Offices = _unitOfWork.OfficeRepository.Get()
            //};
            return View(proposalType);
        }

        [HttpPost]
        public ActionResult UpdateProposalType(ProposalType model)
        {
            if (ModelState.IsValid)
            {
                var proposalType = _unitOfWork.ProposalTypeRepository.GetById(model.Id);
                if (proposalType != null)
                {

                    proposalType.Content = model.Content;
                    proposalType.Active = model.Active;
                    _unitOfWork.Save();
                    return RedirectToAction("CreateProposalType", new { result = "add" });
                }
            }
            return HttpNotFound();

        }
        [HttpPost]
        public bool DeleteProposalType(int proposalTypeId = 0)
        {
            var proposalType = _unitOfWork.ProposalTypeRepository.GetById(proposalTypeId);
            if (proposalType == null)
            {
                return false;
            }
            proposalType.Active = false;
            _unitOfWork.Save();
            return true;
        }
        #endregion

        public void ExportTargetOffice()
        {

            var revenues = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.Month == 8 && a.Year == 2025)
                .GroupBy(a => new { a.OfficeId })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault());
            var revenueHOs = _unitOfWork.RevenueOfficeRepository.GetQuery(a => a.Month == 8 && a.Year == 2025);
            var dt = new DataTable();
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId));
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chỉ tiêu DS công ty");
            dt.Columns.Add("Cam kết HT doanh số");
            foreach (var office in offices)
            {
                var revenueHO = revenueHOs.Where(a => a.OfficeId == office.Id).FirstOrDefault();
                var revenue = revenues.Where(a => a.OfficeId == office.Id).FirstOrDefault();
                dt.Rows.Add(office.ShortName, office.Zone?.Name, 8, revenueHO != null ? revenueHO.Target_TS.ToString("N0") : "", revenue != null ? revenue.TargetBM_TS.ToString("N0") : "");
            }
            var filename = $"danh-sach-cam-ket-hoan-thanh-DS-CN.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách cam kết hoàn thành DS");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportTargetUser()
        {
            var revenueHOMonths = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.Month == 8 && a.Year == 2025);
            var revenueBMMonths = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Month == 8 && a.Year == 2025)
                .GroupBy(a => new { a.UserId })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault());
            var revenueBMWeeks = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.Month == 8 && a.Year == 2025)
                .GroupBy(a => new { a.UserId, a.WeekNumber })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault());
            var dt = new DataTable();
            var users = _unitOfWork.UserRepository.GetQuery(a => a.Active && a.Office != null && a.TypeUser != null && a.TypeUser != TypeUser.PKT && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.ASM,
                q => q.OrderBy(a => a.Office.ZoneId).ThenBy(a => a.OfficeId));
            dt.Columns.Add("Nhân sự");
            dt.Columns.Add("Mã nhân viên");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chỉ tiêu DS công ty");
            dt.Columns.Add("Cam kết HT doanh số");
            dt.Columns.Add("DS cam kết Tuần 1");
            dt.Columns.Add("DS cam kết Tuần 2");
            dt.Columns.Add("DS cam kết Tuần 3");
            dt.Columns.Add("DS cam kết Tuần 4");
            dt.Columns.Add("DS cam kết Tuần 5");
            dt.Columns.Add("DS cam kết Tuần 6");
            foreach (var user in users)
            {
                //var revenueHOMonth = revenueHOMonths.Where(a => a.UserId == user.Id).FirstOrDefault();
                //var revenueBMMonth = revenueBMMonths.Where(a => a.UserId == user.Id).FirstOrDefault();
                //dt.Columns.Add(user.Fullname);
                //dt.Columns.Add(user.Office.ShortName);
                //dt.Columns.Add(user.Office.Zone?.Name);
                //dt.Columns.Add("8");
                //dt.Columns.Add(revenueHOMonth != null ? revenueHOMonth.Target.ToString("N0") : "");
                //dt.Columns.Add(revenueBMMonth != null ? revenueBMMonth.TargetBM.ToString("N0") : "");
                //for (int i = 1; i <= 6; i++)
                //{
                //    var revenueBMWeek = revenueBMWeeks.Where(a => a.UserId == user.Id && (int)a.WeekNumber == i).FirstOrDefault();
                //    dt.Columns.Add(revenueBMWeek != null ? revenueBMWeek.TargetBM.ToString("N0") : "");

                //}
                var revenueHOMonth = revenueHOMonths.FirstOrDefault(a => a.UserId == user.Id);
                var revenueBMMonth = revenueBMMonths.FirstOrDefault(a => a.UserId == user.Id);

                var row = dt.NewRow();
                row["Nhân sự"] = user.Fullname ?? user.Username;
                row["Mã nhân viên"] = user.MaNhanVien ?? user.Username;
                row["Chi nhánh"] = user.Office.ShortName;
                row["Vùng"] = user.Office.Zone?.Name;
                row["Tháng"] = "8"; // hoặc revenueMonth.Month.ToString()

                row["Chỉ tiêu DS công ty"] = revenueHOMonth != null ? revenueHOMonth.Target.ToString("N0") : "";
                row["Cam kết HT doanh số"] = revenueBMMonth != null ? revenueBMMonth.TargetBM.ToString("N0") : "";

                for (int i = 1; i <= 6; i++)
                {
                    var revenueBMWeek = revenueBMWeeks.FirstOrDefault(a => a.UserId == user.Id && (int)a.WeekNumber == i);
                    row[$"DS cam kết Tuần {i}"] = revenueBMWeek != null ? revenueBMWeek.TargetBM.ToString("N0") : "";
                }

                dt.Rows.Add(row);
            }

            var filename = $"danh-sach-PBDS-nhan-su.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách phân bổ doanh số");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportTargetUser2()
        {
            var month = 8;
            var year = 2025;

            // Lấy dữ liệu chỉ 1 lần, sau đó xử lý trên bộ nhớ
            var revenueHOMonths = _unitOfWork.RevenueUser_MonthRepository
                .GetQuery(a => a.Month == month && a.Year == year)
                .AsNoTracking()
                .ToDictionary(a => a.UserId, a => a);

            var revenueBMMonths = _unitOfWork.RevenueUser_Month_BMRepository
                .GetQuery(a => a.Month == month && a.Year == year)
                .AsNoTracking()
                .GroupBy(a => a.UserId)
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault())
                .ToDictionary(a => a.UserId, a => a);

            var revenueBMWeeks = _unitOfWork.RevenueUser_WeekRepository
                .GetQuery(a => a.Month == month && a.Year == year)
                .AsNoTracking()
                .GroupBy(a => new { a.UserId, a.WeekNumber })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault())
                .ToList()
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var dt = new DataTable();
            dt.Columns.Add("Nhân sự");
            dt.Columns.Add("Mã nhân viên");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Chỉ tiêu DS công ty");
            dt.Columns.Add("Cam kết HT doanh số");

            for (int i = 1; i <= 6; i++)
            {
                dt.Columns.Add($"DS cam kết Tuần {i}");
            }

            var users = _unitOfWork.UserRepository.GetQuery(
                    a => a.Active && a.Office != null && a.TypeUser != null
                         && a.TypeUser != TypeUser.PKT
                         && a.TypeUser != TypeUser.HO
                         && a.TypeUser != TypeUser.CV
                         && a.TypeUser != TypeUser.ASM,
                    q => q.OrderBy(a => a.Office.ZoneId).ThenBy(a => a.OfficeId))
                .AsNoTracking()
                .ToList();

            foreach (var user in users)
            {
                var row = dt.NewRow();
                row["Nhân sự"] = user.Fullname ?? user.Username;
                row["Mã nhân viên"] = user.MaNhanVien ?? user.Username;
                row["Chi nhánh"] = user.Office.ShortName;
                row["Vùng"] = user.Office.Zone?.Name;
                row["Tháng"] = month.ToString();

                // Chỉ tiêu công ty
                if (revenueHOMonths.TryGetValue(user.Id, out var revenueHOMonth))
                {
                    row["Chỉ tiêu DS công ty"] = revenueHOMonth.Target.ToString("N0");
                }

                // Cam kết doanh số BM
                if (revenueBMMonths.TryGetValue(user.Id, out var revenueBMMonth))
                {
                    row["Cam kết HT doanh số"] = revenueBMMonth.TargetBM.ToString("N0");
                }

                // DS cam kết từng tuần
                if (revenueBMWeeks.TryGetValue(user.Id, out var weekList))
                {
                    for (int i = 1; i <= 6; i++)
                    {
                        var revenueBMWeek = weekList.FirstOrDefault(w => (int)w.WeekNumber == i);
                        if (revenueBMWeek != null)
                        {
                            row[$"DS cam kết Tuần {i}"] = revenueBMWeek.TargetBM.ToString("N0");
                        }
                    }
                }

                dt.Rows.Add(row);
            }

            var filename = $"danh-sach-PBDS-nhan-su.xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Danh sách phân bổ doanh số");

                // Load dữ liệu vào Excel
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                // Định dạng tiêu đề
                using (var rng = ws.Cells[1, 1, 1, dt.Columns.Count])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                // Trả file Excel về client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment; filename=" + filename);
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public void ExportEvent()
        {
            var events = _unitOfWork.EventRepository.GetQuery(a => a.Month == 8 && a.Year == 2025)
                .GroupBy(a => new { a.DayofWeek, a.WeekNumber, a.OfficeId })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault());
            var dt = new DataTable();
            var offices = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId));
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Tuần");
            for (int d = 2; d <= 8; d++) // 2: Monday, ..., 8: Sunday
            {
                if (d != 8)
                    dt.Columns.Add($"Thứ {d}");
                else
                    dt.Columns.Add("Chủ nhật");
            }

            foreach (var office in offices)
            {
                for (int i = 1; i <= 6; i++) // i = tuần (1–6)
                {
                    var row = dt.NewRow();
                    row["Chi nhánh"] = office.ShortName;
                    row["Vùng"] = office.Zone?.Name;
                    row["Tháng"] = "8";
                    row["Tuần"] = i;

                    for (int j = 2; j <= 8; j++) // j = thứ trong tuần (2–8)
                    {
                        var eventDay = events.FirstOrDefault(a =>
                            a.OfficeId == office.Id &&
                            (int)a.WeekNumber == i &&
                            (int)a.DayofWeek == j);
                        if (eventDay != null)
                        {
                            var eventInfo = string.Join("\n", new[]
{
    $"Loại hoạt động: {GetEnumDisplayName(eventDay.TypeEvent)}",
    $"Tên hoạt động: {eventDay.Name}",
    $"Đối tượng tham gia: {GetEnumDisplayName(eventDay.TypeJoin)}",
    $"Lứa tuổi: {eventDay.Ages}",
    $"Thời gian: {eventDay.TimeFrom} - {eventDay.TimeTo}",
});


                            var columnName = j == 8 ? "Chủ nhật" : $"Thứ {j}";
                            row[columnName] = eventInfo.Trim(); // loại bỏ dòng trắng đầu
                        }
                    }

                    dt.Rows.Add(row); // ✅ Mỗi tuần là 1 dòng
                }
            }

            var filename = $"danh-sach-su-kien.xlsx";
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách sự kiện");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

                //Format the header for column 1-14
                using (var rng = ws.Cells["A1:O1"])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;                      //Set Pattern for the background to Solid
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));  //Set color to dark blue
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                //Example how to Format Column 7 as numeric
                //using (var col = ws.Cells[2, 7, 2 + dt.Rows.Count, 7])
                //{
                //    col.Style.Numberformat.Format = "#,##0";
                //    col.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                //}

                //Write it back to the client
                if (ws.Dimension != null) // kiểm tra sheet có dữ liệu
                {
                    ws.Cells[ws.Dimension.Address].Style.WrapText = true;
                }

                // ✅ (Tùy chọn) Tự động giãn cột cho vừa nội dung
                ws.Cells[ws.Dimension.Address].AutoFitColumns();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", "attachment;  filename=" + filename + "");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportEvent2()
        {
            var month = 8;
            var year = 2025;

            // Lấy toàn bộ sự kiện thỏa điều kiện, group theo OfficeId + WeekNumber + DayofWeek
            var eventsList = _unitOfWork.EventRepository
                .GetQuery(a => a.Month == month && a.Year == year)
                .AsNoTracking()
                .GroupBy(a => new { a.OfficeId, a.WeekNumber, a.DayofWeek })
                .Select(g => g.OrderByDescending(a => a.CreateDate).FirstOrDefault())
                .ToList();

            // Dictionary tra cứu nhanh: OfficeId -> WeekEnum -> DayEnum
            var eventDict = eventsList
                .GroupBy(e => e.OfficeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.GroupBy(e => e.WeekNumber)
                          .ToDictionary(
                              wg => wg.Key,
                              wg => wg.ToDictionary(e => e.DayofWeek, e => e)
                          )
                );

            var offices = _unitOfWork.OfficeRepository
                .GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId))
                .AsNoTracking()
                .ToList();

            var dt = new DataTable();
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Tháng");
            dt.Columns.Add("Tuần");

            for (int d = 2; d <= 8; d++) // Thứ 2 đến Chủ nhật
            {
                dt.Columns.Add(d == 8 ? "Chủ nhật" : $"Thứ {d}");
            }

            foreach (var office in offices)
            {
                for (int i = 1; i <= 6; i++) // Tuần 1–6
                {
                    var row = dt.NewRow();
                    row["Chi nhánh"] = office.ShortName;
                    row["Vùng"] = office.Zone?.Name;
                    row["Tháng"] = month.ToString();
                    row["Tuần"] = i;

                    // Ép kiểu từ int sang enum (WeekEnum và DayEnum là enum thực tế bạn đang dùng)
                    var weekEnum = (WeekNumber)i;

                    for (int j = 2; j <= 8; j++)
                    {
                        var dayEnum = (DayofWeek)j;

                        if (eventDict.TryGetValue(office.Id, out var weekDict) &&
                            weekDict.TryGetValue(weekEnum, out var dayDict) &&
                            dayDict.TryGetValue(dayEnum, out var eventDay))
                        {
                            var eventInfo = string.Join("\n", new[]
                            {
                        $"Loại hoạt động: {GetEnumDisplayName(eventDay.TypeEvent)}",
                        $"Tên hoạt động: {eventDay.Name}",
                        $"Đối tượng tham gia: {GetEnumDisplayName(eventDay.TypeJoin)}",
                        $"Lứa tuổi: {eventDay.Ages}",
                        $"Thời gian: {eventDay.TimeFrom} - {eventDay.TimeTo}",
                    });

                            var columnName = j == 8 ? "Chủ nhật" : $"Thứ {j}";
                            row[columnName] = eventInfo.Trim();
                        }
                    }

                    dt.Rows.Add(row);
                }
            }

            var filename = $"danh-sach-su-kien.xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Danh sách sự kiện");

                ws.Cells["A1"].LoadFromDataTable(dt, true);

                using (var rng = ws.Cells[1, 1, 1, dt.Columns.Count])
                {
                    rng.Style.Font.Bold = true;
                    rng.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    rng.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    rng.Style.Font.Color.SetColor(Color.White);
                }

                if (ws.Dimension != null)
                {
                    ws.Cells[ws.Dimension.Address].Style.WrapText = true;
                    ws.Cells[ws.Dimension.Address].AutoFitColumns();
                }

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }

        public static string GetEnumDisplayName(Enum enumValue)
        {
            var displayAttr = enumValue.GetType()
                .GetField(enumValue.ToString())
                ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;

            return displayAttr?.Name ?? enumValue.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using OceanEduSlide.Utils;
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
using Z.EntityFramework.Plus;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using OceanEduSlide.Migrations;
using System.Globalization;
using System.Text;
namespace OceanEduSlide.Controllers
{
    [Authorize, AdminRoleFilters]
    public class VcmsController : BaseController
    {
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
                    model.Hotline = config.Hotline;
                    model.Email = config.Email;
                    model.AutoRevenue = config.AutoRevenue;
                    model.AutoUser = config.AutoUser;
                    //model.AutoCallLog = config.AutoCallLog;
                    model.AutoDebt = config.AutoDebt;
                    model.LiveChat = config.LiveChat;
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
        //        //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName");

        //        return RedirectToAction("CreateTarget", new { result = "add" });

        //    }
        //    else
        //    {
        //        return HttpNotFound();
        //    }
        //}
        public ActionResult SyncOfficeNames()
        {

            var users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeIds != null && string.IsNullOrEmpty(a.OfficeNames)).ToList();
            foreach (var item in users)
            {
                var officeNames = "";
                var listId = item.OfficeIds.Trim(',').Split(',');
                foreach (var id in listId)
                {
                    var idInt = int.Parse(id);
                    var office = _unitOfWork.OfficeRepository.GetById(idInt);
                    officeNames += office.ShortCode + ",";
                }
                officeNames = officeNames.Trim(',');
                item.OfficeNames = officeNames;
            }
            _unitOfWork.Save();
            return Content("ok");
        }
        public ActionResult CreateUser(string result = "")
        {

            if (Role != RoleAdmin.Admin)
                return RedirectToAction("Index", new { roll = "NoPermisstion" });
            ViewBag.Result = result;
            var model = new CreateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active),
                Zones = _unitOfWork.ZoneRepository.Get(a => a.Active && a.ShortCode != null),

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
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(a => a.Active), "Id", "ShortName");
                    model.SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(a => a.Active), "Id", "Name");
                    return View(model);
                }
                var exist2 = _unitOfWork.UserRepository.GetQuery().Any(z => !string.IsNullOrEmpty(model.MaNhanVien) && z.MaNhanVien.Equals(model.MaNhanVien));
                if (exist2)
                {
                    ModelState.AddModelError("", @"Đã tồn tại nhân sự có mã nhân viên " + model.MaNhanVien);
                    model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName");
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
                    var catIds2 = fc.GetValues("CatIDs2");
                    if (catIds2 != null)
                    {
                        foreach (var item in catIds2)
                        {
                            model.ZoneIds += (item + ",");
                        }
                        model.ZoneIds = "," + model.ZoneIds;
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
                        ZoneIds = model.ZoneIds,
                        OfficeNames = model.OfficeNames,
                        CDCM = model.CDCM
                    };
                    _unitOfWork.UserRepository.Insert(m);
                    _unitOfWork.Save();
                    //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName");

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
                users = users.Where(l => l.Office != null && l.Office.ZoneId == zoneId || l.ZoneId == zoneId);
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName"),
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Users = users,
                Offices = _unitOfWork.OfficeRepository.Get(a => a.Active),
                Zones = _unitOfWork.ZoneRepository.Get(a => a.Active && a.ShortCode != null)
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

            if (!string.IsNullOrEmpty(user.ZoneIds))
            {
                model.CatIds2 = user.ZoneIds
                                        .Split(',')
                                        .Select(x => x.Trim())
                                        .ToList();
            }
            model.ZoneIds = user.ZoneIds;

            var zId = user.ZoneId;
            if (zId != null)
                model.SelectOffices = OfficeSelectList(zId);

            model.OfficeId = user.OfficeId ?? 0;
            model.ZoneId = user.ZoneId ?? 0;
            model.TypeUser = user.TypeUser ?? null;
            model.CDCM = user.CDCM;
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
                var catIds2 = fc.GetValues("CatIDs2");
                if (catIds2 != null)
                {
                    foreach (var item in catIds2)
                    {
                        model.ZoneIds += (item + ",");
                    }
                    model.ZoneIds = "," + model.ZoneIds;
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
                    user.ZoneIds = model.ZoneIds;
                    user.OfficeNames = model.OfficeNames;
                    user.CDCM = model.CDCM;
                    _unitOfWork.Save();
                    return RedirectToAction("ListUser", new { result = "update" });
                }
            }
            return HttpNotFound();

        }

        public ActionResult ListMapTypeUser(string CDCM, int? UserType, int? active, string result = "")
        {
            ViewBag.Result = result;
            var maptypes = _unitOfWork.MapTypeUserRepository.GetQuery(orderBy: l => l.OrderBy(a => a.TypeUser).ThenBy(a => a.Sort));
            if (UserType.HasValue)
            {
                maptypes = maptypes.Where(l => (int)l.TypeUser == UserType);
            }
            if (active == 1)
            {
                maptypes = maptypes.Where(l => l.Active);
            }
            if (active == 2)
            {
                maptypes = maptypes.Where(l => !l.Active);
            }
            if (!string.IsNullOrEmpty(CDCM))
            {
                var newkey = CDCM.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    maptypes = maptypes.Where(l => l.CDCM == newkey);
                }
            }

            var model = new ListMapTypeViewModel
            {
                CDCM = CDCM,
                active = active,
                TypeUser = UserType,
                MapTypeUsers = maptypes
            };

            return View(model);
        }
        public ActionResult MapTypeUser()
        {
            var model = new MapTypeUser();
            return View(model);
        }
        [HttpPost]
        public ActionResult MapTypeUser(MapTypeUser model)
        {
            if (ModelState.IsValid)
            {
                var exist = _unitOfWork.MapTypeUserRepository.GetQuery().Any(z => z.CDCM.Equals(model.CDCM));
                if (exist)
                {
                    ModelState.AddModelError("", @"CDCM này đã tồn tại");
                    return View(model);
                }
                else
                {
                    model.Admin = Fullname;
                    _unitOfWork.MapTypeUserRepository.Insert(model);
                    _unitOfWork.Save();
                    return RedirectToAction("ListMapTypeUser", new { result = "add" });
                }
            }
            return RedirectToAction("ListMapTypeUser");
        }
        public ActionResult UpdateMapTypeUser(int mapTypeUserId = 0)
        {
            var mapTypeUser = _unitOfWork.MapTypeUserRepository.GetById(mapTypeUserId);
            if (mapTypeUser == null)
            {
                return RedirectToAction("ListMapTypeUser");
            }
            return View(mapTypeUser);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult UpdateMapTypeUser(MapTypeUser model)
        {
            var MapTypeUser = _unitOfWork.MapTypeUserRepository.GetById(model.Id);
            if (MapTypeUser == null)
            {
                return RedirectToAction("ListMapTypeUser");
            }
            if (ModelState.IsValid)
            {
                MapTypeUser.Edit = true;
                MapTypeUser.LastEdit = "by " + Fullname + ", at " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                MapTypeUser.Active = model.Active;
                MapTypeUser.Sort = model.Sort;
                MapTypeUser.CDCM = model.CDCM;
                MapTypeUser.TypeUser = model.TypeUser;
                _unitOfWork.Save();
                return RedirectToAction("ListMapTypeUser", new { result = "update" });
            }
            return View(model);
        }
        [HttpPost]
        public JsonResult DeleteMapTypeUser(int id)
        {
            var map = _unitOfWork.MapTypeUserRepository.GetById(id);
            _unitOfWork.MapTypeUserRepository.Delete(map);
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa thành công" });

        }
        [HttpPost]
        public bool QuickUpdateUser(bool active, int userId = 0)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            if (user == null)
            {
                return false;
            }
            user.Active = active;
            _unitOfWork.Save();
            return true;
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
        public ActionResult DeleteUserx2MNS()
        {
            var users = _unitOfWork.UserRepository.GetQuery().GroupBy(a => a.MaNhanVien).Where(g => g.Count() > 1).Select(g => g.OrderByDescending(u => u.Id).FirstOrDefault());
            var count = users.Count();
            foreach (var user in users)
            {
                var listCallLog = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list0 = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list1 = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list2 = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list3 = _unitOfWork.RevenueUser_MonthRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list4 = _unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list5 = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list6 = _unitOfWork.RevenueUser_Week_RealRepository.GetQuery(a => a.HistoryUserId != null && a.HistoryUser.UserId == user.Id);
                var list7 = _unitOfWork.ProposalRepository.GetQuery(a => a.UserId2 != null && a.UserId2 == user.Id);
                var list8 = _unitOfWork.DebtRepository.GetQuery(a => a.UserId == user.Id);
                var list9 = _unitOfWork.HistoryUserRepository.GetQuery(a => a.UserId == user.Id);

                listCallLog.Delete();
                list0.Delete();
                list1.Delete();
                list2.Delete();
                list3.Delete();
                list4.Delete();
                list5.Delete();
                list6.Delete();
                list7.Delete();
                list8.Delete();
                list9.Delete();
                _unitOfWork.UserRepository.Delete(user);
            }
            _unitOfWork.Save();
            return Content("Xóa tài khoản thành công" + count + "TK");

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
        public ActionResult DeleteUserx2NgayVaoLam(int month)
        {
            var list = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Month == month).GroupBy(a => new { a.User.MaNhanVien, a.TypeUser, a.OfficeId, a.Status }).Where(g => g.Count() >= 2).Select(g => g.OrderBy(x => x.DayStart).FirstOrDefault()).ToList();
            //var list = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active && a.Status == StatusUser.Active && a.Month == 10).GroupBy(a => new { a.User.MaNhanVien, a.TypeUser, a.OfficeId }).Select(g => g.OrderBy(x => x.DayStart).FirstOrDefault()).ToList();
            int i = 0;
            foreach (var l in list)
            {
                l.Active = false;
                i++;
            }
            _unitOfWork.Save();

            var listUnActive = _unitOfWork.HistoryUserRepository.GetQuery(a => !a.Active && a.Month == month).Select(a => a.Id).ToList();
            foreach (var l in listUnActive)
            {
                var listCallLog = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == l);
                listCallLog.Delete();
            }
            return Content("Đã xóa " + i + " User vào làm lại");
        }

        public ActionResult ListHistoryUser(int? page, string username, int? zoneId, int? officeId, int? month, int? year, int? UserType, int? trung, int? active, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var users = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.OfficeId).ThenBy(a => a.User.MaNhanVien).ThenBy(a => a.TypeUser));
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
            if (trung == 1)
            {
                var duplicatedMaNhanViens = users.Where(u => u.Status == StatusUser.Active)
                    .GroupBy(u => u.User.MaNhanVien)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                users = users.Where(u => duplicatedMaNhanViens.Contains(u.User.MaNhanVien) && u.User.MaNhanVien != null);
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
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "ShortName"),
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
            if (user != null)
            {
                user.Active = false;
                _unitOfWork.Save();
                var listCallLog = _unitOfWork.CallLogRepository.GetQuery(a => a.HistoryUserId == user.Id);
                listCallLog.Delete();
                return Json(new { status = true, msg = "Xóa thành công" });
            }
            return Json(new { status = false, msg = "Có lỗi xảy ra" });

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
        public ActionResult ChangeAEC(int month)
        {
            var users = _unitOfWork.HistoryUserRepository.GetQuery(a => a.OfficeId == null && a.Month == month && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.User));
            foreach (var user in users)
            {
                user.TypeUser = TypeUser.AEC;
            }
            _unitOfWork.Save();
            return RedirectToAction("ListHistoryUser");
        }
        public ActionResult ChangePassVico()
        {
            var users = _unitOfWork.UserRepository.Get();
            foreach (var user in users)
            {
                //user.Password = HtmlHelpers.ComputeHash("vico", "SHA256", null);
                //user.OldAcount = true;
                //user.SaleKit = true;
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
                    int row = 1;
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        row++;
                        var index = tbl.Rows[i][0].ToString().Trim();
                        var month = tbl.Rows[i][1].ToString().Trim();
                        int monthInt = 0;
                        if (string.IsNullOrEmpty(month) || !int.TryParse(month, out monthInt))
                        {
                            ModelState.AddModelError("", @"Dòng " + row + ": Cột tháng không thể chuyển thành dạng số");
                            ViewBag.Type = 3;
                            return View();
                        }
                        var zone = tbl.Rows[i][2].ToString().Trim();
                        var officescode = tbl.Rows[i][3].ToString().Trim();
                        officescode = VietnameseCodeHelper.NormalizeVietnameseCode(officescode);
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
                            Month = monthInt,
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
                    offices = offices.Where(l => l.Name.Contains(newkey) || l.ShortName.Contains(newkey) || l.ShortCode.Contains(newkey));
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
            var model = new InsertOfficeViewModel()
            {
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Office = new Office()
            };
            return View(model);
        }
        [HttpPost, ValidateInput(false)]
        public ActionResult Office(InsertOfficeViewModel model, FormCollection fc)
        {
            if (ModelState.IsValid)
            {
                var isPost = true;
                var oldOffice = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == model.ShortCode || a.ShortName == model.ShortName || a.Name == model.Office.Name).FirstOrDefault();
                if (oldOffice != null)
                {
                    isPost = false;
                    ModelState.AddModelError("", "Đã tồn tại chi nhánh có Tên, Tên viết tắt hoặc Mã chi nhánh vừa nhập");
                }
                if (DateTime.TryParse(model.OpenDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                {
                    var Date = new DateTime(cd.Year, cd.Month, cd.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                    model.Office.OpenDate = Date;
                }
                else
                {
                    isPost = false;
                    ModelState.AddModelError("", "Ngày khai trương không hợp lệ");
                }
                if (isPost)
                {
                    model.Office.ZoneId = model.ZoneId;
                    model.Office.ShortName = VietnameseCodeHelper.NormalizeVietnameseCode(model.ShortName);
                    model.Office.ShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(model.ShortCode);
                    _unitOfWork.OfficeRepository.Insert(model.Office);
                    _unitOfWork.Save();

                    var zone = _unitOfWork.ZoneRepository.GetById(model.ZoneId);
                    if (string.IsNullOrEmpty(zone.OfficeIds))
                    {
                        zone.OfficeIds = ",";
                    }
                    if (string.IsNullOrEmpty(zone.ShortName))
                    {
                        zone.ShortName = "";
                    }
                    zone.OfficeIds += model.Office.Id + ",";
                    zone.ShortName += "," + model.ShortCode;
                    zone.ShortName = zone.ShortName.Trim(',');
                    _unitOfWork.Save();
                    return RedirectToAction("ListOffice", new { result = "success" });
                }
            }
            model.SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name");
            return View(model);
        }
        //public ActionResult UpdateOffice(int officeId = 0)
        //{
        //    var office = _unitOfWork.OfficeRepository.GetById(officeId);
        //    if (office == null)
        //    {
        //        return RedirectToAction("ListOffice");
        //    }
        //    var model = new InsertOfficeViewModel
        //    {
        //        Office = office,
        //        ShortName = office.ShortName,
        //        ShortCode = office.ShortCode,
        //        OpenDate = office.OpenDate?.ToString("dd/MM/yyyy"),
        //        SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name")
        //    };
        //    if (office.ZoneId.HasValue)
        //        model.ZoneId = office.ZoneId.Value;
        //    return View(model);
        //}
        //[HttpPost, ValidateInput(false)]
        //public ActionResult UpdateOffice(InsertOfficeViewModel model)
        //{
        //    var Office = _unitOfWork.OfficeRepository.GetById(model.Office.Id);
        //    if (Office == null)
        //    {
        //        return RedirectToAction("ListOffice");
        //    }
        //    if (ModelState.IsValid)
        //    {
        //        var isPost = true;

        //        if (DateTime.TryParse(model.OpenDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
        //        {
        //            var Date = new DateTime(cd.Year, cd.Month, cd.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
        //            Office.OpenDate = Date;
        //        }
        //        else
        //        {
        //            isPost = false;
        //            ModelState.AddModelError("", "Ngày khai trương không hợp lệ");
        //        }
        //        if (isPost)
        //        {
        //            if (Office.ZoneId != null)
        //            {
        //                var oldZone = Office.Zone;
        //                //Hủy OfficeIds và ShortName cũ
        //                if (!string.IsNullOrEmpty(oldZone.OfficeIds))
        //                {
        //                    if (oldZone.OfficeIds.Contains("," + Office.Id + ","))
        //                        oldZone.OfficeIds = oldZone.OfficeIds.Replace("," + Office.Id + ",", ",");
        //                }
        //                if (!string.IsNullOrEmpty(oldZone.ShortName))
        //                {
        //                    oldZone.ShortName = "," + oldZone.ShortName + ",";
        //                    if (oldZone.ShortName.Contains("," + Office.ShortCode + ","))
        //                        oldZone.ShortName = oldZone.ShortName.Replace("," + Office.ShortCode + ",", ",");
        //                    oldZone.ShortName = oldZone.ShortName.Trim(',');
        //                }
        //            }

        //            Office.ZoneId = model.ZoneId;
        //            var zone = _unitOfWork.ZoneRepository.GetById(model.ZoneId);
        //            if (string.IsNullOrEmpty(zone.OfficeIds))
        //            {
        //                zone.OfficeIds = ",";
        //            }
        //            if (string.IsNullOrEmpty(zone.ShortName))
        //            {
        //                zone.ShortName = "";
        //            }
        //            if (!zone.OfficeIds.Contains("," + Office.Id + ","))
        //                zone.OfficeIds += model.Office.Id + ",";
        //            if (!("," + zone.ShortName + ",").Contains("," + Office.ShortCode + ","))
        //                zone.ShortName += "," + model.ShortCode;
        //            zone.ShortName = zone.ShortName.Trim(',');

        //            Office.ShortName = VietnameseCodeHelper.NormalizeVietnameseCode(model.ShortName);
        //            Office.ShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(model.ShortCode);
        //            Office.Name = VietnameseCodeHelper.NormalizeVietnameseCode(model.Office.Name);
        //            Office.Active = model.Office.Active;
        //            Office.Sort = model.Office.Sort;
        //            Office.Email = model.Office.Email;
        //            Office.Hotline = model.Office.Hotline;
        //            Office.Place = model.Office.Place;
        //            _unitOfWork.Save();
        //            return RedirectToAction("ListOffice", new { result = "update" });
        //        }

        //    }

        //    //DebugModelState();
        //    model.SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name");
        //    return View(model);
        //}
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
                    if (shortcode == "") continue;
                    shortcode = VietnameseCodeHelper.NormalizeVietnameseCode(shortcode);
                    var shortname = tbl.Rows[i][2].ToString().Trim();
                    if (shortname == "") continue;
                    shortname = VietnameseCodeHelper.NormalizeVietnameseCode(shortname);
                    var fullname = tbl.Rows[i][3].ToString().Trim();
                    if (fullname == "") continue;
                    fullname = VietnameseCodeHelper.NormalizeVietnameseCode(fullname);
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
        public ActionResult ChangeOfficeAndZoneVietnameseCode()
        {
            var offices = _unitOfWork.OfficeRepository.Get();
            var countOffice = 0;
            var countZone = 0;
            foreach (var item in offices)
            {
                var newName = VietnameseCodeHelper.NormalizeVietnameseCode(item.Name);
                var newShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(item.ShortCode);
                var newShortName = VietnameseCodeHelper.NormalizeVietnameseCode(item.ShortName);
                if (item.Name != newName || item.ShortCode != newShortCode || item.ShortName != newShortName)
                {
                    countOffice++;
                    item.Name = newName;
                    item.ShortCode = newShortCode;
                    item.ShortName = newShortName;
                }

            }
            var zones = _unitOfWork.ZoneRepository.Get();
            foreach (var item in zones)
            {
                var newName = VietnameseCodeHelper.NormalizeVietnameseCode(item.Name);
                var newShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(item.ShortCode);
                var newShortName = VietnameseCodeHelper.NormalizeVietnameseCode(item.ShortName);
                if (item.Name != newName || item.ShortCode != newShortCode || item.ShortName != newShortName)
                {
                    countZone++;
                    item.Name = newName;
                    item.ShortCode = newShortCode;
                    item.ShortName = newShortName;
                }
            }
            _unitOfWork.Save();
            return Content("Đã đồng bộ mã ký tự "+ countOffice + " chi nhánh và " + countZone + " vùng");
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
                    officescode = VietnameseCodeHelper.NormalizeVietnameseCode(officescode);
                    var fullname = tbl.Rows[i][1].ToString().Trim();
                    if (fullname == "") continue;
                    fullname = VietnameseCodeHelper.NormalizeVietnameseCode(fullname);
                    var shortcode = tbl.Rows[i][2].ToString().Trim();
                    if (shortcode == "") continue;
                    shortcode = VietnameseCodeHelper.NormalizeVietnameseCode(shortcode);
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
                model.Zone.Name = VietnameseCodeHelper.NormalizeVietnameseCode(model.Zone.Name);
                model.Zone.ShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(model.Zone.ShortCode);
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
            return HttpNotFound();
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
                    zone.OfficeIds = "";
                    if (catIds != null)
                    {
                        foreach (var item in catIds)
                        {
                            zone.OfficeIds += (item + ",");
                        }
                        zone.OfficeIds = "," + zone.OfficeIds;
                    }
                    zone.Name = VietnameseCodeHelper.NormalizeVietnameseCode(model.Zone.Name);
                    zone.ShortCode = VietnameseCodeHelper.NormalizeVietnameseCode(model.Zone.ShortCode);
                    zone.Active = model.Zone.Active;
                    //_unitOfWork.Save();
                    zone.ShortName = "";
                    if (zone.OfficeIds != "")
                    {
                        string[] a = zone.OfficeIds.Trim(',').Split(',');
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
                    }

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

        public ActionResult ClearDiscount(string name, string Cth, string startDate, string endDate, string createDate, string officeId, string result = "")
        {
            var discounts = _unitOfWork.DiscountRepository.GetQuery();
            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateTime.TryParse(startDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.StartDate) <= DbFunctions.TruncateTime(pd));
                }
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(endDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.EndDate) >= DbFunctions.TruncateTime(pd));
                }
            }
            if (!string.IsNullOrEmpty(createDate))
            {
                if (DateTime.TryParse(createDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.CreateDate) == DbFunctions.TruncateTime(pd));
                }
            }
            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    discounts = discounts.Where(l => l.Username.Contains(newkey));
                }
            }
            if (!string.IsNullOrEmpty(Cth))
            {
                discounts = discounts.Where(l => l.Cth.Contains(Cth));
            }
            if (!string.IsNullOrEmpty(officeId))
            {
                discounts = discounts.Where(a => ("," + a.Offices + ",").Contains("," + officeId + ","));
            }
            var countDelete = discounts.Count();
            discounts.Delete();
            return RedirectToAction("ListDiscount", new { countDelete });
        }
        public ActionResult ListDiscount(int? page, string name, string Cth, string startDate, string endDate, string createDate, string officeId, int countDelete = 0, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var discounts = _unitOfWork.DiscountRepository.GetQuery(orderBy: l => l.OrderBy(a => a.Id));

            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateTime.TryParse(startDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.StartDate) <= DbFunctions.TruncateTime(pd));
                }
            }
            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(endDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.EndDate) >= DbFunctions.TruncateTime(pd));
                }
            }
            if (!string.IsNullOrEmpty(createDate))
            {
                if (DateTime.TryParse(createDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    discounts = discounts.Where(l => DbFunctions.TruncateTime(l.CreateDate) == DbFunctions.TruncateTime(pd));
                }
            }
            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    discounts = discounts.Where(l => l.Username.Contains(newkey));
                }
            }
            if (!string.IsNullOrEmpty(Cth))
            {
                discounts = discounts.Where(l => l.Cth.Contains(Cth));
            }
            if (!string.IsNullOrEmpty(officeId))
            {
                discounts = discounts.Where(a => ("," + a.Offices + ",").Contains("," + officeId + ","));
            }
            var model = new ListDiscountViewModel
            {
                Discounts = discounts.ToPagedList(pageNumber, pageSize),
                Name = name,
                Cth = Cth,
                CreateDate = createDate,
                StartDate = startDate,
                EndDate = endDate,
                officeId = officeId,
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "ShortCode", "ShortName"),
            };
            ViewBag.countDelete = countDelete;
            return View(model);
        }
        public bool DeleteDiscount(int discountId)
        {

            var model = _unitOfWork.DiscountRepository.GetById(discountId);
            if (model != null)
                _unitOfWork.DiscountRepository.Delete(model);
            _unitOfWork.Save();

            return true;
        }
        public ActionResult CreateDiscount(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateDiscountViewModel();
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateDiscount(CreateDiscountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var isPost = true;

                if (model.Discount.Offices.Contains(" "))
                {
                    ModelState.AddModelError("", "Ô Các chi nhánh áp dụng không được chứa khoảng trắng (dấu cách)");
                    isPost = false;
                }
                model.Discount.Offices = VietnameseCodeHelper.NormalizeVietnameseCode(model.Discount.Offices);
                var listOfficeCode = model.Discount.Offices.Split(',');
                if (listOfficeCode.Any(a => string.IsNullOrEmpty(a)))
                {
                    ModelState.AddModelError("", "Kiểm tra lại định dạng Các chi nhánh áp dụng, các chi nhánh ngăn cách bởi 01 dấu phẩy");
                    isPost = false;
                }
                foreach (var item in listOfficeCode)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                        if (office == null)
                        {
                            ModelState.AddModelError("", "Kiểm tra lại dữ liệu Các chi nhánh áp dụng, không có Chi nhánh nào có Mã chi nhánh là " + item);
                            isPost = false;
                        }
                    }
                }
                if (DateTime.TryParse(model.StartDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    model.Discount.StartDate = pd;
                }
                if (DateTime.TryParse(model.EndDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd2))
                {
                    model.Discount.EndDate = pd2;
                }
                if (model.Discount.EndDate != null && model.Discount.StartDate != null && model.Discount.StartDate > model.Discount.EndDate)
                {
                    ModelState.AddModelError("", "Ngày hiệu lực không được lớn hơn ngày hết hạn");
                    isPost = false;
                }
                if (model.Discount.Pathway > model.Discount.PathwayTo)
                {
                    ModelState.AddModelError("", "Số tháng từ không được lớn hơn số tháng đến");
                    isPost = false;
                }
                if (isPost)
                {
                    if (model.MoneyDiscount != null)
                    {
                        model.Discount.MoneyDiscount = Convert.ToInt32(model.MoneyDiscount.Replace(",", ""));
                    }
                    model.Discount.Cth = model.Cth;
                    _unitOfWork.DiscountRepository.Insert(model.Discount);
                    _unitOfWork.Save();
                    return RedirectToAction("CreateDiscount", new { result = "add" });
                }
            }
            return View(model);

        }
        public ActionResult UpdateDiscount(int dcId)
        {
            var dc = _unitOfWork.DiscountRepository.GetById(dcId);
            if (dc == null)
            {
                return RedirectToAction("ListDisCount");
            }
            var model = new CreateDiscountViewModel()
            {
                Discount = dc,
                StartDate = dc.StartDate?.ToString("dd/MM/yyyy"),
                EndDate = dc.EndDate?.ToString("dd/MM/yyyy"),
                Cth = dc.Cth,
                MoneyDiscount = dc.MoneyDiscount?.ToString("N0")
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateDiscount(CreateDiscountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var isPost = true;

                if (model.Discount.Offices.Contains(" "))
                {
                    ModelState.AddModelError("", "Ô Các chi nhánh áp dụng không được chứa khoảng trắng (dấu cách)");
                    isPost = false;
                }
                model.Discount.Offices = VietnameseCodeHelper.NormalizeVietnameseCode(model.Discount.Offices);
                var listOfficeCode = model.Discount.Offices.Split(',');
                if (listOfficeCode.Any(a => string.IsNullOrEmpty(a)))
                {
                    ModelState.AddModelError("", "Kiểm tra lại định dạng Các chi nhánh áp dụng, các chi nhánh ngăn cách bởi 01 dấu phẩy");
                    isPost = false;
                }
                foreach (var item in listOfficeCode)
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        var office = _unitOfWork.OfficeRepository.GetQuery(a => a.ShortCode == item).FirstOrDefault();
                        if (office == null)
                        {
                            ModelState.AddModelError("", "Kiểm tra lại dữ liệu Các chi nhánh áp dụng, không có Chi nhánh nào có Mã chi nhánh là " + item);
                            isPost = false;
                        }
                    }
                }
                if (DateTime.TryParse(model.StartDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd))
                {
                    model.Discount.StartDate = pd;
                }
                if (DateTime.TryParse(model.EndDate, new CultureInfo("vi-VN"), DateTimeStyles.None, out var pd2))
                {
                    model.Discount.EndDate = pd2;
                }
                if (model.Discount.EndDate != null && model.Discount.StartDate != null && model.Discount.StartDate > model.Discount.EndDate)
                {
                    ModelState.AddModelError("", "Ngày hiệu lực không được lớn hơn ngày hết hạn");
                    isPost = false;
                }
                if (model.Discount.Pathway > model.Discount.PathwayTo)
                {
                    ModelState.AddModelError("", "Số tháng từ không được lớn hơn số tháng đến");
                    isPost = false;
                }
                if (isPost)
                {
                    if (model.MoneyDiscount != null)
                    {
                        model.Discount.MoneyDiscount = Convert.ToInt32(model.MoneyDiscount.Replace(",", ""));
                    }
                    model.Discount.Cth = model.Cth;
                    _unitOfWork.DiscountRepository.Update(model.Discount);
                    _unitOfWork.Save();
                    return RedirectToAction("ListDiscount", new { result = "add" });
                }
            }
            return View(model);

        }
        public ActionResult InsertDiscountExcel()
        {
            return View();
        }
        [HttpPost]
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
                int sheet = 0;
                var isPost = true;
                var listOffice = _unitOfWork.OfficeRepository.GetQuery().AsNoTracking().ToList();
                foreach (DataTable tbl in result.Tables)
                {
                    sheet++;
                    for (var i = 1; i < tbl.Rows.Count; i++)
                    {
                        var dong = i + 1;
                        var fullname = tbl.Rows[i][0].ToString().Trim();
                        if (string.IsNullOrEmpty(fullname))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Mã ưu đãi: Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        int? moneyDiscount = int.TryParse(tbl.Rows[i][2].ToString().Trim(), out var r) ? (int?)r : null;
                        double? percentDiscount = double.TryParse(tbl.Rows[i][3].ToString().Trim(), out var r2) ? (double?)r2 : null;

                        var startDateStr = tbl.Rows[i][4].ToString().Trim();
                        var endDateStr = tbl.Rows[i][5].ToString().Trim();
                        var startDate = DateTime.TryParse(startDateStr, out var sDate) ? sDate : (DateTime?)null;
                        var endDate = DateTime.TryParse(endDateStr, out var eDate) ? eDate : (DateTime?)null;
                        int pathway = int.TryParse(tbl.Rows[i][10].ToString().Trim(), out var r3) ? r3 : 0;
                        int pathwayTo = int.TryParse(tbl.Rows[i][11].ToString().Trim(), out var r4) ? r4 : 0;
                        var gift = tbl.Rows[i][7].ToString().Trim();
                        var offices = tbl.Rows[i][9].ToString().Trim();
                        offices = VietnameseCodeHelper.NormalizeVietnameseCode(offices);
                        if (endDate != null && startDate != null && startDate > endDate)
                        {
                            ModelState.AddModelError("", "Ngày hiệu lực không được lớn hơn ngày hết hạn: Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        if (pathway > pathwayTo)
                        {
                            ModelState.AddModelError("", "Số tháng từ không được lớn hơn số tháng đến: Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        if (string.IsNullOrEmpty(offices))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Chi nhánh: Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        else
                        {
                            if (offices.Contains(" "))
                            {
                                ModelState.AddModelError("", "Ô Chi nhánh không được chứa khoảng trắng (dấu cách): Dòng " + dong + ", Sheet " + sheet);
                                isPost = false;
                            }
                            var listOfficeCode = offices.Split(',');
                            if (listOfficeCode.Any(a => string.IsNullOrEmpty(a)))
                            {
                                ModelState.AddModelError("", "Kiểm tra lại định dạng Chi nhánh, các chi nhánh ngăn cách bởi 01 dấu phẩy: Dòng " + dong + ", Sheet " + sheet);
                                isPost = false;
                            }
                            foreach (var item in listOfficeCode)
                            {
                                if (!string.IsNullOrEmpty(item))
                                {
                                    var office = listOffice.FirstOrDefault(a => a.ShortCode == item);
                                    if (office == null)
                                    {
                                        ModelState.AddModelError("", "Kiểm tra lại dữ liệu Chi nhánh, không có Chi nhánh nào có Mã chi nhánh là \"" + item + "\" - Dòng " + dong + ", Sheet " + sheet);
                                        isPost = false;
                                    }
                                }
                            }
                        }
                        var cth = tbl.Rows[i][12].ToString().Trim();
                        if (string.IsNullOrEmpty(cth))
                        {
                            ModelState.AddModelError("", @"Thiếu dữ liệu cột Chương trình học: Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        var listCTH = new List<string>() { "Anh văn nhi đồng", "Anh văn thiếu nhi", "T.A học thuật Trung học", "Luyện thi IELTS", "T.A giao tiếp quốc tế TOEIC" };
                        if (!listCTH.Contains(cth))
                        {
                            ModelState.AddModelError("", @"Không có chương trình học nào tên là + " + cth + ": Dòng " + dong + ", Sheet " + sheet);
                            isPost = false;
                        }
                        if (isPost)
                        {
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
                                StartDate = startDate,
                                EndDate = endDate,
                                Active = true
                            };
                            listDiscount.Add(discount);
                        }
                    }
                }
                if (isPost)
                {
                    if (listDiscount.Any())
                    {
                        _unitOfWork.DiscountRepository.InsertRange(listDiscount);
                    }
                    _unitOfWork.Save();
                    return RedirectToAction("ListDiscount", new { result = "add" });

                }
                return View();
            }
            return RedirectToAction("ListDiscount");
        }
        public ActionResult InsertGroupDiscountExcel()
        {
            return View();
        }
        [HttpPost]
        public ActionResult InsertGroupDiscountExcel(FormCollection fc)
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
                //var docPath = "/documents/logimport/" + DateTime.Now.ToString("yyyy/MM/dd");
                //HtmlHelpers.CreateFolder(Server.MapPath(docPath));
                //var docFileName = DateTime.Now.ToFileTimeUtc() + Path.GetExtension(file.FileName);
                //var logImport = new Models.LogImport
                //{
                //    Admin = Fullname,
                //    Name = Path.GetFileName(file.FileName),
                //    File = DateTime.Now.ToString("yyyy/MM/dd") + "/" + docFileName,
                //    TypeImport = TypeImport.Type7,
                //};
                //_unitOfWork.LogImportRepository.Insert(logImport);
                //_unitOfWork.Save();
                //// Lưu tệp tài liệu
                //var filePath = Path.Combine(Server.MapPath(docPath), docFileName);
                //file.SaveAs(filePath);
                var result = reader.AsDataSet();
                reader.Close();

                //var tbl = result.Tables[0];
                //var discounts = _unitOfWork.DiscountRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Id));
                var listOldGroup = _unitOfWork.GroupDiscountRepository.GetQuery();
                listOldGroup.Delete();
                var listGroup = new List<GroupDiscount>();
                var listReportCategoryAdd = new List<ReportCategory>();
                var listReportCategory = _unitOfWork.ReportCategoryRepository.GetQuery(a => a.Active && a.Group == 8 && a.ReportCategoryId == null).ToList();
                int sort = 0;
                var tbl = result.Tables[0];
                for (var i = 2; i < tbl.Rows.Count; i++)
                {
                    var dong = i + 1;
                    var soQD = tbl.Rows[i][2].ToString().Trim();
                    if (string.IsNullOrEmpty(soQD))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Số QĐ: Dòng " + dong);
                        return View();
                    }
                    var phanloai = tbl.Rows[i][3].ToString().Trim();
                    if (string.IsNullOrEmpty(phanloai))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Loại: Dòng " + dong);
                        return View();
                    }
                    var noidung = tbl.Rows[i][4].ToString().Trim();
                    var startDateStr = tbl.Rows[i][5].ToString().Trim();
                    DateTime? sDate = null;

                    if (!string.IsNullOrEmpty(startDateStr))
                    {
                        DateTime parsedDate;
                        if (DateTime.TryParse(startDateStr, out parsedDate))
                        {
                            if (parsedDate <= new DateTime(1753, 1, 1))
                            {
                                ModelState.AddModelError("", @"Ngày bắt đầu không hợp lệ: Dòng " + dong);
                                return View();
                            }
                            sDate = parsedDate;
                        }
                    }

                    var endDateStr = tbl.Rows[i][6].ToString().Trim();
                    DateTime? eDate = null;

                    if (!string.IsNullOrEmpty(endDateStr))
                    {
                        DateTime parsedDate;
                        if (DateTime.TryParse(endDateStr, out parsedDate))
                        {
                            if (parsedDate <= new DateTime(1753, 1, 1))
                            {

                                ModelState.AddModelError("", @"Ngày kết thúc không hợp lệ: Dòng " + dong);
                                return View();
                            }
                            eDate = parsedDate;
                        }
                    }
                    var note = tbl.Rows[i][7].ToString().Trim();
                    var nhomQD = tbl.Rows[i][8].ToString().Trim();
                    if (string.IsNullOrEmpty(nhomQD))
                    {
                        ModelState.AddModelError("", @"Thiếu dữ liệu cột Tên QĐ: Dòng " + dong);
                        return View();
                    }
                    int year = 0;
                    if (!nhomQD.Contains("/"))
                    {
                        ModelState.AddModelError("", @"Sai định dạng chung cột Tên QĐ (số QĐ/năm): Dòng " + dong);
                        return View();
                    }
                    var yearStr = nhomQD.Split('/')[1];
                    if (!int.TryParse(yearStr, out year))
                    {
                        ModelState.AddModelError("", @"Không thể tách chuỗi để lấy ra năm: Dòng " + dong);
                        return View();
                    }
                    var reportCategory = listReportCategory.FirstOrDefault(a => a.Name.ToLower().Contains(phanloai.ToLower()));
                    if (reportCategory == null)
                    {
                        reportCategory = listReportCategoryAdd.FirstOrDefault(a => a.Name.ToLower().Contains(phanloai.ToLower()));
                    }
                    if (reportCategory == null)
                    {
                        var name = phanloai;
                        if (phanloai.Length <= 12)
                        {
                            name = "Doanh thu " + phanloai;
                        }
                        var newReportCategory = new ReportCategory()
                        {
                            Name = name,
                            Sort = phanloai.ToLower().Contains("khác") ? 20 : sort,
                            Group = 8,
                            Active = true,
                            ReportCategoryId = null,
                            TypeCat = TypeCat.Type1,
                            Count = 1,
                            Auto = false,
                        };
                        _unitOfWork.ReportCategoryRepository.Insert(newReportCategory);
                        _unitOfWork.Save();
                        listReportCategoryAdd.Add(newReportCategory);
                        sort += 1;
                        var cateChild = new ReportCategory()
                        {
                            Name = "Kết quả",
                            Sort = 1,
                            Group = 8,
                            Active = true,
                            ReportCategoryId = newReportCategory.Id,
                            TypeCat = TypeCat.Type1,
                            Count = null,
                            Auto = true,
                        };
                        _unitOfWork.ReportCategoryRepository.Insert(cateChild);
                    }
                    var groupDiscount = new GroupDiscount
                    {
                        SoQD = soQD,
                        NhomQD = nhomQD,
                        Year = year,
                        PhanLoai = phanloai,
                        StartDate = sDate,
                        EndDate = eDate,
                        Content = noidung,
                        Note = note,
                    };
                    listGroup.Add(groupDiscount);
                }

                if (listGroup.Any())
                {
                    _unitOfWork.GroupDiscountRepository.InsertRange(listGroup);
                }
                _unitOfWork.Save();

            }
            return RedirectToAction("ListGroupDiscount", new { result = "add" });
        }

        public ActionResult ListGroupDiscount(int? page, string name, int? year, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var groupDiscounts = _unitOfWork.GroupDiscountRepository.GetQuery(orderBy: o => o.OrderByDescending(a => a.Year)).AsNoTracking();

            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    groupDiscounts = groupDiscounts.Where(l => l.NhomQD.Contains(newkey));
                }
            }
            if (year != null)
            {
                groupDiscounts = groupDiscounts.Where(l => l.Year == year);
            }
            var model = new ListGroupDiscountViewModel
            {
                GroupDiscounts = groupDiscounts.ToPagedList(pageNumber, pageSize),
                Name = name,
                Year = year,
            };
            return View(model);
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
                    typeFault.Sort = model.Sort;
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
        public ActionResult SyncZoneProposal(string result = "")
        {
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            var dexuats = _unitOfWork.ProposalRepository.GetQuery(a => a.CreateDate.Month == thisMonth && a.CreateDate.Year == thisYear);
            foreach (var item in dexuats)
            {
                if (item.Office.ZoneId != null)
                    item.ZoneId = item.Office.ZoneId ?? 0;
            }
            _unitOfWork.Save();
            return Content("Đã sync lại vùng đề xuất");
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
                    proposalType.Sort = model.Sort;
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

        #region Export
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
        public void ExportDiscount()
        {
            // Bước 1: Lấy dữ liệu cần export, chỉ lấy các cột cần thiết
            var discounts = _unitOfWork.DiscountRepository
                .GetQuery()
                .AsNoTracking()
                .ToList(); // Truy vấn DB ngay

            // Bước 2: Tạo file Excel và ghi dữ liệu trực tiếp vào worksheet
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Danh sách QĐ ưu đãi");

                // Header (dòng 1)
                ws.Cells[1, 1].Value = "Tên ưu đãi";
                ws.Cells[1, 2].Value = "Phần trăm ưu đãi";
                ws.Cells[1, 3].Value = "Ưu đãi tiền mặt";
                ws.Cells[1, 4].Value = "Quà tặng";
                ws.Cells[1, 5].Value = "Số tháng từ";
                ws.Cells[1, 6].Value = "Số tháng đến";
                ws.Cells[1, 7].Value = "Ngày hiệu lực";
                ws.Cells[1, 8].Value = "Ngày hết hạn";
                ws.Cells[1, 9].Value = "Chi nhánh";
                ws.Cells[1, 10].Value = "Chương trình học";
                ws.Cells[1, 11].Value = "Phân loại";

                // Ghi dữ liệu bắt đầu từ dòng 2
                int row = 2;
                foreach (var item in discounts)
                {
                    ws.Cells[row, 1].Value = item.Username;
                    ws.Cells[row, 2].Value = item.PercentDiscount;
                    ws.Cells[row, 3].Value = item.MoneyDiscount;
                    ws.Cells[row, 4].Value = item.Gift;
                    ws.Cells[row, 5].Value = item.Pathway;
                    ws.Cells[row, 6].Value = item.PathwayTo;
                    ws.Cells[row, 7].Value = item.StartDate?.ToString("dd/MM/yyyy");
                    ws.Cells[row, 8].Value = item.EndDate?.ToString("dd/MM/yyyy");
                    ws.Cells[row, 9].Value = item.Offices;
                    ws.Cells[row, 10].Value = item.Cth;
                    ws.Cells[row, 11].Value = item.PhanLoai;
                    row++;
                }

                // Optional: Tự động căn độ rộng cột
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                // Bước 3: Trả về file Excel qua HTTP response
                var filename = "danh-sach-qdud.xlsx";
                var excelBytes = pck.GetAsByteArray();

                // Thiết lập headers và gửi file
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(excelBytes);
                Response.End();
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
        public void ExportHistoryUser(int month)
        {
            var users = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Active, q => q.OrderByDescending(a => a.Year).ThenByDescending(a => a.Month).ThenBy(a => a.OfficeId == null).ThenByDescending(a => a.Office.ZoneId).ThenBy(a => a.OfficeId)).ToList();
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
        public void ExportHistoryUser2(int trung, int month)
        {
            var users = _unitOfWork.HistoryUserRepository.GetQuery(
    a => a.Active && a.Month == month && a.Year == DateTime.Now.Year && a.Status == StatusUser.Active,
    q => q.OrderByDescending(a => a.Year)
          .ThenByDescending(a => a.Month)
          .ThenBy(a => a.OfficeId == null)
          .ThenByDescending(a => a.Office.ZoneId)
          .ThenBy(a => a.OfficeId)
).ToList();

            if (trung == 1)
            {
                // Lọc ra những MaNhanVien bị trùng trong danh sách đã lọc theo StatusUser.Active
                var duplicatedMaNhanViens = users
                    .Where(u => !string.IsNullOrEmpty(u.User.MaNhanVien))
                    .GroupBy(u => u.User.MaNhanVien)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                users = users
                    .Where(u => duplicatedMaNhanViens.Contains(u.User.MaNhanVien))
                    .ToList();
            }

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
                dt.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.Office.ShortName, item.Office.Zone?.Name, item.Target_TS, item.Target_HV, item.Target_SAB);
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
                dt2.Rows.Add(i, item.Month.ToString() + " - " + item.Year.ToString(), item.HistoryUser.Office?.Zone?.Name, item.HistoryUser.Office?.ShortName, item.HistoryUser.User.Fullname, GetEnumDisplayName(item.HistoryUser.TypeUser), item.HistoryUser.DayStart, item.HistoryUser.DayEnd, GetEnumDisplayName(item.HistoryUser.Status), item.Target);

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
        public void ExportEvent2(int Year, int Month, int Week, int? OfficeId, int? ZoneId, int? TypeView)
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
        public void ExportPhieuThu()
        {
            var phieuthus = _unitOfWork.PhieuThuRepository.GetQuery();
            var dt = new DataTable();
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Ngày thanh toán");
            dt.Columns.Add("Đặc biệt");
            dt.Columns.Add("Loại");
            dt.Columns.Add("Số phiếu thu");
            dt.Columns.Add("Mã HV");
            dt.Columns.Add("Học viên");
            dt.Columns.Add("Trước ưu đãi");
            dt.Columns.Add("Sau ưu đãi");
            dt.Columns.Add("% Ưu đãi");
            dt.Columns.Add("Giới tính");
            dt.Columns.Add("Hình thức thanh toán");
            dt.Columns.Add("Ghi chú");
            dt.Columns.Add("Đăng ký");
            dt.Columns.Add("Giờ tạo");
            dt.Columns.Add("Mã nhân viên chốt sale");
            dt.Columns.Add("Tên nhân viên chốt sale");
            dt.Columns.Add("Mã cộng tác viên");
            dt.Columns.Add("Số tháng học dự kiến");
            dt.Columns.Add("CT Khuyến Mãi");
            dt.Columns.Add("Loại chương trình");
            dt.Columns.Add("Chương trình học");
            dt.Columns.Add("Cấp độ");
            dt.Columns.Add("Mô-đun");
            dt.Columns.Add("Mã nhóm CTUD");

            var filename = $"danh-sach-phieu-thu.xlsx";
            foreach (var item in phieuthus)
            {
                var thdb = item.THDB ? "x" : "";
                dt.Rows.Add(item.ChiNhanh, item.NgayThanhToan.Value.ToString("dd/MM/yyyy"), thdb, item.Loai, item.ReceiptCode, item.MaHV, item.TenHV, item.TUD, item.SUD, item.PhanTramUD, item.GioiTinh, item.HinhThucThanhToan, item.Notes,
                    item.DangKy, item.GioTao, item.MaNVChotSale, item.ChotSale, item.CongTacVien, item.ThangHocDuKien, item.UD_FINAL, item.LoaiCTH, item.ChuongTrinhHoc, item.CapDo, item.Modun, item.UD_NhomUDFINAL);
            }
            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                var ws = pck.Workbook.Worksheets.Add("Danh sách phiếu thu");

                //Load the datatable into the sheet, starting from cell A1. Print the column names on row 1
                ws.Cells["A1"].LoadFromDataTable(dt, true);

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
        public void ExportCallLogPaged()
        {

            int pageSize = 100000;
            int pageIndex = 0;
            string filename = "danh-sach-cuoc-goi.xlsx";
            using (var pck = new ExcelPackage())
            {
                while (true)
                {
                    var pageData = _unitOfWork.CallLogRepository
                        .GetQuery(a => a.HistoryUserId != null && a.BillSec >= 60, q => q.OrderBy(a => a.UniqueId))
                        .Skip(pageIndex * pageSize)
                        .Take(pageSize)
                        .ToList();

                    if (!pageData.Any()) break;

                    var sheetName = $"Trang {pageIndex + 1}";
                    var ws = pck.Workbook.Worksheets.Add(sheetName);

                    // Ghi tiêu đề
                    string[] headers = {
                "Chi nhánh", "Mã NV", "Tên NV", "Vị trí", "Trạng thái",
                "Ngày vào làm", "Ngày nghỉ/ điều chuyển", "Ngày gọi",
                "Duration", "BillSec", "Disposition", "Type"
            };

                    for (int col = 0; col < headers.Length; col++)
                    {
                        ws.Cells[1, col + 1].Value = headers[col];
                        ws.Cells[1, col + 1].Style.Font.Bold = true;
                        ws.Cells[1, col + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        ws.Cells[1, col + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }

                    // Ghi dữ liệu từng dòng
                    int row = 2;
                    foreach (var item in pageData)
                    {
                        ws.Cells[row, 1].Value = item.HistoryUser.Office?.Name;
                        ws.Cells[row, 2].Value = item.Exten;
                        ws.Cells[row, 3].Value = item.HistoryUser.User.Fullname;
                        ws.Cells[row, 4].Value = item.HistoryUser.TypeUser;
                        ws.Cells[row, 5].Value = GetEnumDisplayName(item.HistoryUser.Status);
                        ws.Cells[row, 6].Value = item.HistoryUser.DayStart;
                        ws.Cells[row, 7].Value = item.HistoryUser.DayEnd;
                        ws.Cells[row, 8].Value = item.CallDate;
                        ws.Cells[row, 9].Value = item.Duration;
                        ws.Cells[row, 10].Value = item.BillSec;
                        ws.Cells[row, 11].Value = item.Disposition;
                        ws.Cells[row, 12].Value = item.Type;
                        row++;
                    }

                    pageIndex++;
                }

                // Trả file về client
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportCallLog(int month)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Lấy toàn bộ cuộc gọi trong tháng 8 có HistoryUserId
            var allCallLogs = _unitOfWork.CallLogRepository
                .GetQuery(a => a.CallDate.Month == month && a.HistoryUserId != null && a.Type == "out");

            // Nhóm theo HistoryUserId
            var groupedData = allCallLogs
                .GroupBy(a => a.HistoryUserId)
                .Select(g => new
                {
                    HistoryUserId = g.Key,
                    TotalCalls = g.Count(),
                    CallsOver60Sec = g.Count(x => x.BillSec >= 60),
                    User = g.FirstOrDefault().HistoryUser
                })
                .ToList();

            var dt = new DataTable();
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Mã NV");
            dt.Columns.Add("Tên NV");
            dt.Columns.Add("Vị trí");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Ngày vào làm");
            dt.Columns.Add("Ngày nghỉ/ điều chuyển");
            dt.Columns.Add("Tổng cuộc gọi ra");
            dt.Columns.Add("Cuộc gọi >= 60s");

            foreach (var item in groupedData)
            {
                dt.Rows.Add(
                    item.User.Office?.Name,
                    item.User.User.MaNhanVien,
                    item.User.User.Fullname,
                    GetEnumDisplayName(item.User.TypeUser),
                    GetEnumDisplayName(item.User.Status),
                    item.User.DayStart.ToString("dd/MM/yyyy"),
                    item.User.DayEnd?.ToString("dd/MM/yyyy") ?? "",
                    item.TotalCalls,
                    item.CallsOver60Sec
                );
            }

            var filename = "thong-ke-cuoc-goi-thang-" + month + ".xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Thống kê cuộc gọi");
                ws.Cells["A1"].LoadFromDataTable(dt, true);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportDthuNV(int month)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            //var allDthuNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Month == month && a.ReportCategoryId == );
            var listHistoryUser = _unitOfWork.HistoryUserRepository.GetQuery(a => a.Month == month && a.TypeUser != TypeUser.ASM && a.TypeUser != TypeUser.CV && a.TypeUser != TypeUser.HO && a.TypeUser != TypeUser.ASM,
            q => q.OrderBy(a => a.OfficeId).ThenBy(a => a.Sort));

            var dt = new DataTable();
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Mã NV");
            dt.Columns.Add("Tên NV");
            dt.Columns.Add("Vị trí");
            dt.Columns.Add("Trạng thái");
            dt.Columns.Add("Ngày vào làm");
            dt.Columns.Add("Ngày nghỉ/ điều chuyển");
            dt.Columns.Add("Chỉ tiêu");
            dt.Columns.Add("Thực đạt");
            dt.Columns.Add("% HT");

            foreach (var item in listHistoryUser)
            {
                //var chitieu = ""; var thucdat = ""; var ht = "";

                var chitieu = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == month && a.ReportCategoryId == 87).FirstOrDefault()?.Data ?? "";
                var thucdat = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == month && a.ReportCategoryId == 88).FirstOrDefault()?.Data ?? "";
                var ht = _unitOfWork.ReportDataRepository.GetQuery(a => a.HistoryUserId == item.Id && a.Month == month && a.ReportCategoryId == 89).FirstOrDefault()?.Data ?? "";
                dt.Rows.Add(
                    item.Office?.Name,
                    item.User.MaNhanVien,
                    item.User.Fullname,
                    GetEnumDisplayName(item.TypeUser),
                    GetEnumDisplayName(item.Status),
                    item.DayStart.ToString("dd/MM/yyyy"),
                    item.DayEnd?.ToString("dd/MM/yyyy") ?? "",
                    chitieu,
                    thucdat,
                    ht
                );
            }

            var filename = "thong-ke-doanh-thu-NV-thang-" + month + ".xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Thống kê Doanh thu NV");
                ws.Cells["A1"].LoadFromDataTable(dt, true);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        public void ExportDthuCN(int month)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            //var allDthuNV = _unitOfWork.ReportDataRepository.GetQuery(a => a.Month == month && a.ReportCategoryId == );
            var listOffice = _unitOfWork.OfficeRepository.GetQuery(a => a.Active, q => q.OrderBy(a => a.ZoneId));

            var dt = new DataTable();
            dt.Columns.Add("Vùng");
            dt.Columns.Add("Chi nhánh");
            dt.Columns.Add("Chỉ tiêu");
            dt.Columns.Add("Thực đạt");
            dt.Columns.Add("% HT");

            foreach (var item in listOffice)
            {
                //var chitieu = ""; var thucdat = ""; var ht = "";

                var chitieu = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == item.Id && a.Month == month && a.ReportCategoryId == 34).FirstOrDefault()?.Data ?? "";
                var thucdat = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == item.Id && a.Month == month && a.ReportCategoryId == 35).FirstOrDefault()?.Data ?? "";
                var ht = _unitOfWork.ReportDataRepository.GetQuery(a => a.OfficeId == item.Id && a.Month == month && a.ReportCategoryId == 36).FirstOrDefault()?.Data ?? "";
                dt.Rows.Add(
                    item.Zone?.Name,
                    item.Name,
                    chitieu,
                    thucdat,
                    ht
                );
            }

            var filename = "thong-ke-doanh-thu-CN-thang-" + month + ".xlsx";
            using (var pck = new ExcelPackage())
            {
                var ws = pck.Workbook.Worksheets.Add("Thống kê Doanh thu CN");
                ws.Cells["A1"].LoadFromDataTable(dt, true);
                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={filename}");
                Response.BinaryWrite(pck.GetAsByteArray());
            }
        }
        #endregion

        #region CallLogCustom
        public async Task<ActionResult> SyncRecently()
        {
            var service = new CallLogService();
            await service.SyncRecentlyAsync();
            return Content("Đã đồng bộ 7 ngày gần đây");
        }
        public async Task<ActionResult> SyncToDay()
        {
            var service = new CallLogService();
            await service.SyncTodayAsync();
            return Content("Đã đồng bộ ngày hôm nay");
        }
        public async Task<ActionResult> SyncCustom(int month, int day)
        {
            var service = new CallLogService();
            await service.SyncCusTom(month, day);
            return Content("Đã đồng bộ 7 ngày. " + day + " - " + month);
        }
        public async Task<ActionResult> SyncDuplicate()
        {
            var service = new CallLogService();
            await service.SyncDuplicateAsync();
            return Content("Chuyển thành công cuộc gọi của các nhân sự tháng có cùng mã nhân viên");
        }
        #endregion
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
using Antlr.Runtime.Misc;
using ExcelDataReader;
using Helpers;
using OceanEduSlide.DAL;
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

namespace OceanEduSlide.Controllers
{
    [Authorize]
    public class VcmsController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private IEnumerable<Admin> Admins => _unitOfWork.AdminRepository.Get();

        #region Admin
        public ActionResult Index()
        {
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
                    model.AboutText = config.AboutText;
                    model.AboutBody = config.AboutBody;
                    model.AboutFooter = config.AboutFooter;
                    model.Customers = config.Customers;
                    //model.Agencies = config.Agencies;
                    model.Years = config.Years;
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
                    var ticket = new FormsAuthenticationTicket(1, model.Username.ToLower(), DateTime.Now, DateTime.Now.AddDays(30), true,
                        admin.ToString(), FormsAuthentication.FormsCookiePath);
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
            var model = new CreateAdminViewModel
            {
                Admins = Admins.Where(z => z.Id == id),
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
        public ActionResult CreateTarget(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateTargetViewModel
            {
                SelectUsers = new SelectList(_unitOfWork.UserRepository.Get(a => a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.CM), "Id", "Username"),
                Revenue = new RevenueUser_Month()
                {
                    Month = DateTime.Now.Month,
                    Year = DateTime.Now.Year,
                }
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateTarget(CreateTargetViewModel model)
        {
            if (ModelState.IsValid)
            {
                //var m = new RevenueUser_Month
                //{
                //    Target = model.Revenue.Target,
                //    UserId = model.Revenue.UserId,
                //    Year = model.Revenue.Year,
                //    Month = model.Revenue.Month,
                //};
                _unitOfWork.RevenueUser_MonthRepository.Insert(model.Revenue);
                _unitOfWork.Save();
                //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");

                return RedirectToAction("CreateTarget", new { result = "add" });

            }
            else
            {
                return HttpNotFound();
            }
        }
        public ActionResult CreateUser(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                //Users = Users,
            };
            return View(model);
        }
        [HttpPost]
        public ActionResult CreateUser(CreateUserViewModel model)
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
                else
                {
                    var m = new User
                    {
                        Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null),
                        Username = model.Username,
                        Fullname = model.Fullname,
                        OfficeId = model.OfficeId,
                        ZoneId = model.ZoneId,
                        Active = model.Active,
                        SaleKit = model.SaleKit,
                        TypeUser = model.TypeUser,
                    };
                    _unitOfWork.UserRepository.Insert(m);
                    _unitOfWork.Save();
                    //model.SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name");

                    return RedirectToAction("CreateUser", new { result = "add" });
                }
            }
            else
            {
                return HttpNotFound();
            }
        }

        public ActionResult ListUser(int? page, string username, int? officeId, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var users = _unitOfWork.UserRepository.GetQuery(orderBy: l => l.OrderByDescending(a => a.Id));

            if (officeId.HasValue)
            {
                users = users.Where(l => l.OfficeId == officeId);
            }
            if (username != null)
            {
                var newkey = username.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    users = users.Where(l => l.Username.Contains(newkey) || l.Fullname.Contains(newkey) || l.MaNhanVien.Contains(newkey));
                }
            }
            var model = new ListUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                Users = users.ToPagedList(pageNumber, pageSize),
                officeId = officeId,
                Username = username,
                MemberCredentials = _unitOfWork.MemberCredentialRepository.GetQuery(),
            };
            return View(model);
        }
        public ActionResult ListFaceId(int userId)
        {
            var model = _unitOfWork.MemberCredentialRepository.Get(a => a.UserId == userId);
            return View(model);
        }
        public ActionResult UpdateUser(int id)
        {
            var model = new CreateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                SelectZones = new SelectList(_unitOfWork.ZoneRepository.Get(), "Id", "Name"),
                Users = _unitOfWork.UserRepository.Get(z => z.Id == id),

            };
            model.OfficeId = model.Users.FirstOrDefault()?.OfficeId ?? 0;
            model.ZoneId = model.Users.FirstOrDefault()?.ZoneId ?? 0;
            model.TypeUser = model.Users.FirstOrDefault()?.TypeUser ?? null;
            return View(model);
        }
        [HttpPost]
        public ActionResult UpdateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _unitOfWork.UserRepository.GetQuery(z => z.Username == model.Username).FirstOrDefault();
                if (user != null)
                {
                    user.Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null);
                    user.OfficeId = model.OfficeId;
                    user.ZoneId = model.ZoneId;
                    user.Active = model.Active;
                    user.SaleKit = model.SaleKit;
                    user.TypeUser = model.TypeUser;
                    user.Fullname = model.Fullname;
                    _unitOfWork.Save();
                    return RedirectToAction("CreateUser", new { result = "update" });
                }
            }
            return HttpNotFound();

        }
        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            _unitOfWork.UserRepository.Delete(user);
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa tài khoản thành công" });

        }
        public ActionResult InsertUserExcel()
        {
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
                    var password = tbl.Rows[i][5].ToString().Trim();
                    if (password == "") continue;
                    var password2 = HtmlHelpers.ComputeHash(password, "SHA256", null);

                    var fullname = tbl.Rows[i][6].ToString().Trim();
                    var maxnhanvien = tbl.Rows[i][7].ToString().Trim();
                    var phanquyen = tbl.Rows[i][9].ToString().Trim();
                    var zones = tbl.Rows[i][10].ToString().Trim();
                    if (user != null)
                    {
                        user.Password = password2;
                        user.Active = true;
                        user.OfficeId = office?.Id ?? null;
                        user.MaNhanVien = maxnhanvien;
                        user.Fullname = fullname;
                        switch (phanquyen)
                        {
                            case "ASM":
                                user.TypeUser = TypeUser.ASM;
                                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zones).FirstOrDefault();
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
                                user.TypeUser = TypeUser.EC;
                                break;
                            case "BM":
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
                        user = new User
                        {
                            Username = username,
                            Password = password2,
                            Active = true,
                            OfficeId = office?.Id ?? null,
                            MaNhanVien = maxnhanvien,
                            Fullname = fullname,
                        };
                        switch (phanquyen)
                        {
                            case "ASM":
                                user.TypeUser = TypeUser.ASM;
                                var z = _unitOfWork.ZoneRepository.GetQuery(a => a.ShortCode == zones).FirstOrDefault();
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
                                user.TypeUser = TypeUser.EC;
                                break;
                            case "BM":
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
        public ActionResult DeleteUserWrong()
        {
            //var users = _unitOfWork.UserRepository.GetQuery(a => !string.IsNullOrEmpty(a.MaNhanVien) && a.MaNhanVien.Length < 8);
            //foreach (var user in users)
            //{
            //    user.Active = false;
            //}
            var userNoOffices = _unitOfWork.UserRepository.GetQuery(a => a.TypeUser == TypeUser.HO || a.TypeUser == TypeUser.CV || a.TypeUser == TypeUser.ASM);
            foreach (var user in userNoOffices)
            {
                user.OfficeId = null;
            }
            _unitOfWork.Save();
            return RedirectToAction("ListUser");
        }
        #endregion
        public ActionResult TestTable()
        {
            return View("TestTable2");
        }
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
            return RedirectToAction("ListUser");
        }
        #endregion

        #region Office
        public ActionResult ListOffice(int? page, string name, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var offices = _unitOfWork.OfficeRepository.GetQuery(orderBy: l => l.OrderBy(a => a.Sort));

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
                Offices = _unitOfWork.OfficeRepository.Get()
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
                    foreach (var item in a)
                    {
                        int officeId = int.Parse(item.ToString());
                        var office = _unitOfWork.OfficeRepository.GetById(officeId);
                        if (office != null)
                        {
                            office.ZoneId = zone.Id;
                        }
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
                    zone.Active = model.Zone.Active;
                    _unitOfWork.Save();
                    string[] a = zone.OfficeIds.Trim(',').Split(',');
                    foreach (var item in a)
                    {
                        int officeId = int.Parse(item);
                        var office = _unitOfWork.OfficeRepository.GetById(officeId);
                        if (office != null)
                        {
                            office.ZoneId = zone.Id;
                            if (!("," + zone.ShortName + ",").Contains("," + office.ShortCode + ","))
                                zone.ShortName += "," + office.ShortCode;
                        }
                    }
                    _unitOfWork.Save();

                    return RedirectToAction("CreateZone", new { result = "add" });
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
                var result = reader.AsDataSet();
                reader.Close();

                //var tbl = result.Tables[0];
                var discounts = _unitOfWork.DiscountRepository.GetQuery(a => a.Active, o => o.OrderBy(a => a.Id));
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
                        _unitOfWork.DiscountRepository.Insert(discount);
                    }
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

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
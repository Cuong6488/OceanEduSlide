using Helpers;
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
using OceanEduSlide.DAL;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using OceanEduSlide.Migrations;
using System.Text.RegularExpressions;
using ExcelDataReader;
namespace OceanEduSlide.Controllers
{
    [Authorize]
    public class VcmsController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private IEnumerable<Admin> Admins => _unitOfWork.AdminRepository.Get();

        #region Admin
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
        public ActionResult CreateUser(string result = "")
        {
            ViewBag.Result = result;
            var model = new CreateUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
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
                    return View(model);
                }
                else
                {
                    var m = new User
                    {
                        Password = HtmlHelpers.ComputeHash(model.Password, "SHA256", null),
                        Username = model.Username,
                        OfficeId = model.OfficeId,
                        Active = model.Active,
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

        public ActionResult ListUser(int? page, string name, int? officeId, string result = "")
        {
            ViewBag.Result = result;
            var pageNumber = page ?? 1;
            const int pageSize = 15;
            var users = _unitOfWork.UserRepository.GetQuery(orderBy: l => l.OrderByDescending(a => a.Id));

            if (officeId.HasValue)
            {
                users = users.Where(l => l.OfficeId == officeId);
            }
            if (name != null)
            {
                var newkey = name.Trim();
                if (!string.IsNullOrEmpty(newkey))
                {
                    users = users.Where(l => l.Username.Contains(newkey));
                }
            }
            var model = new ListUserViewModel
            {
                SelectOffices = new SelectList(_unitOfWork.OfficeRepository.Get(), "Id", "Name"),
                Users = users.ToPagedList(pageNumber, pageSize),
                officeId = officeId,
                Username = name,
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

                Users = _unitOfWork.UserRepository.Get(z => z.Id == id),

            };
            model.OfficeId = model.Users.FirstOrDefault()?.OfficeId ?? 0;
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
                    user.Active = model.Active;
                    _unitOfWork.Save();
                    return RedirectToAction("CreateUser", new { result = "update" });
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
        public JsonResult DeleteUser(int userId)
        {
            var user = _unitOfWork.UserRepository.GetById(userId);
            _unitOfWork.UserRepository.Delete(user);
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Xóa tài khoản thành công" });

        }
        //public ActionResult ClearOffice()
        //{
        //    var offices = _unitOfWork.OfficeRepository.Get();
        //    if(offices.Count() < 30)
        //    {
        //        foreach(var item in offices)
        //        {
        //            _unitOfWork.OfficeRepository.Delete(item);
        //        }
        //    }
        //    _unitOfWork.Save();
        //    return RedirectToAction("ListOffice");

        //}
        #endregion


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
        //[HttpPost]
        //public ActionResult InsertOfficeExcel()
        //{

        //    var file = Request.Files["MemberFile"];
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

        //        var tbl = result.Tables[0];
        //        var members = _unitOfWork.UserRepository.GetQuery(a => a.Active, o => o.OrderByDescending(a => a.CreateDate));
        //        for (var i = 1; i < tbl.Rows.Count; i++)
        //        {
        //            //var username = tbl.Rows[i][3].ToString().Trim();
        //            //var countUser = members.Count(a => a.Username == username);
        //            //if (countUser > 0) continue;

        //            var fullname = tbl.Rows[i][0].ToString().Trim();
        //            if (fullname == null) continue;

        //            var company = tbl.Rows[i][1].ToString().Trim();
        //            var businessLicense = tbl.Rows[i][2].ToString().Trim();
        //            var jobTitle = tbl.Rows[i][3].ToString().Trim();


        //            var address = tbl.Rows[i][4].ToString().Trim();
        //            var website = tbl.Rows[i][5].ToString().Trim();



        //            var phone = tbl.Rows[i][6];
        //            if (phone == null) continue;

        //            var phoneVal = phone.ToString().Trim();

        //            // Bước 2: Chuyển +84 thành 0
        //            if (phoneVal.StartsWith("+84"))
        //            {
        //                phoneVal = "0" + phoneVal.Substring(3);
        //            }

        //            // Bước 3: Xoá dấu cách và dấu chấm
        //            phoneVal = phoneVal.Replace(" ", "").Replace(".", "").Replace("-", "");
        //            phoneVal = Regex.Replace(phoneVal, @"[^0-9]", "");
        //            if (!phoneVal.StartsWith("0"))
        //            {
        //                phoneVal = "0" + phoneVal;
        //            }
        //            if (!Regex.IsMatch(phoneVal, @"^\d{10,11}$"))
        //            {
        //                continue; // Bỏ qua nếu không đúng định dạng
        //            }

        //            var countPhone = members.Count(a => a.PhoneNumber != null && a.PhoneNumber == phoneVal);
        //            if (countPhone > 0) continue;

        //            var email = tbl.Rows[i][7];
        //            //if (email == null) continue;
        //            var emailVal = email.ToString().Trim();
        //            var countEmail = members.Count(a => a.Email != null && a.Email == emailVal);
        //            if (countEmail > 0) continue;



        //            //var companyHotline = tbl.Rows[i][10].ToString().Trim();
        //            //var mst = tbl.Rows[i][11].ToString().Trim();
        //            var member = new User
        //            {
        //                Fullname = fullname,
        //                //Address = tbl.Rows[i][4].ToString().Trim(),
        //                PhoneNumber = phoneVal == "" ? null : phoneVal,
        //                Email = emailVal == "" ? "ceo.vefglobal@gmail.com" : emailVal,
        //                Company = company == "" ? null : company,
        //                BusinessLicense = businessLicense == "" ? null : businessLicense,
        //                JobTitle = jobTitle == "" ? null : jobTitle,
        //                Address = address == "" ? null : address,
        //                CompanyWebsite = website == "" ? null : website,
        //                //CompanyHotline = companyHotline == "" ? null : companyHotline,
        //                //CompanyNumber = mst == "" ? null : mst,
        //                Password = HtmlHelpers.ComputeHash("123456", "SHA256", null),
        //                Active = true,
        //                TypeUser = TypeUser.Normal,
        //            };
        //            _unitOfWork.UserRepository.Insert(member);
        //            _unitOfWork.Save();
        //            if (Forum() != null)
        //            {
        //                var memberForum = new MemberForum
        //                {
        //                    CreateDate = DateTime.Now,
        //                    ForumId = Forum().Id,
        //                    UserId = member.Id,
        //                    Office = "Thành viên",
        //                    TypeStatus = TypeStatus.Approved,

        //                    Hot = false,
        //                };
        //                _unitOfWork.MemberForumRepository.Insert(memberForum);
        //                _unitOfWork.Save();
        //            }
        //        }
        //    }
        //    return RedirectToAction("ListUser", "Vcms");
        //}
        #endregion

        #region Discount
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
                    OfficeId = model.OfficeId,
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
        #endregion
        //[HttpPost]
        //public ActionResult InsertUserExcel()
        //{

        //    var file = Request.Files["MemberFile"];
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

        //        var tbl = result.Tables[0];
        //        var members = _unitOfWork.UserRepository.GetQuery(a => a.Active, o => o.OrderByDescending(a => a.CreateDate));
        //        for (var i = 1; i < tbl.Rows.Count; i++)
        //        {
        //            //var username = tbl.Rows[i][3].ToString().Trim();
        //            //var countUser = members.Count(a => a.Username == username);
        //            //if (countUser > 0) continue;

        //            var fullname = tbl.Rows[i][0].ToString().Trim();
        //            if (fullname == null) continue;

        //            var company = tbl.Rows[i][1].ToString().Trim();
        //            var businessLicense = tbl.Rows[i][2].ToString().Trim();
        //            var jobTitle = tbl.Rows[i][3].ToString().Trim();


        //            var address = tbl.Rows[i][4].ToString().Trim();
        //            var website = tbl.Rows[i][5].ToString().Trim();



        //            var phone = tbl.Rows[i][6];
        //            if (phone == null) continue;

        //            var phoneVal = phone.ToString().Trim();

        //            // Bước 2: Chuyển +84 thành 0
        //            if (phoneVal.StartsWith("+84"))
        //            {
        //                phoneVal = "0" + phoneVal.Substring(3);
        //            }

        //            // Bước 3: Xoá dấu cách và dấu chấm
        //            phoneVal = phoneVal.Replace(" ", "").Replace(".", "").Replace("-", "");
        //            phoneVal = Regex.Replace(phoneVal, @"[^0-9]", "");
        //            if (!phoneVal.StartsWith("0"))
        //            {
        //                phoneVal = "0" + phoneVal;
        //            }
        //            if (!Regex.IsMatch(phoneVal, @"^\d{10,11}$"))
        //            {
        //                continue; // Bỏ qua nếu không đúng định dạng
        //            }

        //            var countPhone = members.Count(a => a.PhoneNumber != null && a.PhoneNumber == phoneVal);
        //            if (countPhone > 0) continue;

        //            var email = tbl.Rows[i][7];
        //            //if (email == null) continue;
        //            var emailVal = email.ToString().Trim();
        //            var countEmail = members.Count(a => a.Email != null && a.Email == emailVal);
        //            if (countEmail > 0) continue;



        //            //var companyHotline = tbl.Rows[i][10].ToString().Trim();
        //            //var mst = tbl.Rows[i][11].ToString().Trim();
        //            var member = new User
        //            {
        //                Fullname = fullname,
        //                //Address = tbl.Rows[i][4].ToString().Trim(),
        //                PhoneNumber = phoneVal == "" ? null : phoneVal,
        //                Email = emailVal == "" ? "ceo.vefglobal@gmail.com" : emailVal,
        //                Company = company == "" ? null : company,
        //                BusinessLicense = businessLicense == "" ? null : businessLicense,
        //                JobTitle = jobTitle == "" ? null : jobTitle,
        //                Address = address == "" ? null : address,
        //                CompanyWebsite = website == "" ? null : website,
        //                //CompanyHotline = companyHotline == "" ? null : companyHotline,
        //                //CompanyNumber = mst == "" ? null : mst,
        //                Password = HtmlHelpers.ComputeHash("123456", "SHA256", null),
        //                Active = true,
        //                TypeUser = TypeUser.Normal,
        //            };
        //            _unitOfWork.UserRepository.Insert(member);
        //            _unitOfWork.Save();
        //            if (Forum() != null)
        //            {
        //                var memberForum = new MemberForum
        //                {
        //                    CreateDate = DateTime.Now,
        //                    ForumId = Forum().Id,
        //                    UserId = member.Id,
        //                    Office = "Thành viên",
        //                    TypeStatus = TypeStatus.Approved,

        //                    Hot = false,
        //                };
        //                _unitOfWork.MemberForumRepository.Insert(memberForum);
        //                _unitOfWork.Save();
        //            }
        //        }
        //    }
        //    return RedirectToAction("ListUser", "Vcms");
        //}

        protected override void Dispose(bool disposing)
        {
            _unitOfWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
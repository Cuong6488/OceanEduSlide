using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Migrations;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using PagedList;
using System;
using System.Data.Entity;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    public class HomeController : Controller
    {
        private readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private string OfficeCode => RouteData.Values["OfficeCode"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        #region Account
        [OverrideActionFilters]
        [Route("dang-nhap")]
        public ActionResult Login()
        {
            return View();
        }
        [OverrideActionFilters]
        [Route("dang-nhap")]
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(UserLoginModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var usernameVal = model.Username.Trim();
                var user = _unitOfWork.UserRepository.GetQuery(a => a.Username == usernameVal).SingleOrDefault();

                if (user == null)
                {
                    ModelState.AddModelError("", @"Tên đăng nhập không chính xác.");
                    return View(model);
                }
                if (!user.Active)
                {
                    ModelState.AddModelError("", @"Bạn chưa xác thực tài khoản. Vui lòng kiểm tra email và làm theo hướng dẫn");
                    return View(model);
                }
                if (!HtmlHelpers.VerifyHash(model.Password, "SHA256", user.Password))
                {
                    ModelState.AddModelError("", @"Mật khẩu không chính xác. Vui lòng thử lại.");
                    return View(model);
                }

                var office = _unitOfWork.OfficeRepository.GetById(user.OfficeId);
                var userData = user.Username + "|" + user.OfficeId + "|" + office.ShortCode;
                var ticket = new FormsAuthenticationTicket(2, user.Username, DateTime.Now, DateTime.Now.AddDays(1), true, userData);
                var encTicket = FormsAuthentication.Encrypt(ticket);
                Response.Cookies.Add(new HttpCookie(".ASPXAUTHMEMBER", encTicket));
                //if (user.TypeUser == TypeUser.BM || user.TypeUser == TypeUser.HO)
                //{
                //    return RedirectToAction("Revenue", "Tuyensinh");
                //}
                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index");

            }
            return View();
        }
        //[Route("thoat-he-thong")]
        public RedirectToRouteResult LogOut()
        {
            var cookie = Request.Cookies[".ASPXAUTHMEMBER"];
            if (cookie != null)
            {
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }
            return RedirectToAction("Login");
        }
        #endregion
        public ActionResult Index(int? officeId)
        {
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(DateTime.Now.Year, DateTime.Now.Month);
            ViewBag.CurrentWeek = currentWeek;
            if (User.TypeUser == TypeUser.EC)
            {
                var debt = _unitOfWork.DebtRepository.GetQuery(q => q.UserId == User.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
                && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0;
                var model = new UserHomeViewModel
                {
                    User = User,
                    Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek),
                    TMonth = (_unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).FirstOrDefault()?.TargetBM - debt) ?? 0,
                    RevenueMonthNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).Sum(a => a.TargetBM) ?? 0,
                    TWeek = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).FirstOrDefault()?.TargetBM ?? 0,
                    RevenueWeekNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).Sum(a => a.TargetBM) ?? 0,
                    Debts = _unitOfWork.DebtRepository.GetQuery(q => q.UserId == User.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
                    && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3))
                };
                model.TargetMonthPercent = model.TMonth > 0 ? model.RevenueMonthNow / model.TMonth * 100 : 0;
                model.RemainPercent = 100 - model.TargetMonthPercent;
                model.TargetWeekPercent = model.TWeek > 0 ? model.RevenueWeekNow / model.TWeek * 100 : 0;
                switch (DateTime.Now.DayOfWeek)
                {
                    case DayOfWeek.Monday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Monday);
                        break;
                    case DayOfWeek.Tuesday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Tuesday);
                        break;
                    case DayOfWeek.Thursday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Thursday);
                        break;
                    case DayOfWeek.Friday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Friday);
                        break;
                    case DayOfWeek.Wednesday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Wednessday);
                        break;
                    case DayOfWeek.Sunday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Sunday);
                        break;
                    case DayOfWeek.Saturday:
                        model.Revenues = model.Revenues.Where(a => a.DayofWeek == DayofWeek.Saturday);
                        break;
                    default:
                        break;
                }
                return View(model);
            }
            if (User.TypeUser == TypeUser.BM || User.TypeUser == TypeUser.ASM)
            {
                var model = new BMHomeViewModel
                {
                    OfficeId = officeId,
                    User = User,
                };
                if (User.TypeUser == TypeUser.BM)
                    model.OfficeId = User.OfficeId;
                if (model.OfficeId != null)
                {
                    var users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeId == model.OfficeId);
                    int today = 0;
                    switch (DateTime.Now.DayOfWeek)
                    {
                        case DayOfWeek.Monday:
                            today = 2;
                            break;
                        case DayOfWeek.Tuesday:
                            today = 3;
                            break;
                        case DayOfWeek.Thursday:
                            today = 4;
                            break;
                        case DayOfWeek.Friday:
                            today = 5;
                            break;
                        case DayOfWeek.Wednesday:
                            today = 6;
                            break;
                        case DayOfWeek.Sunday:
                            today = 7;
                            break;
                        case DayOfWeek.Saturday:
                            today = 8;
                            break;
                        default:
                            break;
                    }

                    var userItems = users.ToList().Select(x => new BMHomeViewModel.UserItem
                    {
                        User = x,
                        Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today),
                        TMonth = (_unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.UserId == x.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).FirstOrDefault()?.TargetBM -
                        (_unitOfWork.DebtRepository.GetQuery(q => q.UserId == x.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
                        && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0)) ?? 0,
                        RevenueMonthNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == x.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).Sum(a => a.TargetBM) ?? 0,
                        TWeek = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).FirstOrDefault()?.TargetBM ?? 0,
                        RevenueWeekNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == x.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).Sum(a => a.TargetBM) ?? 0,
                        Debts = _unitOfWork.DebtRepository.GetQuery(q => q.UserId == x.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
                        && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)),
                    });

                    model.UserItems = userItems;
                }
                if (User.TypeUser == TypeUser.ASM)
                    model.Offices = _unitOfWork.OfficeRepository.Get(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                return View("IndexBM", model);
            }
            return View();
        }
        //public JsonResult GetOffice(int? zoneId)
        //{
        //    var zone = _unitOfWork.ZoneRepository.GetById(zoneId);

        //    //var offices = _unitOfWork.OfficeRepository
        //    //    .GetQuery(a => a.Active && a.ZoneId != null && a.ZoneId == zoneId, q => q.OrderBy(a => a.Sort)).Select(a => new { a.Id, a.Name });
        //    var offices = _unitOfWork.OfficeRepository
        //       .GetQuery(a => a.Active && zone.OfficeIds.Contains("," + a.Id + ","), q => q.OrderBy(a => a.Name)).Select(a => new { a.Id, a.Name });
        //    return Json(offices, JsonRequestBehavior.AllowGet);
        //}
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of any resources here if needed
            }
            base.Dispose(disposing);
        }
    }
}
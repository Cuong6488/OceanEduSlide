using Helpers;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;
using OceanEduSlide.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace OceanEduSlide.Controllers
{
    [MemberFilter]
    public class HomeController : Controller
    {
        public readonly UnitOfWork _unitOfWork = new UnitOfWork();
        private string Username => RouteData.Values["Username"].ToString();
        private new User User => _unitOfWork.UserRepository.GetQuery(a => a.Username == Username).SingleOrDefault();
        public ActionResult Index()
        {
            return View();
        }
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

                var userData = user.Username + "|" + user.OfficeId;
                var ticket = new FormsAuthenticationTicket(2, user.Username, DateTime.Now, DateTime.Now.AddDays(1), true, userData);
                var encTicket = FormsAuthentication.Encrypt(ticket);
                Response.Cookies.Add(new HttpCookie(".ASPXAUTHMEMBER", encTicket));

                if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Home");

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
            return RedirectToAction("Index", "Home");
        }
        [Route("gioi-thieu")]
        public ActionResult About()
        {
            return View();
        }
        [Route("hoc-phi")]
        public ActionResult Price()
        {
            var model = new PriceViewModel
            {
                SelectDiscounts = new SelectList(_unitOfWork.DiscountRepository.Get(a => a.OfficeId == User.OfficeId), "Id", "Username"),
                Office = _unitOfWork.OfficeRepository.GetQuery().FirstOrDefault(a => a.Id == User.OfficeId)
            };
            return View(model);
        }
        [HttpPost]
        public JsonResult CalcMoney(int id, string totalMoney)
        {
            int intTotalMoney = Convert.ToInt32(totalMoney.Replace(".", "").Replace(",", "").Replace("đ", ""));
            var discount = _unitOfWork.DiscountRepository.GetById(id);
            int moneyDiscount = 0;
            if (discount != null)
            {
                moneyDiscount = discount.MoneyDiscount ?? 0;
                if (discount.PercentDiscount != null)
                {
                    decimal? decimalMoney = intTotalMoney * discount.PercentDiscount / 100;
                    moneyDiscount += (int)Math.Round((double)decimalMoney);
                    return Json(new { status = true, moneyDiscount, gift = discount.Gift??"Chưa có",finalMoney = intTotalMoney - moneyDiscount });

                }
            }
            return Json(new { status = false });
        }
        public ActionResult Pathway()
        {
            return View();
        }
        public ActionResult Face()
        {
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}
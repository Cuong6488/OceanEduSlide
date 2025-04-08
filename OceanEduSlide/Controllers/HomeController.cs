using OceanEduSlide.Filters;
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
        [HttpPost/*, ValidateAntiForgeryToken*/]
        public ActionResult Login(string id, string password/*, string returnUrl*/)
        {

            if (id == "salekitoceanedu" && password == "ocean@123")
            {
                var userData = id;
                var ticket = new FormsAuthenticationTicket(2, "userName", DateTime.Now, DateTime.Now.AddDays(1), true, userData);
                var encTicket = FormsAuthentication.Encrypt(ticket);
                Response.Cookies.Add(new HttpCookie(".ASPXAUTHMEMBER", encTicket));

                //if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                //    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                //{
                //    return Redirect(returnUrl);
                //}
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
            return RedirectToAction("Index", "Home");
        }
        [Route("gioi-thieu")]
        public ActionResult About()
        {
            return View();
        }
        public ActionResult Pathway()
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
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace OceanEduSlide.Filters
{
    public class MemberFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cookie = filterContext.HttpContext.Request.Cookies[".ASPXAUTHMEMBER"];
            if (cookie == null)
            {
                var returnUrl = filterContext.HttpContext.Request.Url?.AbsolutePath;
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                 {{ "action", "Login" },
                 { "controller", "Home" },
                 { "returnUrl", returnUrl }
                 });
                //filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                //    {{"action", "Login"}, {"controller", "Home"}});
            }
            else
            {
                var ticketInfo = FormsAuthentication.Decrypt(cookie.Value);
                var data = ticketInfo.UserData;
                //filterContext.RouteData.Values["Fullname"] = data.Split('|')[0];
                filterContext.RouteData.Values["Username"] = ticketInfo?.Name;

                filterContext.RouteData.Values["OfficeId"] = data.Split('|')[1];
                //filterContext.RouteData.Values["MemberId"] = data.Split('|')[2];
            }
            base.OnActionExecuting(filterContext);
        }
    }

    public class IsMemberLoginFiler : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cookie = filterContext.HttpContext.Request.Cookies[".ASPXAUTHMEMBER"];
            if (cookie == null)
            {
                filterContext.RouteData.Values["Id"] = "";
                //filterContext.RouteData.Values["Username"] = "";
                //filterContext.RouteData.Values["Fullname"] = "";
                //filterContext.RouteData.Values["MemberAvatar"] = "";
            }
            else
            {
                var ticketInfo = FormsAuthentication.Decrypt(cookie.Value);
                if (ticketInfo != null)
                {
                    var data = ticketInfo.UserData;
                    filterContext.RouteData.Values["Id"] = data;
                    //filterContext.RouteData.Values["Fullname"] = data.Split('|')[0];
                    //filterContext.RouteData.Values["Username"] = ticketInfo.Name;
                    //filterContext.RouteData.Values["MemberAvatar"] = data.Split('|')[2];
                }
                else
                {
                    filterContext.RouteData.Values["Id"] = "";
                    //filterContext.RouteData.Values["Username"] = "";
                    //filterContext.RouteData.Values["Fullname"] = "";
                    //filterContext.RouteData.Values["MemberAvatar"] = "";
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
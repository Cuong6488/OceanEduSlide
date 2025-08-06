using OceanEduSlide.Models;
using System;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace OceanEduSlide.Filters
{
    //public class MemberFilter : ActionFilterAttribute
    //{
    //    public override void OnActionExecuting(ActionExecutingContext filterContext)
    //    {
    //        var cookie = filterContext.HttpContext.Request.Cookies[".ASPXAUTHMEMBER"];
    //        if (cookie == null)
    //        {
    //            var returnUrl = filterContext.HttpContext.Request.Url?.AbsolutePath;
    //            filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
    //             {{ "action", "Login" },
    //             { "controller", "Home" },
    //             { "returnUrl", returnUrl }
    //             });
    //            //filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
    //            //    {{"action", "Login"}, {"controller", "Home"}});
    //        }
    //        else
    //        {
    //            var ticketInfo = FormsAuthentication.Decrypt(cookie.Value);
    //            var data = ticketInfo.UserData;
    //            //filterContext.RouteData.Values["Fullname"] = data.Split('|')[0];
    //            filterContext.RouteData.Values["Username"] = ticketInfo?.Name;

    //            filterContext.RouteData.Values["OfficeId"] = data.Split('|')[1];
    //            filterContext.RouteData.Values["OfficeCode"] = data.Split('|')[2];
    //            //filterContext.RouteData.Values["MemberId"] = data.Split('|')[2];
    //        }
    //        base.OnActionExecuting(filterContext);
    //    }
    //}
    public class MemberFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cookie = filterContext.HttpContext.Request.Cookies[".ASPXAUTHMEMBER"];
            if (cookie == null)
            {
                var returnUrl = filterContext.HttpContext.Request.Url?.AbsolutePath;
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "action", "Login" },
                { "controller", "Home" },
                { "returnUrl", returnUrl }
            });
                return;
            }

            var ticketInfo = FormsAuthentication.Decrypt(cookie.Value);
            var data = ticketInfo.UserData;
            var dataParts = data.Split('|');

            if (dataParts.Length < 5)
            {
                // Dữ liệu không đủ
                filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "action", "Login" },
                { "controller", "Home" }
            });
                return;
            }

            filterContext.RouteData.Values["Username"] = ticketInfo?.Name;
            filterContext.RouteData.Values["OfficeId"] = dataParts[1];
            filterContext.RouteData.Values["OfficeCode"] = dataParts[2];
            filterContext.RouteData.Values["OldAccount"] = dataParts[3];

            // Lấy TypeUser từ phần tử thứ 5
            var userTypeRaw = dataParts[4];
            var currentController = filterContext.RouteData.Values["controller"]?.ToString().ToLower();
            var currentAction = filterContext.RouteData.Values["action"]?.ToString().ToLower();

            if (userTypeRaw == "PKT" && bool.Parse(dataParts[3]) != false)
            {
                if (!filterContext.IsChildAction)
                {
                    var controller = filterContext.RouteData.Values["controller"]?.ToString().ToLower();
                    var action = filterContext.RouteData.Values["action"]?.ToString().ToLower();

                    if (!(controller == "proposal" && action == "listproposal"))
                    {
                        filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
            {
                { "controller", "Proposal" },
                { "action", "ListProposal" }
            });
                        return;
                    }
                }
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
    public class ForcePasswordChangeFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var cookie = filterContext.HttpContext.Request.Cookies[".ASPXAUTHMEMBER"];
            if (cookie != null)
            {
                var ticketInfo = FormsAuthentication.Decrypt(cookie.Value);
                var data = ticketInfo.UserData.Split('|');

                // Ví dụ: DaDoiMatKhau nằm ở vị trí cuối cùng
                var daDoiMatKhau = data.Length > 3 && bool.TryParse(data[3], out var isChanged) && isChanged;

                var routeData = filterContext.RouteData;
                var controller = routeData.Values["controller"].ToString().ToLower();
                var action = routeData.Values["action"].ToString().ToLower();

                // Chỉ redirect nếu chưa đổi mật khẩu và không phải đang ở trang đổi mật khẩu
                if (!daDoiMatKhau && !(controller == "home" && (action == "changepasswordrequired")))
                {
                    filterContext.Result = new RedirectToRouteResult(new RouteValueDictionary
                {
                    { "controller", "Home" },
                    { "action", "ChangePasswordRequired" }
                });
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
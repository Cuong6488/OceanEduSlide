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
using System.Globalization;
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
                // QL : Quản lý - không thuộc chi nhánh nào
                var userData = user.Username + "|" + user.OfficeId + "|" + office?.ShortCode;
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
                return RedirectToAction("IndexShare");

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
        [HttpPost]
        public JsonResult ChangePassword(string oldpassword, string newpassword, string confirmpassword)
        {
            if (!HtmlHelpers.VerifyHash(oldpassword, "SHA256", User.Password))
            {
                return Json(new { status = false, msg = "Mật khẩu cũ không chính xác. Hãy kiểm tra lại." });
            }
            else if (confirmpassword != newpassword)
            {
                return Json(new { status = false, msg = "Xác nhận mật khẩu không chính xác. Hãy kiểm tra lại." });
            }
            else if (newpassword.Length > 60)
            {
                return Json(new { status = false, msg = "Mật khẩu không được quá 60 ký tự." });

            }

            User.Password = HtmlHelpers.ComputeHash(newpassword, "SHA256", null);
            _unitOfWork.Save();
            return Json(new { status = true, msg = "Đổi mật khẩu thành công." });

        }


        #endregion

        #region SaleKit
        [Route("trang-chu")]
        public ActionResult IndexSaleKit()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        public JsonResult GetDiscount(string cth, double pathway)
        {
            var discounts = _unitOfWork.DiscountRepository
                .GetQuery(a => a.Active && ("," + a.Offices + ",").Contains("," + OfficeCode + ",") &&
                (!a.StartDate.HasValue || DbFunctions.TruncateTime(a.StartDate) <= DbFunctions.TruncateTime(DateTime.Now)) &&
                (!a.EndDate.HasValue || DbFunctions.TruncateTime(a.EndDate) >= DbFunctions.TruncateTime(DateTime.Now)) &&
                a.Cth == cth /*&& (a.Pathway <= pathway && pathway <= a.PathwayTo)*/, q => q.OrderBy(a => a.Id)).Select(a => new { a.Id, a.Username });
            return Json(discounts, JsonRequestBehavior.AllowGet);
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
                    double? decimalMoney = intTotalMoney * discount.PercentDiscount / 100;
                    moneyDiscount += (int)Math.Round((double)decimalMoney);
                    return Json(new { status = true, moneyDiscount, gift = discount.Gift ?? "Chưa có", finalMoney = intTotalMoney - moneyDiscount });

                }
            }
            return Json(new { status = false });
        }

        [Route("chuong-trinh-hoc")]
        public ActionResult Pathway()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        public ActionResult Face()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        [Route("cuoc-thi")]
        public ActionResult Race()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        [Route("tin-tuc")]
        public ActionResult Articles()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        //public ActionResult CourseOutline()
        //{
        //    if (!User.SaleKit && User.TypeUser != null)
        //        return HttpNotFound();
        //    return View();
        //}
        [Route("gioi-thieu")]
        public ActionResult About()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            return View();
        }
        [Route("hoc-phi")]
        public ActionResult Price()
        {
            if (!User.SaleKit && User.TypeUser != null)
                return HttpNotFound();
            //var model = new PriceViewModel
            //{
            //    SelectDiscounts = new SelectList(_unitOfWork.DiscountRepository.Get(a => a.Active), "Id", "Username"),
            //};
            var office = _unitOfWork.OfficeRepository.GetQuery().FirstOrDefault(a => a.Id == User.OfficeId);
            return View(office);
        }

        #endregion

        #region Tuyen_Sinh
        public ActionResult Index(int? officeId, string Date, string Result = "")
        {
            if (User.TypeUser == null)
                return HttpNotFound();
            if (User.TypeUser == TypeUser.EC || User.TypeUser == TypeUser.ALT || User.TypeUser == TypeUser.SAB || User.TypeUser == TypeUser.CM)
            {
                (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now);
                ViewBag.CurrentWeek = currentWeek;
                var debt = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.UserId == User.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
                && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0;
                var model = new UserHomeViewModel
                {
                    User = User,
                    Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek),
                    TMonth = (_unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).FirstOrDefault()?.TargetBM - debt) ?? 0,
                    RevenueMonthNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year).Sum(a => a.TargetBM) ?? 0,
                    TWeek = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).FirstOrDefault()?.TargetBM ?? 0,
                    RevenueWeekNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == User.Id && a.Month == DateTime.Now.Month && a.Year == DateTime.Now.Year && (int)a.WeekNumber == currentWeek).Sum(a => a.TargetBM) ?? 0,
                    Debts = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.UserId == User.Id && q.Year == (DateTime.Now.Month - 1 == 0 ? DateTime.Now.Year - 1 : DateTime.Now.Year) && q.Month == (DateTime.Now.Month - 1 == 0 ? 12 : DateTime.Now.Month - 1)
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
                if (model.Revenues.Any())
                {
                    model.TargetBM = model.Revenues.Sum(a => a.TargetBM ?? 0);
                    model.DataQuantity = model.Revenues.Sum(a => a.DataQuantity ?? 0);
                    model.Confirm1 = model.Revenues.Sum(a => a.Confirm1 ?? 0);
                    model.Confirm2 = model.Revenues.Sum(a => a.Confirm2 ?? 0);
                    model.CI = model.Revenues.Sum(a => a.CI ?? 0);
                    model.DT = model.Revenues.Sum(a => a.DT ?? 0);
                }
                ViewBag.Result = Result;
                return View(model);
            }
            if (User.TypeUser == TypeUser.BM || User.TypeUser == TypeUser.ASM)
            {
                var model = new BMHomeViewModel
                {
                    OfficeId = officeId,
                    User = User,
                    Date = Date ?? DateTime.Now.ToString("dd/MM/yyyy")
                };
                if (User.TypeUser == TypeUser.BM)
                    model.OfficeId = User.OfficeId;
                if (model.OfficeId != null)
                {
                    var users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeId == model.OfficeId && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.SAB || a.TypeUser == TypeUser.CM || a.TypeUser == TypeUser.BM));
                    int today = 0;
                    if (string.IsNullOrEmpty(Date))
                        Date = DateTime.Now.ToString("dd/MM/yyyy");
                    if (DateTime.TryParse(Date, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
                    {
                        var date = new DateTime(cd.Year, cd.Month, cd.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                        switch (date.DayOfWeek)
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

                        (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(date.Year, date.Month, date);
                        ViewBag.CurrentWeek = currentWeek;
                        var userItems = users.ToList().Select(x => new BMHomeViewModel.UserItem
                        {
                            User = x,
                            Revenues = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today),
                            TMonth = (_unitOfWork.RevenueUser_Month_BMRepository.GetQuery(a => a.Active && a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year).FirstOrDefault()?.TargetBM -
                            (_unitOfWork.DebtRepository.GetQuery(q => q.UserId == x.Id && q.Year == (date.Month - 1 == 0 ? date.Year - 1 : date.Year) && q.Month == (date.Month - 1 == 0 ? 12 : date.Month - 1)
                            && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0)) ?? 0,
                            RevenueMonthNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year).Sum(a => a.TargetBM) ?? 0,
                            TWeek = _unitOfWork.RevenueUser_WeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek).FirstOrDefault()?.TargetBM ?? 0,
                            RevenueWeekNow = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek).Sum(a => a.TargetBM) ?? 0,
                            Debts = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.UserId == x.Id && q.Year == (date.Month - 1 == 0 ? date.Year - 1 : date.Year) && q.Month == (date.Month - 1 == 0 ? 12 : date.Month - 1)
                            && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)),
                            TargetBM = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.TargetBM ?? 0),
                            DataQuantity = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.DataQuantity ?? 0),
                            Confirm1 = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.Confirm1 ?? 0),
                            Confirm2 = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.Confirm2 ?? 0),
                            CI = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.CI ?? 0),
                            DT = _unitOfWork.RevenueUser_DayOfWeekRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today)?.ToList().Sum(i => i.DT ?? 0),
                            Report = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == x.Id && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek && (int)a.DayofWeek == today).FirstOrDefault(),
                        });
                        model.UserItems = userItems;
                        model.TMonth = _unitOfWork.RevenueOffice_BMRepository.GetQuery(a => a.OfficeId == model.OfficeId).FirstOrDefault()?.TargetBM_TS ?? 0;
                        model.TWeek = userItems.Sum(a => a.TWeek);
                        model.TWeekReal = _unitOfWork.RevenueUser_DayOfWeek_RealRepository
                            .GetQuery(a => a.User.OfficeId == model.OfficeId && a.Month == date.Month && a.Year == date.Year && (int)a.WeekNumber == currentWeek)
                            .Sum(a => a.TargetBM) ?? 0; 
                        model.Confirm1 = userItems.Sum(a => a.Confirm1 ?? 0);
                        model.Confirm2 = userItems.Sum(a => a.Confirm2 ?? 0);
                        model.DT = userItems.Sum(a => a.DT ?? 0);
                        model.CI = userItems.Sum(a => a.CI ?? 0);
                        model.DataQuantity = userItems.Sum(a => a.DataQuantity ?? 0);
                        model.TargetBM = userItems.Sum(a => a.TargetBM ?? 0);
                        model.CIReal = userItems.Sum(a => a.Report?.CI ?? 0);
                        model.DTReal = userItems.Sum(a => a.Report?.DT ?? 0);
                        model.DataQuantityReal = userItems.Sum(a => a.Report?.DataQuantity ?? 0);
                        model.TargetBMReal = userItems.Sum(a => a.Report?.TargetBM ?? 0);
                        model.Confirm1Real = userItems.Sum(a => a.Report?.Confirm1 ?? 0);
                        model.Confirm2Real = userItems.Sum(a => a.Report?.Confirm2 ?? 0);
                        model.RankOffice = _unitOfWork.RankOfficeRepository.GetQuery(a => a.OfficeId == model.OfficeId).FirstOrDefault();
                        model.Debt = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.User.OfficeId == model.OfficeId && q.Year == (date.Month - 1 == 0 ? date.Year - 1 : date.Year) && q.Month == (date.Month - 1 == 0 ? 12 : date.Month - 1)
                            && (q.TypeDebt == TypeDebt.Type1 || q.TypeDebt == TypeDebt.Type2 || q.TypeDebt == TypeDebt.Type3)).Sum(q => (decimal?)(q.TotalMoney - q.DownMoney)) ?? 0;
                        model.DebtBad = _unitOfWork.DebtRepository.GetQuery(q => q.Active && q.User.OfficeId == model.OfficeId && q.Year == (date.Month - 1 == 0 ? date.Year - 1 : date.Year) && q.Month == (date.Month - 1 == 0 ? 12 : date.Month - 1)
                            && (q.TypeDebt == TypeDebt.Type4 || q.TypeDebt == TypeDebt.Type5)).Sum(q => (decimal?)q.TotalMoney) ?? 0;
                    }
                }
                if (User.TypeUser == TypeUser.ASM)
                    model.Offices = _unitOfWork.OfficeRepository.Get(a => User.Zone.OfficeIds.Contains("," + a.Id.ToString() + ","));
                return View("IndexBM", model);
            }
            return View("IndexHO");
        }
        public PartialViewResult LoadDebt(int debtId)
        {
            var model = _unitOfWork.DebtRepository.GetById(debtId);
            return PartialView(model);
        }
        public PartialViewResult LoadEvent(int evId)
        {
            var model = _unitOfWork.EventRepository.GetById(evId);
            return PartialView(model);
        }
        public ActionResult Report()
        {
            if (User.TypeUser != TypeUser.EC && User.TypeUser != TypeUser.SAB && User.TypeUser != TypeUser.ALT && User.TypeUser != TypeUser.CM)
                return HttpNotFound();
            var report = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => a.UserId == User.Id && DbFunctions.TruncateTime(DateTime.Now) == DbFunctions.TruncateTime(a.CreateDate)).FirstOrDefault();
            if (report != null)
            {
                var reportViewModel = new ReportViewModel
                {
                    Report = report,
                    TargetBM = report.TargetBM?.ToString("N0")
                };
                return View(reportViewModel);
            }
            (int workingWeeks, int currentWeek) = DateHelper.CalculateWeeks(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now);

            var model = new ReportViewModel
            {
                Report = new RevenueUser_DayOfWeek_Real { Active = true, UserId = User.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year },
            };
            switch (currentWeek)
            {
                case 1:
                    model.Report.WeekNumber = WeekNumber.Week1;
                    break;
                case 2:
                    model.Report.WeekNumber = WeekNumber.Week2;
                    break;
                case 3:
                    model.Report.WeekNumber = WeekNumber.Week3;
                    break;
                case 4:
                    model.Report.WeekNumber = WeekNumber.Week4;
                    break;
                case 5:
                    model.Report.WeekNumber = WeekNumber.Week5;
                    break;
                case 6:
                    model.Report.WeekNumber = WeekNumber.Week6;
                    break;
                default:
                    break;
            }
            switch (DateTime.Now.DayOfWeek)
            {
                case DayOfWeek.Monday:
                    model.Report.DayofWeek = DayofWeek.Monday;
                    break;
                case DayOfWeek.Tuesday:
                    model.Report.DayofWeek = DayofWeek.Tuesday;
                    break;
                case DayOfWeek.Thursday:
                    model.Report.DayofWeek = DayofWeek.Thursday;
                    break;
                case DayOfWeek.Friday:
                    model.Report.DayofWeek = DayofWeek.Friday;
                    break;
                case DayOfWeek.Wednesday:
                    model.Report.DayofWeek = DayofWeek.Wednessday;
                    break;
                case DayOfWeek.Sunday:
                    model.Report.DayofWeek = DayofWeek.Sunday;
                    break;
                case DayOfWeek.Saturday:
                    model.Report.DayofWeek = DayofWeek.Saturday;
                    break;
                default:
                    break;
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult Report(ReportViewModel model)
        {
            var report = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetById(model.Report.Id);
            if (report != null)
            {
                if (!string.IsNullOrEmpty(model.TargetBM))
                    report.TargetBM = Convert.ToDecimal(model.TargetBM.Replace(",", ""));
                else
                    report.TargetBM = null;
                report.DataQuantity = model.Report.DataQuantity;
                report.Confirm1 = model.Report.Confirm1;
                report.Confirm2 = model.Report.Confirm2;
                report.Confirm3 = model.Report.Confirm3;
                report.CI = model.Report.CI;
                report.DT = model.Report.DT;
                _unitOfWork.Save();
                return RedirectToAction("Index", new { Result = "update" });
            }
            if (!string.IsNullOrEmpty(model.TargetBM))
                model.Report.TargetBM = Convert.ToDecimal(model.TargetBM.Replace(",", ""));
            _unitOfWork.RevenueUser_DayOfWeek_RealRepository.Insert(model.Report);
            _unitOfWork.Save();
            return RedirectToAction("Index", new { Result = "add" });
        }
        public ActionResult ListReport(string Date, int OfficeId)
        {
            if (User.TypeUser != TypeUser.BM && User.TypeUser != TypeUser.ASM)
                return RedirectToAction("Index");
            if (string.IsNullOrEmpty(Date))
                Date = DateTime.Now.ToString("dd/MM/yyyy");

            if (DateTime.TryParse(Date, new CultureInfo("vi-VN"), DateTimeStyles.None, out var cd))
            {
                var date = new DateTime(cd.Year, cd.Month, cd.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                var model = new ListReportViewModel
                {
                    User = User,
                    OfficeId = OfficeId,
                    Date = Date,
                };
                var users = _unitOfWork.UserRepository.GetQuery(a => a.OfficeId == OfficeId && (a.TypeUser == TypeUser.EC || a.TypeUser == TypeUser.ALT || a.TypeUser == TypeUser.SAB));
                var officeName = _unitOfWork.OfficeRepository.GetById(OfficeId)?.Name;
                ViewBag.OfficeName = officeName == null ? "" : "- Chi nhánh " + officeName;
                var reportItems = users.ToList().Select(x => new ListReportViewModel.ReportItem
                {
                    User = x,
                    Report = _unitOfWork.RevenueUser_DayOfWeek_RealRepository.GetQuery(a => DbFunctions.TruncateTime(date) == DbFunctions.TruncateTime(a.CreateDate) && a.UserId == x.Id).FirstOrDefault(),
                });
                model.ReportItems = reportItems;
                return View(model);
            }
            return RedirectToAction("Index");
        }
        #endregion

        public ActionResult IndexShare(int? officeId)
        {
            if (User.TypeUser == null)
                return RedirectToAction("IndexSaleKit");
            else if (!User.SaleKit)
                return RedirectToAction("Index");
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
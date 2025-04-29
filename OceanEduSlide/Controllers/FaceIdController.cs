using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Newtonsoft.Json;
using OceanEduSlide.DAL;
using OceanEduSlide.Filters;
using OceanEduSlide.Models;

namespace OceanEduSlide.Controllers
{
    //[MemberFilter]
    [Authorize]
    public class FaceIdController : Controller
    {
        //private string UserId => RouteData.Values["MemberId"].ToString();
        //private string MemberEmail => RouteData.Values["MemberEmail"].ToString();
        //private string MemberName => RouteData.Values["Username"].ToString();

        private static readonly Fido2 _fido2 = new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost:44375", // ✍️ domain app thật
            ServerName = "Sale Kit App",
            Origin = "https://localhost:44375/" // HTTPS chuẩn
        });

        // =========================================
        // 🚀 Bắt đầu đăng ký FaceID
        //[OverrideActionFilters]
        public JsonResult BeginRegistration(string userId,string username)
        {
            var user = new Fido2User
            {
                DisplayName = username,
                //Name = MemberEmail,
                Name = username,
                Id = Encoding.UTF8.GetBytes(userId) // Lấy từ User.Identity
            };

            var authenticatorSelection = new AuthenticatorSelection
            {
                UserVerification = UserVerificationRequirement.Required,
                AuthenticatorAttachment = AuthenticatorAttachment.CrossPlatform,
            };

            var options = _fido2.RequestNewCredential(user, new List<PublicKeyCredentialDescriptor>(), authenticatorSelection, AttestationConveyancePreference.None);

            // 🔥 Lưu Challenge vào Session
            //Session["fido2.challenge"] = options.Challenge;
            Session["fido2.options"] = options;

            var json = new
            {
                challenge = options.Challenge,
                rp = new { name = options.Rp.Name },
                user = new { id = options.User.Id, name = options.User.Name, displayName = options.User.DisplayName },
                pubKeyCredParams = options.PubKeyCredParams.Select(a => new { type = "public-key", alg = a.Alg }),
                timeout = options.Timeout,
                attestation = options.Attestation.ToString().ToLower(),
                authenticatorSelection = new { authenticatorAttachment = options.AuthenticatorSelection.AuthenticatorAttachment.ToString().ToLower(), userVerification = options.AuthenticatorSelection.UserVerification.ToString().ToLower() },
                excludeCredentials = options.ExcludeCredentials,
                extensions = options.Extensions
            };

            return Json(json, JsonRequestBehavior.AllowGet);
        }

        // =========================================
        // 🚀 Hoàn thành đăng ký FaceID
        [HttpPost]
        public async Task<ActionResult> CompleteRegistration(string userId)
        {
            var jsonOptions = new JsonSerializerSettings { };
            var attestationResponse = JsonConvert.DeserializeObject<AuthenticatorAttestationRawResponse>(
                new StreamReader(Request.InputStream).ReadToEnd(), jsonOptions);
            var jsonData = new StreamReader(Request.InputStream).ReadToEnd();
            Console.WriteLine(jsonData);
            if (attestationResponse == null)
            {
                return new HttpStatusCodeResult(400, "Invalid attestation response");
            }

            //var storedChallenge = (byte[])Session["fido2.challenge"];
            //if (storedChallenge == null) return new HttpStatusCodeResult(400, "Invalid session");
            //var options = new CredentialCreateOptions
            //{
            //    Challenge = storedChallenge,
            //};

            var options = (CredentialCreateOptions)Session["fido2.options"];
            var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, (args) => Task.FromResult(true));

            // 🔥 Lưu Credential vào Database
            SaveCredential(success.Result,userId);

            return Json(new { status = "ok" });
        }

        //[OverrideActionFilters]
        [AllowAnonymous]
        // =========================================
        // 🚀 Bắt đầu đăng nhập bằng FaceID
        public JsonResult BeginLogin(string email)
        {
            //email = email ?? MemberEmail;
            //email = email ?? MemberName;
            var userHandle = Encoding.UTF8.GetBytes(email);

            var existingKeys = LoadUserKeys(userHandle); // Load từ database
            var options = _fido2.GetAssertionOptions(existingKeys, UserVerificationRequirement.Required);

            // 🔥 Lưu Challenge
            //Session["fido2.assertionChallenge"] = options.Challenge;
            Session["fido2.assertionChallenge"] = options;

            var json = new
            {
                challenge = options.Challenge,
                rpId = options.RpId,
                timeout = options.Timeout,
                userVerification = options.UserVerification.ToString().ToLower(),
                allowCredentials = options.AllowCredentials.Select(a => new { id = a.Id, type = a.Type, transports = a.Transports }),
                extensions = options.Extensions
            };
            return Json(json, JsonRequestBehavior.AllowGet);
        }

        //[OverrideActionFilters]
        [AllowAnonymous]
        // =========================================
        // 🚀 Xác thực đăng nhập
        [HttpPost]
        public async Task<ActionResult> CompleteLogin()
        {
            var jsonOptions = new JsonSerializerSettings { };
            var assertionResponse = JsonConvert.DeserializeObject<AuthenticatorAssertionRawResponse>(
                new StreamReader(Request.InputStream).ReadToEnd(), jsonOptions);

            //var storedChallenge = (byte[])Session["fido2.assertionChallenge"];
            //if (storedChallenge == null) return new HttpStatusCodeResult(400, "Invalid session");
            //var options = new AssertionOptions
            //{
            //    Challenge = storedChallenge
            //};

            var options = (AssertionOptions)Session["fido2.assertionChallenge"];
            var cred = LoadCredential(Convert.ToBase64String(assertionResponse.Id)); // Lấy từ database
            if (cred == null)
            {
                //return new HttpStatusCodeResult(400, "Credential not found");
                return Json(new { status = "error", message = "Tài khoản chưa đăng ký FaceID. Vui lòng đăng ký trước." });
            }

            var res = await _fido2.MakeAssertionAsync(assertionResponse, options, cred.PublicKey, cred.SignatureCounter, (args) => Task.FromResult(true));

            // 🔥 Cập nhật signature counter
            UpdateSignatureCounter(Convert.ToBase64String(assertionResponse.Id), res.Counter);

            using (var db = new UnitOfWork())
            {
                var credentialId = Convert.ToBase64String(res.CredentialId);
                var entity = db.MemberCredentialRepository.GetQuery(x => x.CredentialId == credentialId).FirstOrDefault();
                if (entity == null)
                {
                    //return new HttpStatusCodeResult(400, "Credential not found");
                    return Json(new { status = "error", message = "Không tìm thấy thông tin phù hợp." });
                }
                var member = entity.User;
                //if (member.ExpireDate < DateTime.Now)
                //{
                //    return new HttpStatusCodeResult(403, "User exprired.");
                //}

                var userData = member.Username + "|" + member.OfficeId + "|"/* + member.Role + "|" + member.Image + "|"*/ + member.Id;
                var ticket = new FormsAuthenticationTicket(2, member.Username, DateTime.Now, DateTime.Now.AddMinutes(15), false, userData);
                var encTicket = FormsAuthentication.Encrypt(ticket);
                //Lưu theo phiên - not set expired
                Response.Cookies.Add(new HttpCookie(".ASPXAUTHMEMBER", encTicket) { SameSite = SameSiteMode.Lax, Secure = true, /*Expires = ticket.Expiration*/ });
                return Json(new { status = "ok", redirectUrl = Url.Action("Index", "Home") });
            }
        }
        // =========================================
        // 🔥 Các hàm DB giả lập
        private void SaveCredential(AttestationVerificationSuccess cred,string userId)
        {
            using (var db = new UnitOfWork())
            {
                var credId = Convert.ToBase64String(cred.CredentialId);

                // Check nếu CredentialId đã tồn tại
                var exists = db.MemberCredentialRepository.GetQuery(x => x.CredentialId == credId).Any();
                if (exists)
                {
                    // Credential đã tồn tại, không lưu nữa
                    return;
                }
                db.MemberCredentialRepository.Insert(new MemberCredential
                {
                    UserId = Convert.ToInt32(userId),
                    CredentialId = Convert.ToBase64String(cred.CredentialId),
                    PublicKey = Convert.ToBase64String(cred.PublicKey),
                    SignatureCounter = cred.Counter,
                    UserHandle = Convert.ToBase64String(cred.User.Id),
                    AuthenticatorAttestationGuid = cred.Aaguid,
                    DeviceName = GetDeviceName() + " / " + GetBrowser(),
                    Platform = GetOperatingSystem() + " / " + Request.Browser.Platform
                });
                db.Save();
            }
        }

        private StoredCredential LoadCredential(string credentialId)
        {
            using (var db = new UnitOfWork())
            {
                var entity = db.MemberCredentialRepository.GetQuery(x => x.CredentialId == credentialId).FirstOrDefault();
                if (entity == null) return null;

                return new StoredCredential
                {
                    UserId = Convert.FromBase64String(entity.CredentialId),
                    PublicKey = Convert.FromBase64String(entity.PublicKey),
                    SignatureCounter = (uint)entity.SignatureCounter
                };
            }
        }

        private PublicKeyCredentialDescriptor[] LoadUserKeys(byte[] userHandle)
        {
            using (var db = new UnitOfWork())
            {
                var finaluserHandle = Convert.ToBase64String(userHandle);
                var list = db.MemberCredentialRepository.GetQuery(x => x.UserHandle == finaluserHandle).ToList();
                return list.Select(x => new PublicKeyCredentialDescriptor(Convert.FromBase64String(x.CredentialId))).ToArray();
            }
        }
        [HttpPost]
        //public async Task<JsonResult> RemoveFaceId(string credentialId)
        //{
        //    using (var db = new UnitOfWork())
        //    {
        //        var entity = db.MemberCredentialRepository.GetQuery(x => x.CredentialId == credentialId).FirstOrDefault();
        //        if (entity == null) return Json(new { status = "error", msg = "Thông tin FaceId không chính xác" });

        //        db.MemberCredentialRepository.Delete(entity);
        //        await db.SaveAsync();
        //        return Json(new { status = "ok", msg = "Thông tin FaceId không chính xác" });
        //    }
        //}

        private void UpdateSignatureCounter(string credentialId, long counter)
        {
            using (var db = new UnitOfWork())
            {
                var entity = db.MemberCredentialRepository.GetQuery(x => x.CredentialId == credentialId).FirstOrDefault();
                if (entity != null)
                {
                    entity.SignatureCounter = counter;
                    db.Save();
                }
            }
        }

        private string GetDeviceName()
        {
            var manufacturer = Request.Browser.MobileDeviceManufacturer ?? "";
            var model = Request.Browser.MobileDeviceModel ?? "";

            if (!string.IsNullOrWhiteSpace(manufacturer + model))
            {
                return $"{manufacturer} / {model}";
            }

            return Request.Browser.Type;
        }

        private string GetOperatingSystem()
        {
            string userAgent = Request.UserAgent ?? "";

            if (userAgent.Contains("Windows NT 10.0"))
                return "Windows 10/11";
            if (userAgent.Contains("Windows NT 6.3"))
                return "Windows 8.1";
            if (userAgent.Contains("Windows NT 6.2"))
                return "Windows 8";
            if (userAgent.Contains("Windows NT 6.1"))
                return "Windows 7";
            if (userAgent.Contains("iPhone"))
                return "iPhone iOS";
            if (userAgent.Contains("iPad"))
                return "iPad iOS";
            if (userAgent.Contains("Android"))
                return "Android";
            if (userAgent.Contains("Macintosh"))
                return "MacOS";
            if (userAgent.Contains("Linux"))
                return "Linux";

            return "Unknown OS";
        }

        private string GetBrowser()
        {
            string userAgent = Request.UserAgent ?? "";

            if (userAgent.Contains("Edge"))
                return "Edge";
            if (userAgent.Contains("OPR") || userAgent.Contains("Opera"))
                return "Opera";
            if (userAgent.Contains("Chrome"))
                return "Chrome";
            if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
                return "Safari";
            if (userAgent.Contains("Firefox"))
                return "Firefox";
            if (userAgent.Contains("MSIE") || userAgent.Contains("Trident/7"))
                return "Internet Explorer";

            return "Unknown Browser";
        }
    }
}
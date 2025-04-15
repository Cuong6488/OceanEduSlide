using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace OceanEduSlide.DAL
{
    public class EmailValidationAttribute : ValidationAttribute
    {
        // Biểu thức chính quy kiểm tra cú pháp email
        private const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        // Hàm kiểm tra tính hợp lệ
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var email = value as string;

            // Nếu không có giá trị, bỏ qua kiểm tra
            if (string.IsNullOrWhiteSpace(email))
            {
                return ValidationResult.Success;
            }

            // 1. Kiểm tra cú pháp email
            if (!Regex.IsMatch(email, EmailPattern))
            {
                return new ValidationResult(GetErrorMessage("Invalid email format."));
            }

            // 2. Kiểm tra tên miền email qua DNS
            string domain = email.Split('@')[1];
            if (!IsDomainValid(domain))
            {
                return new ValidationResult(GetErrorMessage("Email domain is invalid or does not exist."));
            }

            // 3. Kiểm tra tên miền email qua DNS
            if (!IsEmailValidSmtp(domain))
            {
                return new ValidationResult(GetErrorMessage("Email is invalid or does not exist."));
            }

            // Email hợp lệ
            return ValidationResult.Success;
        }

        // Kiểm tra DNS của tên miền
        private bool IsDomainValid(string domain)
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(domain); // Tra cứu DNS
                return hostEntry != null; // Domain tồn tại
            }
            catch
            {
                return false; // Domain không hợp lệ hoặc không tồn tại
            }
        }
        public bool IsEmailValidSmtp(string domain)
        {
            try
            {
                // Lấy MX record
                var hostEntry = Dns.GetHostEntry(domain);
                var smtpServer = hostEntry.AddressList[0];

                // Kết nối đến SMTP server
                using (var client = new TcpClient())
                {
                    client.Connect(smtpServer, 25);
                    return client.Connected; // Nếu kết nối được nghĩa là hợp lệ
                }
            }
            catch
            {
                return false;
            }
        }

        // Trả về thông báo lỗi song ngữ
        private string GetErrorMessage(string englishMessage)
        {
            // Lấy văn hóa hiện tại của ứng dụng
            var currentCulture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            // Mapping thông báo lỗi tiếng Việt
            var translations = new Dictionary<string, string>
            {
                { "Invalid email format.", "Địa chỉ email không đúng định dạng." },
                { "Email domain is invalid or does not exist.", "Tên miền của email không hợp lệ hoặc không tồn tại." },
                { "Email is invalid or does not exist.", "Email không hợp lệ hoặc không tồn tại." }
            };

            // Trả về thông báo tương ứng với ngôn ngữ
            if (currentCulture == "vi" && translations.ContainsKey(englishMessage))
            {
                return translations[englishMessage];
            }

            // Mặc định trả về tiếng Anh
            return englishMessage;
        }
    }
}
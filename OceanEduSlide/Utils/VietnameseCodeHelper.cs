using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace OceanEduSlide.Utils
{
    public class VietnameseCodeHelper
    {
        public static string NormalizeVietnameseCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            // replace ký tự "Đ" sai unicode chuẩn
            return input
                .Trim()
                .Replace('Ð', 'Đ')
                .Replace('ð', 'đ')
                .Normalize(NormalizationForm.FormC);
        }
    }
}
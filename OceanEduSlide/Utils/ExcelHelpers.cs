using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace OceanEduSlide.Utils
{
    public class ExcelHelpers
    {
        public static string RemoveHtmlAndKeepLineBreaks(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            // Chuyển <br>, <br/>, <p>, </p> thành dấu xuống dòng
            text = Regex.Replace(text, @"<(br|BR)\s*/?>", Environment.NewLine);
            text = Regex.Replace(text, @"</p\s*>", Environment.NewLine);
            text = Regex.Replace(text, @"<p\s*>", string.Empty);

            // Loại bỏ tất cả các thẻ HTML còn lại
            text = Regex.Replace(text, "<.*?>", string.Empty);

            // Loại bỏ khoảng trắng dư thừa
            text = Regex.Replace(text, "[\\s\\r\\n]+", " ");

            // Thêm lại các xuống dòng đã chuyển
            text = text.Replace(" " + Environment.NewLine, Environment.NewLine); // tránh double space + newline
            text = HttpUtility.HtmlDecode(text).Trim();

            return text;
        }

    }
}
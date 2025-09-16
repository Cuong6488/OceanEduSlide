using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Web;

namespace OceanEduSlide.EnumHelpers
{
    public static class EnumExtensions
    {
        //public static string GetDisplayName(this Enum enumValue)
        //{
        //    return enumValue.GetType()
        //        .GetMember(enumValue.ToString())
        //        .First()
        //        .GetCustomAttribute<DisplayAttribute>()?
        //        .GetName() ?? enumValue.ToString();
        //}
        public static string GetDisplayName(this Enum enumValue)
        {
            if (enumValue == null)
                return string.Empty;

            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .FirstOrDefault()?
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName() ?? enumValue.ToString();
        }

    }
}
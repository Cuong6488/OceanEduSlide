using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace OceanEduSlide.Utils
{
    public static class ModelStateExtensions
    {
        public static List<ModelStateErrorInfo> GetAllErrors(this ModelStateDictionary modelState)
        {
            return modelState
                .Where(x => x.Value.Errors.Any())
                .SelectMany(x => x.Value.Errors.Select(e => new ModelStateErrorInfo
                {
                    Field = x.Key,
                    Message = e.ErrorMessage
                }))
                .ToList();
        }
    }

    public class ModelStateErrorInfo
    {
        public string Field { get; set; }
        public string Message { get; set; }
    }
}
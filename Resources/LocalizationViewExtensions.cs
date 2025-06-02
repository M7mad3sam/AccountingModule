using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;

namespace AspNetCoreMvcTemplate.Resources
{
    public static class LocalizationViewExtensions
    {
        public static string Localize(this IHtmlHelper htmlHelper, string key)
        {
            var localizer = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService(typeof(IStringLocalizer<SharedResource>)) as IStringLocalizer<SharedResource>;
            
            return localizer?[key] ?? key;
        }
    }
}

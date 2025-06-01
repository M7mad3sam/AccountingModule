using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace AspNetCoreMvcTemplate.Resources
{
    public class SharedResource
    {
        // This class is used to share resources across the application
    }

    public class LocalizationService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;

        public LocalizationService(IStringLocalizer<SharedResource> localizer)
        {
            _localizer = localizer;
        }

        public string GetLocalizedString(string key)
        {
            return _localizer[key];
        }
    }

    public static class LocalizationExtensions
    {
        public static IServiceCollection AddAppLocalization(this IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = null);

            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[]
                {
                    new CultureInfo("en-US"),
                    new CultureInfo("ar-EG")
                };

                options.DefaultRequestCulture = new RequestCulture("en-US");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
                
                // Custom request culture provider that reads the culture from a cookie
                options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                {
                    var userLangs = context.Request.Cookies["Language"];
                    var culture = string.IsNullOrEmpty(userLangs) ? "en-US" : userLangs;
                    
                    return Task.FromResult(new ProviderCultureResult(culture));
                }));
            });

            services.AddSingleton<LocalizationService>();

            return services;
        }

        public static IApplicationBuilder UseAppLocalization(this IApplicationBuilder app)
        {
            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            return app;
        }
    }
}

using Devbraco.Journeys;
using Devbraco.Web.Journeys;
using Microsoft.AspNetCore.Authentication.Cookies;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace Devbraco.Web;

public sealed class JourneyWebComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IWizardStateStore, WizardStateStore>();
        builder.Services.AddSingleton<RenewalSubmissionStore>();
        builder.Services.AddScoped<IRenewalClient, LocalRenewalClient>();
        builder.Services.AddAuthentication().AddCookie(DevMember.Scheme, options =>
        {
            options.Cookie.Name = "Devbraco.Member";
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
        });

        builder.Services.Configure<UmbracoPipelineOptions>(options =>
        {
            options.AddFilter(new UmbracoPipelineFilter(nameof(JourneyWebComposer))
            {
                Endpoints = app => app.UseEndpoints(endpoints => endpoints.MapControllers())
            });
        });
    }
}

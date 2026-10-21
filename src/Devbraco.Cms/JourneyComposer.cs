using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Notifications;

namespace Devbraco.Cms;

public sealed class JourneyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, JourneyContentBootstrap>();
        builder.AddNotificationHandler<ContentSavingNotification, JourneyStructureGuard>();
        builder.AddNotificationHandler<ContentDeletingNotification, JourneyStructureGuard>();
        builder.Services.AddScoped<Devbraco.Journeys.ICopyTrace, Devbraco.Journeys.CopyTrace>();
        builder.Services.AddScoped<Devbraco.Journeys.IJourneyCopyProvider, CmsJourneyCopyProvider>();
    }
}

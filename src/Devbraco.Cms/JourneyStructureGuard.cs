using Devbraco.Journeys;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;

namespace Devbraco.Cms;

/// <summary>
/// Clients may blank copy (that means inherit). They may not change structural keys.
/// </summary>
public sealed class JourneyStructureGuard : INotificationHandler<ContentSavingNotification>, INotificationHandler<ContentDeletingNotification>
{
    private static readonly HashSet<string> Aliases = new(StringComparer.Ordinal)
    {
        JourneyContentBootstrap.RootAlias,
        JourneyContentBootstrap.JourneyAlias,
        JourneyContentBootstrap.CopyEntryAlias,
        JourneyContentBootstrap.StepCopyAlias
    };

    public void Handle(ContentSavingNotification notification)
    {
        foreach (var entity in notification.SavedEntities)
        {
            if (!Aliases.Contains(entity.ContentType.Alias) || !entity.HasIdentity)
            {
                continue;
            }

            if (IsStructuralChange(entity, "copyKey")
                || IsStructuralChange(entity, "stepSlug")
                || IsStructuralChange(entity, "journeySlug"))
            {
                notification.CancelOperation(new EventMessage(
                    "Journey structure",
                    "Copy keys, step slugs and journey slugs are part of the application and cannot be changed.",
                    EventMessageType.Error));
            }
        }
    }

    public void Handle(ContentDeletingNotification notification)
    {
        foreach (var entity in notification.DeletedEntities)
        {
            if (Aliases.Contains(entity.ContentType.Alias))
            {
                notification.CancelOperation(new EventMessage(
                    "Journey structure",
                    "Journey copy is created by the application and cannot be deleted.",
                    EventMessageType.Error));
            }
        }
    }

    private static bool IsStructuralChange(IContent entity, string alias)
    {
        if (!entity.HasProperty(alias) || !entity.IsPropertyDirty(alias))
        {
            return false;
        }

        var current = entity.GetValue(alias)?.ToString();
        var published = entity.GetValue(alias, published: true)?.ToString();
        return !string.Equals(current, published, StringComparison.Ordinal);
    }
}

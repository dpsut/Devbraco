using System.Data;
using Devbraco.Journeys;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Devbraco.Cms;

/// <summary>
/// Creates journey document types and copy nodes once. Existing nodes are never overwritten.
/// </summary>
public sealed class JourneyContentBootstrap : INotificationHandler<UmbracoApplicationStartedNotification>
{
    public const string RootAlias = "journeysRoot";
    public const string JourneyAlias = "journey";
    public const string CopyEntryAlias = "copyEntry";
    public const string StepCopyAlias = "stepCopy";

    private readonly ICoreScopeProvider _scopeProvider;
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<JourneyContentBootstrap> _logger;

    public JourneyContentBootstrap(
        ICoreScopeProvider scopeProvider,
        IContentTypeService contentTypeService,
        IContentService contentService,
        IDataTypeService dataTypeService,
        IShortStringHelper shortStringHelper,
        ILogger<JourneyContentBootstrap> logger)
    {
        _scopeProvider = scopeProvider;
        _contentTypeService = contentTypeService;
        _contentService = contentService;
        _dataTypeService = dataTypeService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        using var scope = _scopeProvider.CreateCoreScope(
            IsolationLevel.Unspecified,
            RepositoryCacheMode.Unspecified,
            eventDispatcher: null,
            scopedNotificationPublisher: null,
            scopeFileSystems: null,
            callContext: false,
            autoComplete: false);
        EnsureTypes();
        EnsureContent();
        scope.Complete();
    }

    private void EnsureTypes()
    {
        var textbox = _dataTypeService.GetDataType(Constants.DataTypes.Textbox)
            ?? throw new InvalidOperationException("The built-in text box data type is missing.");
        var textarea = _dataTypeService.GetDataType(Constants.DataTypes.Textarea)
            ?? throw new InvalidOperationException("The built-in text area data type is missing.");

        var copyEntry = GetOrCreate(CopyEntryAlias, "Copy entry", "icon-edit", type =>
        {
            AddText(type, textbox, "copyKey", "Copy key", mandatory: true);
            AddText(type, textbox, "label", "Label");
            AddText(type, textarea, "helpText", "Help text");
            AddText(type, textbox, "placeholder", "Placeholder");
            AddText(type, textbox, "validationMessage", "Validation message");
            AddText(type, textbox, "group", "Group");
        });

        var stepCopy = GetOrCreate(StepCopyAlias, "Step copy", "icon-list", type =>
        {
            AddText(type, textbox, "stepSlug", "Step slug", mandatory: true);
            AddText(type, textbox, "title", "Title");
            AddText(type, textarea, "intro", "Intro");
        });

        var journey = GetOrCreate(JourneyAlias, "Journey", "icon-sitemap", type =>
        {
            AddText(type, textbox, "journeySlug", "Journey slug", mandatory: true);
            AddText(type, textbox, "title", "Title");
            type.AllowedContentTypes =
            [
                new ContentTypeSort(copyEntry.Key, 0, copyEntry.Alias),
                new ContentTypeSort(stepCopy.Key, 1, stepCopy.Alias)
            ];
        });

        GetOrCreate(RootAlias, "Journeys root", "icon-folder", type =>
        {
            type.AllowedAsRoot = true;
            type.AllowedContentTypes = [new ContentTypeSort(journey.Key, 0, journey.Alias)];
        });
    }

    private IContentType GetOrCreate(string alias, string name, string icon, Action<ContentType> configure)
    {
        var existing = _contentTypeService.Get(alias);
        if (existing is not null)
        {
            return existing;
        }

        var type = new ContentType(_shortStringHelper, -1)
        {
            Alias = alias,
            Name = name,
            Icon = icon,
            Variations = ContentVariation.Nothing
        };
        configure(type);
        _contentTypeService.Save(type);
        _logger.LogInformation("Created document type {Alias}", alias);
        return type;
    }

    private void AddText(ContentType type, IDataType dataType, string alias, string name, bool mandatory = false)
    {
        var property = new PropertyType(_shortStringHelper, dataType, alias)
        {
            Name = name,
            Mandatory = mandatory
        };

        var group = type.PropertyGroups.FirstOrDefault(candidate => candidate.Alias == "content");
        if (group is null)
        {
            group = new PropertyGroup(false)
            {
                Alias = "content",
                Name = "Content",
                SortOrder = 0
            };
            type.PropertyGroups.Add(group);
        }

        group.PropertyTypes!.Add(property);
    }

    private void EnsureContent()
    {
        var root = EnsureNode(StableGuid.For("journeys-root"), "Journeys", RootAlias, parentKey: null, values: null);

        foreach (var journey in JourneyCatalog.All)
        {
            var journeyNode = EnsureNode(
                StableGuid.For(journey.CopyKey),
                journey.DefaultTitle,
                JourneyAlias,
                root.Key,
                new Dictionary<string, string?>
                {
                    ["journeySlug"] = journey.Slug,
                    ["title"] = journey.DefaultTitle
                });

            foreach (var step in journey.Layout.Steps)
            {
                EnsureNode(
                    StableGuid.For(step.CopyKey),
                    step.DefaultTitle,
                    StepCopyAlias,
                    journeyNode.Key,
                    new Dictionary<string, string?>
                    {
                        ["stepSlug"] = step.Slug,
                        ["title"] = step.DefaultTitle,
                        ["intro"] = step.DefaultIntro
                    });
            }

            foreach (var field in journey.Fields)
            {
                var group = journey.Layout.Steps.FirstOrDefault(step => step.FieldKeys.Contains(field.Key))?.Slug;
                EnsureNode(
                    StableGuid.For(field.CopyKey),
                    field.DefaultLabel,
                    CopyEntryAlias,
                    journeyNode.Key,
                    new Dictionary<string, string?>
                    {
                        ["copyKey"] = field.CopyKey,
                        ["label"] = field.DefaultLabel,
                        ["helpText"] = field.DefaultHelp,
                        ["placeholder"] = field.DefaultPlaceholder,
                        ["group"] = group
                    });
            }
        }
    }

    private IContent EnsureNode(
        Guid key,
        string name,
        string alias,
        Guid? parentKey,
        IReadOnlyDictionary<string, string?>? values)
    {
        var existing = _contentService.GetById(key);
        if (existing is not null)
        {
            return existing;
        }

        IContent content;
        if (parentKey is null)
        {
            content = _contentService.Create(name, Constants.System.Root, alias);
        }
        else
        {
            _ = _contentService.GetById(parentKey.Value)
                ?? throw new InvalidOperationException($"Parent {parentKey} was not found for {alias}.");
            content = _contentService.Create(name, parentKey.Value, alias);
        }

        content.Key = key;
        if (values is not null)
        {
            foreach (var (property, value) in values)
            {
                if (value is not null)
                {
                    content.SetValue(property, value, culture: null, segment: null);
                }
            }
        }

        var saved = _contentService.Save(content);
        if (!saved.Success)
        {
            throw new InvalidOperationException($"Saving {alias} '{name}' failed: {saved.Result}");
        }

        var published = _contentService.Publish(content, ["*"], userId: -1);
        if (!published.Success)
        {
            throw new InvalidOperationException($"Publishing {alias} '{name}' failed: {published.Result}");
        }

        _logger.LogInformation("Created {Alias} {Name}", alias, name);
        return content;
    }
}

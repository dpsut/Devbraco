using Devbraco.Journeys;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Extensions;

namespace Devbraco.Cms;

public sealed class CmsJourneyCopyProvider : IJourneyCopyProvider
{
    private readonly IPublishedContentCache _cache;
    private readonly ICopyTrace _trace;
    private Dictionary<Guid, IPublishedContent>? _nodes;

    public CmsJourneyCopyProvider(IPublishedContentCache cache, ICopyTrace trace)
    {
        _cache = cache;
        _trace = trace;
    }

    public string JourneyTitle(IJourneyDefinition journey) =>
        Text(journey.CopyKey, "title", journey.DefaultTitle);

    public string StepTitle(ResolvedStep step) =>
        Text(step.CopyKey, "title", step.DefaultTitle);

    public string? StepIntro(ResolvedStep step) =>
        Optional(step.CopyKey, "intro", step.DefaultIntro);

    public string Label(FieldDefinition field) =>
        Text(field.CopyKey, "label", field.DefaultLabel);

    public string? Help(FieldDefinition field) =>
        Optional(field.CopyKey, "helpText", field.DefaultHelp);

    public string? Placeholder(FieldDefinition field) =>
        Optional(field.CopyKey, "placeholder", field.DefaultPlaceholder);

    public string ValidationMessage(FieldDefinition field, string codeDefault) =>
        Text(field.CopyKey, "validationMessage", codeDefault);

    private string Text(string copyKey, string property, string fallback)
    {
        var value = Read(copyKey, property);
        if (string.IsNullOrWhiteSpace(value))
        {
            _trace.RecordFallback(copyKey + "." + property);
            return fallback;
        }

        return value;
    }

    private string? Optional(string copyKey, string property, string? fallback)
    {
        var value = Read(copyKey, property);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            _trace.RecordFallback(copyKey + "." + property);
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private string? Read(string copyKey, string property)
    {
        _nodes ??= Load();
        return _nodes.TryGetValue(StableGuid.For(copyKey), out var node)
            ? node.Value<string>(property)
            : null;
    }

    private Dictionary<Guid, IPublishedContent> Load()
    {
        var nodes = new Dictionary<Guid, IPublishedContent>();
        var root = _cache.GetById(StableGuid.For("journeys-root"));
        if (root is null)
        {
            return nodes;
        }

        nodes[root.Key] = root;
        foreach (var journey in root.Children())
        {
            nodes[journey.Key] = journey;
            foreach (var child in journey.Children())
            {
                nodes[child.Key] = child;
            }
        }

        return nodes;
    }
}

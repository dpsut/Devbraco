using System.Security.Cryptography;
using System.Text;

namespace Devbraco.Journeys;

public sealed class StepDefinition
{
    public StepDefinition(string slug)
    {
        Slug = slug;
    }

    public string Slug { get; }

    public required string CopyKey { get; init; }

    public required string DefaultTitle { get; init; }

    public string? DefaultIntro { get; init; }

    public required IReadOnlyList<string> FieldKeys { get; init; }
}

public sealed class StepLayout
{
    private StepLayout(IReadOnlyList<StepDefinition> steps)
    {
        Steps = steps;
    }

    public IReadOnlyList<StepDefinition> Steps { get; }

    public string Version { get; private init; } = "";

    public static StepLayout From(params (string Slug, string CopyKey, string Title, string? Intro, string[] Fields)[] steps)
    {
        var definitions = steps.Select(step => new StepDefinition(step.Slug)
        {
            CopyKey = step.CopyKey,
            DefaultTitle = step.Title,
            DefaultIntro = step.Intro,
            FieldKeys = step.Fields
        }).ToArray();

        var raw = string.Join("|", definitions.Select(step => step.Slug + ":" + string.Join(",", step.FieldKeys)));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

        return new StepLayout(definitions)
        {
            Version = Convert.ToHexString(hash)[..12].ToLowerInvariant()
        };
    }
}

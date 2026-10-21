namespace Devbraco.Journeys;

public sealed class FieldDefinition
{
    public FieldDefinition(string key, FieldType type)
    {
        Key = key;
        Type = type;
    }

    public string Key { get; }

    public FieldType Type { get; }

    public required string CopyKey { get; init; }

    public required string DefaultLabel { get; init; }

    public string? DefaultHelp { get; init; }

    public string? DefaultPlaceholder { get; init; }

    public bool Sensitive { get; init; }

    public When IncludeWhen { get; init; } = When.Always;

    public IReadOnlyList<Rule> Rules { get; init; } = [];
}

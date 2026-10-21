namespace Devbraco.Journeys;

public interface ICopyTrace
{
    void RecordFallback(string copyKey);

    IReadOnlyCollection<string> Keys { get; }
}

public sealed class CopyTrace : ICopyTrace
{
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);

    public void RecordFallback(string copyKey) => _keys.Add(copyKey);

    public IReadOnlyCollection<string> Keys => _keys;
}

public interface IJourneyCopyProvider
{
    string JourneyTitle(IJourneyDefinition journey);

    string StepTitle(ResolvedStep step);

    string? StepIntro(ResolvedStep step);

    string Label(FieldDefinition field);

    string? Help(FieldDefinition field);

    string? Placeholder(FieldDefinition field);

    string ValidationMessage(FieldDefinition field, string codeDefault);
}

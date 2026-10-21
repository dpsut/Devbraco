namespace Devbraco.Journeys;

public interface IJourneyDefinition
{
    string Slug { get; }

    string CopyKey { get; }

    string DefaultTitle { get; }

    IReadOnlyList<FieldDefinition> Fields { get; }

    StepLayout Layout { get; }
}

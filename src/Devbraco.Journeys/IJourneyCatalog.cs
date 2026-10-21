namespace Devbraco.Journeys;

/// <summary>
/// The journeys a deployment runs. The framework never names one of them.
/// </summary>
public interface IJourneyCatalog
{
    IReadOnlyList<IJourneyDefinition> Journeys { get; }

    IJourneyDefinition? Find(string slug) =>
        Journeys.FirstOrDefault(journey => journey.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
}

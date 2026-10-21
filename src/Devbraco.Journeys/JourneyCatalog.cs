namespace Devbraco.Journeys;

public static class JourneyCatalog
{
    public static IReadOnlyList<IJourneyDefinition> All { get; } = [new RenewalJourney()];

    public static IJourneyDefinition? Find(string slug) =>
        All.FirstOrDefault(journey => journey.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
}

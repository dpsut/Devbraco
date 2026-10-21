namespace Devbraco.Journeys.Membership;

/// <summary>
/// The membership journeys this deployment runs. A client profile will choose a catalogue
/// in Phase 1; until then there is one.
/// </summary>
public sealed class MembershipJourneys : IJourneyCatalog
{
    public IReadOnlyList<IJourneyDefinition> Journeys { get; } = [new RenewalJourney()];
}

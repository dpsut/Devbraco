using System.Reflection;
using Devbraco.Journeys.Membership;

namespace Devbraco.Journeys.Tests;

/// <summary>
/// The dependency rules that keep this codebase shareable across clients. These are the
/// guardrails, not the documentation.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly Framework = typeof(FieldDefinition).Assembly;
    private static readonly Assembly Journeys = typeof(RenewalJourney).Assembly;

    [Fact]
    public void The_framework_does_not_reference_a_CMS()
    {
        Assert.DoesNotContain(References(Framework), name => name.StartsWith("Umbraco", StringComparison.Ordinal));
    }

    [Fact]
    public void The_journeys_do_not_reference_a_CMS()
    {
        Assert.DoesNotContain(References(Journeys), name => name.StartsWith("Umbraco", StringComparison.Ordinal));
    }

    [Fact]
    public void The_framework_does_not_reference_any_journey()
    {
        Assert.DoesNotContain(References(Framework), name =>
            name.StartsWith("Devbraco.Journeys.", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_journey_in_the_catalogue_has_a_valid_layout()
    {
        foreach (var journey in new MembershipJourneys().Journeys)
        {
            Assert.Empty(LayoutValidator.Validate(journey));
        }
    }

    private static IEnumerable<string> References(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? "");
}

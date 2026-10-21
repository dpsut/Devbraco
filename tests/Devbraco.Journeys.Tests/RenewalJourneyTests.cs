using Devbraco.Journeys.Membership;

namespace Devbraco.Journeys.Tests;

/// <summary>
/// Behaviour of the renewal journey itself, as distinct from the framework that runs it.
/// </summary>
public class RenewalJourneyTests
{
    private readonly RenewalJourney _journey = new();

    [Fact]
    public void The_shipped_layout_is_valid()
    {
        Assert.Empty(LayoutValidator.Validate(_journey));
    }

    [Fact]
    public void The_catalogue_exposes_the_journey_by_slug()
    {
        IJourneyCatalog catalogue = new MembershipJourneys();
        Assert.Same(catalogue.Journeys.Single(), catalogue.Find("Renewal"));
    }

    [Fact]
    public void Lapsed_members_see_the_circumstances_step()
    {
        var active = JourneyFlow.VisibleSteps(_journey, Context(MemberSemantic.IsActive));
        var lapsed = JourneyFlow.VisibleSteps(_journey, Context(MemberSemantic.IsLapsed));

        Assert.DoesNotContain(active, step => step.Slug == "circumstances");
        Assert.Contains(lapsed, step => step.Fields.Any(field => field.Key == "lapseReason"));
    }

    [Fact]
    public void Always_sent_fields_are_never_conditional()
    {
        foreach (var key in RenewalJourney.AlwaysSent)
        {
            var field = _journey.Fields.Single(candidate => candidate.Key == key);
            Assert.True(field.IncludeWhen.IsAlways);
        }
    }

    [Fact]
    public void Mapper_omits_lapse_reason_unless_the_member_is_lapsed()
    {
        var answers = new Dictionary<string, string?>
        {
            ["email"] = "person@example.com",
            ["dateOfBirth"] = "1990-04-02",
            ["membershipNumber"] = "M-1024",
            ["lapseReason"] = "Moved away"
        };

        var active = RenewalMapper.Map(answers, Context(MemberSemantic.IsActive));
        var lapsed = RenewalMapper.Map(answers, Context(MemberSemantic.IsLapsed));

        Assert.Null(active.LapseReason);
        Assert.Equal("Moved away", lapsed.LapseReason);
    }

    private static JourneyContext Context(params MemberSemantic[] semantics) => new()
    {
        Semantics = semantics.ToHashSet(),
        Answers = new Dictionary<string, string?>()
    };
}

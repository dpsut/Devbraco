namespace Devbraco.Journeys.Tests;

public class JourneyFlowTests
{
    private readonly RenewalJourney _journey = new();

    [Fact]
    public void Default_layout_is_valid()
    {
        Assert.Empty(LayoutValidator.Validate(_journey));
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

    [Fact]
    public void A_field_that_depends_on_a_later_answer_is_rejected()
    {
        var broken = new BrokenJourney();
        var errors = LayoutValidator.Validate(broken);
        Assert.Contains(errors, error => error.Contains("later step", StringComparison.Ordinal));
    }

    [Fact]
    public void Omitting_a_field_is_a_capability_filter_not_a_hole()
    {
        var effective = _journey.Fields.Select(field => field.Key).Where(key => key != "lapseReason").ToHashSet();
        var errors = LayoutValidator.Validate(_journey, effective);
        Assert.Contains(errors, error => error.Contains("capabilities omit", StringComparison.Ordinal));
    }

    [Fact]
    public void Journeys_assembly_does_not_reference_Umbraco()
    {
        var references = typeof(RenewalJourney).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference =>
            reference.Name?.StartsWith("Umbraco", StringComparison.Ordinal) == true);
    }

    private static JourneyContext Context(params MemberSemantic[] semantics) => new()
    {
        Semantics = semantics.ToHashSet(),
        Answers = new Dictionary<string, string?>()
    };

    private sealed class BrokenJourney : IJourneyDefinition
    {
        public string Slug => "broken";
        public string CopyKey => "broken.journey";
        public string DefaultTitle => "Broken";
        public IReadOnlyList<FieldDefinition> Fields { get; } =
        [
            new FieldDefinition("later", FieldType.Text)
            {
                CopyKey = "broken.field.later",
                DefaultLabel = "Later",
                Rules = [Rule.Required()]
            },
            new FieldDefinition("earlier", FieldType.Text)
            {
                CopyKey = "broken.field.earlier",
                DefaultLabel = "Earlier",
                IncludeWhen = When.AnswerIs("later", "yes"),
                Rules = [Rule.Required()]
            }
        ];

        public StepLayout Layout { get; } = StepLayout.From(
            ("one", "broken.step.one", "One", null, ["earlier"]),
            ("two", "broken.step.two", "Two", null, ["later"]));
    }
}

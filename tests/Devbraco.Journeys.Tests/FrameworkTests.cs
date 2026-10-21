namespace Devbraco.Journeys.Tests;

/// <summary>
/// Framework behaviour, exercised against fixture journeys. No real journey appears here,
/// which is the point: the framework does not know what we happen to be building with it.
/// </summary>
public class FrameworkTests
{
    private readonly TwoStepJourney _journey = new();

    [Fact]
    public void A_valid_layout_reports_no_errors()
    {
        Assert.Empty(LayoutValidator.Validate(_journey));
    }

    [Fact]
    public void A_step_whose_fields_are_all_hidden_is_skipped()
    {
        var withoutSemantic = JourneyFlow.VisibleSteps(_journey, Context());
        var withSemantic = JourneyFlow.VisibleSteps(_journey, Context(MemberSemantic.IsLapsed));

        Assert.DoesNotContain(withoutSemantic, step => step.Slug == "second");
        Assert.Contains(withSemantic, step => step.Slug == "second");
    }

    [Fact]
    public void A_field_missing_from_the_layout_is_rejected()
    {
        var errors = LayoutValidator.Validate(new OrphanFieldJourney());
        Assert.Contains(errors, error => error.Contains("missing from the layout", StringComparison.Ordinal));
    }

    [Fact]
    public void Omitting_a_field_is_reported_as_a_capability_not_a_hole()
    {
        var effective = _journey.Fields.Select(field => field.Key).Where(key => key != "reason").ToHashSet();
        var errors = LayoutValidator.Validate(_journey, effective);
        Assert.Contains(errors, error => error.Contains("capabilities omit", StringComparison.Ordinal));
    }

    [Fact]
    public void A_field_that_depends_on_a_later_answer_is_rejected()
    {
        var errors = LayoutValidator.Validate(new BrokenJourney());
        Assert.Contains(errors, error => error.Contains("later step", StringComparison.Ordinal));
    }

    [Fact]
    public void The_first_incomplete_step_is_the_one_with_a_failing_rule()
    {
        var steps = JourneyFlow.VisibleSteps(_journey, Context(MemberSemantic.IsLapsed));
        var answers = new Dictionary<string, string?> { ["name"] = "Someone" };

        Assert.Equal(1, JourneyFlow.FirstIncompleteIndex(steps, answers));
    }

    private static JourneyContext Context(params MemberSemantic[] semantics) => new()
    {
        Semantics = semantics.ToHashSet(),
        Answers = new Dictionary<string, string?>()
    };

    private sealed class TwoStepJourney : IJourneyDefinition
    {
        public string Slug => "fixture";
        public string CopyKey => "fixture.journey";
        public string DefaultTitle => "Fixture";
        public IReadOnlyList<FieldDefinition> Fields { get; } =
        [
            new FieldDefinition("name", FieldType.Text)
            {
                CopyKey = "fixture.field.name",
                DefaultLabel = "Name",
                Rules = [Rule.Required()]
            },
            new FieldDefinition("reason", FieldType.Text)
            {
                CopyKey = "fixture.field.reason",
                DefaultLabel = "Reason",
                IncludeWhen = When.Semantic(MemberSemantic.IsLapsed),
                Rules = [Rule.Required()]
            }
        ];

        public StepLayout Layout { get; } = StepLayout.From(
            ("first", "fixture.step.first", "First", null, ["name"]),
            ("second", "fixture.step.second", "Second", null, ["reason"]));
    }

    private sealed class OrphanFieldJourney : IJourneyDefinition
    {
        public string Slug => "orphan";
        public string CopyKey => "orphan.journey";
        public string DefaultTitle => "Orphan";
        public IReadOnlyList<FieldDefinition> Fields { get; } =
        [
            new FieldDefinition("shown", FieldType.Text)
            {
                CopyKey = "orphan.field.shown",
                DefaultLabel = "Shown"
            },
            new FieldDefinition("forgotten", FieldType.Text)
            {
                CopyKey = "orphan.field.forgotten",
                DefaultLabel = "Forgotten"
            }
        ];

        public StepLayout Layout { get; } = StepLayout.From(
            ("only", "orphan.step.only", "Only", null, ["shown"]));
    }

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

using Devbraco.Journeys;

namespace Devbraco.Web.Journeys;

public sealed class FieldViewModel
{
    public required string Key { get; init; }

    public required FieldType Type { get; init; }

    public required string Label { get; init; }

    public string? Help { get; init; }

    public string? Placeholder { get; init; }

    public string? Value { get; init; }

    public string? Error { get; init; }

    public bool Sensitive { get; init; }
}

public sealed class JourneyStepViewModel
{
    public required string JourneySlug { get; init; }

    public required string JourneyTitle { get; init; }

    public required string StepSlug { get; init; }

    public required string StepTitle { get; init; }

    public string? Intro { get; init; }

    public required int StepNumber { get; init; }

    public required int StepCount { get; init; }

    public required IReadOnlyList<FieldViewModel> Fields { get; init; }

    public required IReadOnlyList<MemberSemantic> Semantics { get; init; }

    public required IReadOnlyCollection<string> FallbackKeys { get; init; }
}

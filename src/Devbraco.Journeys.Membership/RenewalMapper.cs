namespace Devbraco.Journeys.Membership;

public sealed class RenewalSubmission
{
    public required string Email { get; init; }

    public required DateOnly DateOfBirth { get; init; }

    public required string MembershipNumber { get; init; }

    public string? LapseReason { get; init; }
}

public static class RenewalMapper
{
    public static RenewalSubmission Map(IReadOnlyDictionary<string, string?> answers, JourneyContext context)
    {
        var journey = new RenewalJourney();
        var lapse = journey.Fields.Single(field => field.Key == "lapseReason");
        var includeLapse = lapse.IncludeWhen.Evaluate(context);

        return new RenewalSubmission
        {
            Email = Required(answers, "email"),
            DateOfBirth = DateOnly.ParseExact(Required(answers, "dateOfBirth"), "yyyy-MM-dd"),
            MembershipNumber = Required(answers, "membershipNumber"),
            LapseReason = includeLapse ? answers.GetValueOrDefault("lapseReason") : null
        };
    }

    private static string Required(IReadOnlyDictionary<string, string?> answers, string key)
    {
        if (answers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException($"Cannot map renewal without '{key}'.");
    }
}

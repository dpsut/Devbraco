namespace Devbraco.Journeys.Membership;

/// <summary>
/// Sample journey for Phase 0. Payment is not collected here: card data never enters the catalogue.
/// </summary>
public sealed class RenewalJourney : IJourneyDefinition
{
    public string Slug => "renewal";

    public string CopyKey => "renewal.journey";

    public string DefaultTitle => "Renew your membership";

    public IReadOnlyList<FieldDefinition> Fields { get; } =
    [
        new FieldDefinition("email", FieldType.Email)
        {
            CopyKey = "renewal.field.email",
            DefaultLabel = "Email address",
            DefaultHelp = "We'll send your confirmation here.",
            Sensitive = true,
            Rules = [Rule.Required(), Rule.Email(), Rule.MaxLength(120)]
        },
        new FieldDefinition("dateOfBirth", FieldType.Date)
        {
            CopyKey = "renewal.field.dateOfBirth",
            DefaultLabel = "Date of birth",
            Sensitive = true,
            Rules = [Rule.Required(), Rule.MinAge(18)]
        },
        new FieldDefinition("membershipNumber", FieldType.Text)
        {
            CopyKey = "renewal.field.membershipNumber",
            DefaultLabel = "Membership number",
            DefaultPlaceholder = "For example M-1024",
            Rules = [Rule.Required(), Rule.MaxLength(20)]
        },
        new FieldDefinition("lapseReason", FieldType.Text)
        {
            CopyKey = "renewal.field.lapseReason",
            DefaultLabel = "Why did the membership lapse?",
            IncludeWhen = When.Semantic(MemberSemantic.IsLapsed),
            Rules = [Rule.Required(), Rule.MaxLength(200)]
        }
    ];

    public StepLayout Layout { get; } = StepLayout.From(
        ("your-details", "renewal.step.your-details", "Your details", "Tell us how to reach you.", ["email", "dateOfBirth", "membershipNumber"]),
        ("circumstances", "renewal.step.circumstances", "Your membership", null, ["lapseReason"]));

    /// <summary>Fields the API contract always expects. These must never be conditionally hidden.</summary>
    public static IReadOnlyList<string> AlwaysSent { get; } = ["email", "dateOfBirth", "membershipNumber"];
}

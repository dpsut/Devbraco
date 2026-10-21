namespace Devbraco.Journeys;

public sealed class JourneyContext
{
    public required IReadOnlySet<MemberSemantic> Semantics { get; init; }

    public required IReadOnlyDictionary<string, string?> Answers { get; init; }

    public bool Has(MemberSemantic semantic) => Semantics.Contains(semantic);

    public static JourneyContext Empty { get; } = new()
    {
        Semantics = new HashSet<MemberSemantic>(),
        Answers = new Dictionary<string, string?>()
    };
}

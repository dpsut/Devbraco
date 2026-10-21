namespace Devbraco.Journeys;

/// <summary>
/// A declarative inclusion condition. It is not a lambda, so layout checks and tests can read it.
/// </summary>
public abstract class When
{
    public static When Always { get; } = new AlwaysWhen();

    public static When Semantic(MemberSemantic semantic) => new SemanticWhen(semantic);

    public static When AnswerIs(string fieldKey, string expected) => new AnswerWhen(fieldKey, expected);

    public abstract bool IsAlways { get; }

    public abstract IReadOnlyList<string> DependsOn { get; }

    public abstract bool Evaluate(JourneyContext context);

    private sealed class AlwaysWhen : When
    {
        public override bool IsAlways => true;

        public override IReadOnlyList<string> DependsOn => [];

        public override bool Evaluate(JourneyContext context) => true;
    }

    private sealed class SemanticWhen(MemberSemantic semantic) : When
    {
        public override bool IsAlways => false;

        public override IReadOnlyList<string> DependsOn => [];

        public override bool Evaluate(JourneyContext context) => context.Has(semantic);
    }

    private sealed class AnswerWhen(string fieldKey, string expected) : When
    {
        public override bool IsAlways => false;

        public override IReadOnlyList<string> DependsOn => [fieldKey];

        public override bool Evaluate(JourneyContext context) =>
            context.Answers.TryGetValue(fieldKey, out var value)
            && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}

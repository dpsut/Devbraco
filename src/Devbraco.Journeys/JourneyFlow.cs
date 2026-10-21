namespace Devbraco.Journeys;

public sealed record ResolvedStep(
    string Slug,
    string CopyKey,
    string DefaultTitle,
    string? DefaultIntro,
    IReadOnlyList<FieldDefinition> Fields);

public static class JourneyFlow
{
    public static IReadOnlyList<ResolvedStep> VisibleSteps(
        IJourneyDefinition journey,
        JourneyContext context,
        StepLayout? layout = null)
    {
        var fields = journey.Fields.ToDictionary(field => field.Key);
        var steps = (layout ?? journey.Layout).Steps;
        var visible = new List<ResolvedStep>();

        foreach (var step in steps)
        {
            var included = new List<FieldDefinition>();
            foreach (var key in step.FieldKeys)
            {
                if (!fields.TryGetValue(key, out var field))
                {
                    continue;
                }

                if (field.IncludeWhen.Evaluate(context))
                {
                    included.Add(field);
                }
            }

            if (included.Count == 0)
            {
                continue;
            }

            visible.Add(new ResolvedStep(step.Slug, step.CopyKey, step.DefaultTitle, step.DefaultIntro, included));
        }

        return visible;
    }

    public static IReadOnlyDictionary<string, string> Errors(ResolvedStep step)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in step.Fields)
        {
            var message = FirstError(field);
            if (message is not null)
            {
                errors[field.Key] = message;
            }
        }

        return errors;
    }

    public static string? FirstError(FieldDefinition field, string? value = null)
    {
        foreach (var rule in field.Rules)
        {
            if (!rule.TryValidate(value, out var message))
            {
                return message;
            }
        }

        return null;
    }

    public static int FirstIncompleteIndex(
        IReadOnlyList<ResolvedStep> steps,
        IReadOnlyDictionary<string, string?> answers)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            foreach (var field in steps[i].Fields)
            {
                answers.TryGetValue(field.Key, out var value);
                if (FirstError(field, value) is not null)
                {
                    return i;
                }
            }
        }

        return steps.Count;
    }
}

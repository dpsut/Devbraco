namespace Devbraco.Journeys;

public static class LayoutValidator
{
    public static IReadOnlyList<string> Validate(
        IJourneyDefinition journey,
        IReadOnlySet<string>? effectiveFieldKeys = null)
    {
        var errors = new List<string>();
        var catalogue = journey.Fields.ToDictionary(field => field.Key);
        var effective = effectiveFieldKeys ?? catalogue.Keys.ToHashSet();

        foreach (var key in effective)
        {
            if (!catalogue.ContainsKey(key))
            {
                errors.Add($"Effective field '{key}' is not in the {journey.Slug} catalogue.");
            }
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var step in journey.Layout.Steps)
        {
            foreach (var key in step.FieldKeys)
            {
                if (!catalogue.TryGetValue(key, out _))
                {
                    errors.Add($"Layout step '{step.Slug}' references unknown field '{key}'.");
                    continue;
                }

                if (!effective.Contains(key))
                {
                    errors.Add($"Layout step '{step.Slug}' references '{key}', which this client's capabilities omit.");
                    continue;
                }

                if (!seen.Add(key))
                {
                    errors.Add($"Field '{key}' appears more than once in the layout.");
                }
            }
        }

        foreach (var key in effective)
        {
            if (!seen.Contains(key))
            {
                errors.Add($"Field '{key}' is missing from the layout.");
            }
        }

        var position = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var step in journey.Layout.Steps)
        {
            foreach (var key in step.FieldKeys)
            {
                position[key] = index;
            }

            index++;
        }

        foreach (var field in journey.Fields.Where(field => effective.Contains(field.Key)))
        {
            if (!position.TryGetValue(field.Key, out var fieldStep))
            {
                continue;
            }

            foreach (var dependency in field.IncludeWhen.DependsOn)
            {
                if (!position.TryGetValue(dependency, out var dependencyStep))
                {
                    errors.Add($"Field '{field.Key}' depends on '{dependency}', which is not in the layout.");
                    continue;
                }

                if (dependencyStep > fieldStep)
                {
                    errors.Add($"Field '{field.Key}' depends on '{dependency}', which appears in a later step.");
                }
            }
        }

        return errors;
    }
}

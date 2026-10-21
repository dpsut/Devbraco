namespace Devbraco.Journeys;

/// <summary>
/// Sends a completed journey to whatever system owns the outcome. One per journey slug.
/// Mapping answers onto an API contract belongs here, not in the renderer.
/// </summary>
public interface IJourneySubmissionHandler
{
    string JourneySlug { get; }

    Task<string> SubmitAsync(
        IReadOnlyDictionary<string, string?> answers,
        JourneyContext context,
        string idempotencyKey,
        CancellationToken cancellationToken);
}

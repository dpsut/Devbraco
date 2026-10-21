namespace Devbraco.Journeys.Membership;

/// <summary>
/// The port onto the external renewals API. The host supplies the implementation.
/// </summary>
public interface IRenewalClient
{
    Task<string> SubmitAsync(RenewalSubmission submission, string idempotencyKey, CancellationToken cancellationToken);
}

public sealed class RenewalSubmissionHandler : IJourneySubmissionHandler
{
    private readonly IRenewalClient _client;

    public RenewalSubmissionHandler(IRenewalClient client)
    {
        _client = client;
    }

    public string JourneySlug => "renewal";

    public Task<string> SubmitAsync(
        IReadOnlyDictionary<string, string?> answers,
        JourneyContext context,
        string idempotencyKey,
        CancellationToken cancellationToken) =>
        _client.SubmitAsync(RenewalMapper.Map(answers, context), idempotencyKey, cancellationToken);
}

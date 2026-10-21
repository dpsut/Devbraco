using System.Collections.Concurrent;
using Devbraco.Journeys.Membership;

namespace Devbraco.Web.Journeys;

/// <summary>
/// Development stand-in for the external API. The same store backs the HTTP stub.
/// </summary>
public sealed class RenewalSubmissionStore
{
    private readonly ConcurrentDictionary<string, string> _ids = new(StringComparer.Ordinal);

    public string Accept(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
        }

        return _ids.GetOrAdd(idempotencyKey, _ => Guid.NewGuid().ToString("N")[..12]);
    }
}

public sealed class LocalRenewalClient : IRenewalClient
{
    private readonly RenewalSubmissionStore _store;

    public LocalRenewalClient(RenewalSubmissionStore store)
    {
        _store = store;
    }

    public Task<string> SubmitAsync(RenewalSubmission submission, string idempotencyKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(submission);
        return Task.FromResult(_store.Accept(idempotencyKey));
    }
}

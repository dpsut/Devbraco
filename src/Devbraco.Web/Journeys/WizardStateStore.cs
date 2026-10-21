using System.Security.Cryptography;
using System.Text.Json;
using Devbraco.Journeys;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

namespace Devbraco.Web.Journeys;

public sealed class WizardState
{
    public string JourneySlug { get; set; } = "";

    public string LayoutVersion { get; set; } = "";

    public Dictionary<string, string?> Answers { get; set; } = new(StringComparer.Ordinal);

    public DateTimeOffset StartedAt { get; set; }

    public string IdempotencyKey { get; set; } = "";

    public bool Submitted { get; set; }

    public string? ConfirmationId { get; set; }
}

public interface IWizardStateStore
{
    Task<WizardState?> ReadAsync();

    Task SaveAsync(WizardState state);

    Task ClearAsync();
}

public sealed class WizardStateStore : IWizardStateStore
{
    public const string CookieName = "Devbraco.Wizard";
    private static readonly TimeSpan SlidingLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan HardLifetime = TimeSpan.FromHours(2);

    private readonly IHttpContextAccessor _http;
    private readonly IDistributedCache _cache;
    private readonly IDataProtector _protector;
    private readonly ILogger<WizardStateStore> _logger;

    public WizardStateStore(
        IHttpContextAccessor http,
        IDistributedCache cache,
        IDataProtectionProvider dataProtection,
        ILogger<WizardStateStore> logger)
    {
        _http = http;
        _cache = cache;
        _protector = dataProtection.CreateProtector("Devbraco.WizardState.v1");
        _logger = logger;
    }

    public async Task<WizardState?> ReadAsync()
    {
        var id = SessionId();
        if (id is null)
        {
            return null;
        }

        var protectedBytes = await _cache.GetAsync(CacheKey(id));
        if (protectedBytes is null)
        {
            return null;
        }

        WizardState? state;
        try
        {
            var json = _protector.Unprotect(protectedBytes);
            state = JsonSerializer.Deserialize<WizardState>(json);
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            _logger.LogWarning("Discarding a wizard session that could not be read.");
            return null;
        }

        if (state is null || DateTimeOffset.UtcNow - state.StartedAt > HardLifetime)
        {
            await _cache.RemoveAsync(CacheKey(id));
            return null;
        }

        return state;
    }

    public async Task SaveAsync(WizardState state)
    {
        var id = SessionId() ?? NewSession();
        if (DateTimeOffset.UtcNow - state.StartedAt > HardLifetime)
        {
            throw new InvalidOperationException("This wizard session has expired.");
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(state);
        var protectedBytes = _protector.Protect(json);
        await _cache.SetAsync(CacheKey(id), protectedBytes, new DistributedCacheEntryOptions
        {
            SlidingExpiration = SlidingLifetime
        });

        _logger.LogInformation("Stored wizard state for {Journey}", state.JourneySlug);
    }

    public async Task ClearAsync()
    {
        var id = SessionId();
        if (id is null)
        {
            return;
        }

        await _cache.RemoveAsync(CacheKey(id));
        _http.HttpContext?.Response.Cookies.Delete(CookieName);
    }

    private string? SessionId() => _http.HttpContext?.Request.Cookies[CookieName];

    private string NewSession()
    {
        var id = Guid.NewGuid().ToString("N");
        _http.HttpContext!.Response.Cookies.Append(CookieName, id, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = _http.HttpContext.Request.IsHttps,
            MaxAge = HardLifetime
        });
        return id;
    }

    private static string CacheKey(string id) => "devbraco-wizard-" + id;
}

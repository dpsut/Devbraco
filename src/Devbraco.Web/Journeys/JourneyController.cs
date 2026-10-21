using System.Security.Claims;
using Devbraco.Journeys;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace Devbraco.Web.Journeys;

public sealed class JourneyController : Controller
{
    private readonly IJourneyCopyProvider _copy;
    private readonly ICopyTrace _trace;
    private readonly IWizardStateStore _stateStore;
    private readonly IRenewalClient _renewals;
    private readonly IWebHostEnvironment _environment;

    public JourneyController(
        IJourneyCopyProvider copy,
        ICopyTrace trace,
        IWizardStateStore stateStore,
        IRenewalClient renewals,
        IWebHostEnvironment environment)
    {
        _copy = copy;
        _trace = trace;
        _stateStore = stateStore;
        _renewals = renewals;
        _environment = environment;
    }

    [HttpGet("/go/{journeySlug}")]
    public async Task<IActionResult> Start(string journeySlug)
    {
        var journey = JourneyCatalog.Find(journeySlug);
        if (journey is null)
        {
            return NotFound();
        }

        var state = await CurrentState(journey);
        var steps = Visible(journey, state);
        var index = Math.Min(JourneyFlow.FirstIncompleteIndex(steps, state.Answers), Math.Max(steps.Count - 1, 0));
        return RedirectToAction(nameof(Step), new { journeySlug = journey.Slug, stepSlug = steps[index].Slug });
    }

    [HttpGet("/go/{journeySlug}/{stepSlug}")]
    public async Task<IActionResult> Step(string journeySlug, string stepSlug)
    {
        var journey = JourneyCatalog.Find(journeySlug);
        if (journey is null)
        {
            return NotFound();
        }

        var state = await CurrentState(journey);
        if (state.Submitted && state.ConfirmationId is not null)
        {
            return RedirectToAction(nameof(Done), new { journeySlug, id = state.ConfirmationId });
        }

        var steps = Visible(journey, state);
        var requested = steps.ToList().FindIndex(step => step.Slug.Equals(stepSlug, StringComparison.OrdinalIgnoreCase));
        if (requested < 0)
        {
            return RedirectToAction(nameof(Start), new { journeySlug });
        }

        var incomplete = JourneyFlow.FirstIncompleteIndex(steps, state.Answers);
        if (requested > incomplete)
        {
            return RedirectToAction(nameof(Step), new { journeySlug, stepSlug = steps[Math.Min(incomplete, steps.Count - 1)].Slug });
        }

        return View("Step", BuildModel(journey, steps, requested, state, errors: null));
    }

    [HttpPost("/go/{journeySlug}/{stepSlug}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Step(string journeySlug, string stepSlug, IFormCollection form)
    {
        var journey = JourneyCatalog.Find(journeySlug);
        if (journey is null)
        {
            return NotFound();
        }

        var state = await CurrentState(journey);
        var steps = Visible(journey, state);
        var requested = steps.ToList().FindIndex(step => step.Slug.Equals(stepSlug, StringComparison.OrdinalIgnoreCase));
        if (requested < 0)
        {
            return RedirectToAction(nameof(Start), new { journeySlug });
        }

        var incomplete = JourneyFlow.FirstIncompleteIndex(steps, state.Answers);
        if (requested > incomplete)
        {
            return RedirectToAction(nameof(Step), new { journeySlug, stepSlug = steps[incomplete].Slug });
        }

        var step = steps[requested];
        foreach (var field in step.Fields)
        {
            state.Answers[field.Key] = form[field.Key].ToString();
        }

        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in step.Fields)
        {
            state.Answers.TryGetValue(field.Key, out var value);
            var message = JourneyFlow.FirstError(field, value);
            if (message is not null)
            {
                errors[field.Key] = _copy.ValidationMessage(field, message);
            }
        }

        if (errors.Count > 0)
        {
            await _stateStore.SaveAsync(state);
            return View("Step", BuildModel(journey, steps, requested, state, errors));
        }

        await _stateStore.SaveAsync(state);

        if (requested < steps.Count - 1)
        {
            return RedirectToAction(nameof(Step), new { journeySlug, stepSlug = steps[requested + 1].Slug });
        }

        var context = ContextFor(state);
        var submission = RenewalMapper.Map(state.Answers, context);
        var confirmation = await _renewals.SubmitAsync(submission, state.IdempotencyKey, HttpContext.RequestAborted);
        await _stateStore.ClearAsync();
        return RedirectToAction(nameof(Done), new { journeySlug, id = confirmation });
    }

    [HttpGet("/go/{journeySlug}/done")]
    public IActionResult Done(string journeySlug, string id)
    {
        if (JourneyCatalog.Find(journeySlug) is null || string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        ViewData["JourneySlug"] = journeySlug;
        ViewData["ConfirmationId"] = id;
        return View();
    }

    private async Task<WizardState> CurrentState(IJourneyDefinition journey)
    {
        var state = await _stateStore.ReadAsync();
        if (state is null || !state.JourneySlug.Equals(journey.Slug, StringComparison.OrdinalIgnoreCase) || state.Submitted)
        {
            state = new WizardState
            {
                JourneySlug = journey.Slug,
                LayoutVersion = journey.Layout.Version,
                StartedAt = DateTimeOffset.UtcNow,
                IdempotencyKey = Guid.NewGuid().ToString("N")
            };
            await _stateStore.SaveAsync(state);
        }

        return state;
    }

    private IReadOnlyList<ResolvedStep> Visible(IJourneyDefinition journey, WizardState state) =>
        JourneyFlow.VisibleSteps(journey, ContextFor(state));

    private JourneyContext ContextFor(WizardState state) => new()
    {
        Semantics = CurrentSemantics(),
        Answers = state.Answers
    };

    private IReadOnlySet<MemberSemantic> CurrentSemantics()
    {
        if (!_environment.IsDevelopment())
        {
            return DevMember.DefaultSemantics;
        }

        var result = HttpContext.AuthenticateAsync(DevMember.Scheme).GetAwaiter().GetResult();
        return DevMember.Read(result.Principal);
    }

    private JourneyStepViewModel BuildModel(
        IJourneyDefinition journey,
        IReadOnlyList<ResolvedStep> steps,
        int index,
        WizardState state,
        IReadOnlyDictionary<string, string>? errors)
    {
        var step = steps[index];
        return new JourneyStepViewModel
        {
            JourneySlug = journey.Slug,
            JourneyTitle = _copy.JourneyTitle(journey),
            StepSlug = step.Slug,
            StepTitle = _copy.StepTitle(step),
            Intro = _copy.StepIntro(step),
            StepNumber = index + 1,
            StepCount = steps.Count,
            Semantics = CurrentSemantics().OrderBy(semantic => semantic.ToString()).ToArray(),
            FallbackKeys = _trace.Keys.OrderBy(key => key).ToArray(),
            Fields = step.Fields.Select(field =>
            {
                state.Answers.TryGetValue(field.Key, out var value);
                return new FieldViewModel
                {
                    Key = field.Key,
                    Type = field.Type,
                    Label = _copy.Label(field),
                    Help = _copy.Help(field),
                    Placeholder = _copy.Placeholder(field),
                    Value = value,
                    Error = errors is not null && errors.TryGetValue(field.Key, out var error) ? error : null,
                    Sensitive = field.Sensitive
                };
            }).ToArray()
        };
    }
}

public sealed class DevMemberController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DevMemberController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("/dev/member")]
    public async Task<IActionResult> SignIn(string? semantics, string? returnUrl)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var selected = (semantics ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Enum.TryParse<MemberSemantic>(value, out var semantic) ? semantic : (MemberSemantic?)null)
            .Where(semantic => semantic is not null)
            .Select(semantic => semantic!.Value)
            .ToArray();

        if (selected.Length == 0)
        {
            selected = DevMember.DefaultSemantics.ToArray();
        }

        var claims = selected.Select(semantic => new Claim(DevMember.ClaimType, semantic.ToString()));
        var identity = new ClaimsIdentity(claims, DevMember.Scheme);
        await HttpContext.SignInAsync(DevMember.Scheme, new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Redirect("/go/renewal");
    }
}

public sealed class RenewalStubController : ControllerBase
{
    private readonly RenewalSubmissionStore _store;
    private readonly IWebHostEnvironment _environment;

    public RenewalStubController(RenewalSubmissionStore store, IWebHostEnvironment environment)
    {
        _store = store;
        _environment = environment;
    }

    [HttpPost("/dev/api/renewals")]
    public IActionResult Submit([FromBody] RenewalStubRequest request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var id = _store.Accept(request.IdempotencyKey);
        return Ok(new { id });
    }
}

public sealed class RenewalStubRequest
{
    public string IdempotencyKey { get; set; } = "";
}

using System.Security.Claims;
using Devbraco.Journeys;

namespace Devbraco.Web.Journeys;

public static class DevMember
{
    public const string Scheme = "Devbraco.Member";
    public const string ClaimType = "devbraco-semantic";

    public static IReadOnlySet<MemberSemantic> DefaultSemantics { get; } =
        new HashSet<MemberSemantic> { MemberSemantic.IsActive, MemberSemantic.CanRenew };

    public static IReadOnlySet<MemberSemantic> Read(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return DefaultSemantics;
        }

        var values = principal.FindAll(ClaimType)
            .Select(claim => Enum.TryParse<MemberSemantic>(claim.Value, out var semantic) ? semantic : (MemberSemantic?)null)
            .Where(semantic => semantic is not null)
            .Select(semantic => semantic!.Value)
            .ToHashSet();

        return values.Count == 0 ? DefaultSemantics : values;
    }
}

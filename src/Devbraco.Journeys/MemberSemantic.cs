namespace Devbraco.Journeys;

/// <summary>
/// Meanings journey logic is allowed to branch on. Raw client statuses never appear here.
/// </summary>
public enum MemberSemantic
{
    IsActive,
    IsPending,
    IsLapsed,
    CanRenew,
    RequiresPaymentPrompt
}

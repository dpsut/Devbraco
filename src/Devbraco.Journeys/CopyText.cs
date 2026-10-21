namespace Devbraco.Journeys;

public static class CopyText
{
    /// <summary>
    /// Blank CMS copy means inherit the code default, resolved at read time.
    /// </summary>
    public static string Resolve(string? cmsValue, string codeDefault) =>
        string.IsNullOrWhiteSpace(cmsValue) ? codeDefault : cmsValue;
}

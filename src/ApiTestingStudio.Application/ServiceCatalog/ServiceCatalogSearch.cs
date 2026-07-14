namespace ApiTestingStudio.Application.ServiceCatalog;

/// <summary>
/// Pure, case-insensitive matcher used to filter the Service Explorer tree client-side. Kept here
/// (rather than in the view model) so the matching rule is unit-testable and consistent.
/// </summary>
public static class ServiceCatalogSearch
{
    /// <summary>
    /// True when <paramref name="query"/> is empty/whitespace (match-all) or is contained in
    /// <paramref name="text"/> ignoring case.
    /// </summary>
    public static bool Matches(string? text, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return text is not null
            && text.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}

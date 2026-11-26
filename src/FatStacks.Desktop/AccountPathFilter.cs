namespace FatStacks.Desktop;

internal static class AccountPathFilter
{
    public static IEnumerable<T> Matches<T>(IEnumerable<T> items, Func<T, string> path, string query) =>
        items.Where(item => path(item).Contains(query, StringComparison.OrdinalIgnoreCase));
}

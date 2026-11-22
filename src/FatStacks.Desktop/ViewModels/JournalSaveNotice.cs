using FatStacks.Core.Accounts;
using FatStacks.Core.Journal;
namespace FatStacks.Desktop.ViewModels;

internal static class JournalSaveNotice
{
    public static string Describe(JournalSaveResult saved, IReadOnlyList<AccountRecord> accounts)
    {
        if (saved.ResetLineIds.Count == 0) return "Entry saved.";
        var reset = saved.ResetLineIds.ToHashSet();
        var labels = saved.Entry.Lines.Where(x => x.LineId is { } id && reset.Contains(id)).OrderBy(x => x.Order)
            .Select(line =>
            {
                var account = accounts.FirstOrDefault(x => x.AccountId == line.AccountId);
                var path = account is null ? "Unavailable account" : AccountCatalog.Path(account, accounts);
                return $"Line {line.Order} ({path})";
            });
        return $"Entry saved. Reset to Uncleared: {string.Join("; ", labels)}.";
    }
}

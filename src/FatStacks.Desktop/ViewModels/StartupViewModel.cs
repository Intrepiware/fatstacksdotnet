using FatStacks.Core.Connections;
using FatStacks.Core.Contracts;
namespace FatStacks.Desktop.ViewModels;

public sealed class StartupViewModel
{
    public required IProfileStore Store
    {
        private get; init;
    }
    public required BookSessionCoordinator Sessions
    {
        private get; init;
    }
    public async Task<OperationResult<BookSession>?> OpenLastAsync(CancellationToken token)
    {
        try
        {
            var saved = await Store.LoadAsync(token);
            if (!saved.IsSuccess || saved.Value is null)
                return OperationResult<BookSession>.Fail(saved.Message);
            var last = saved.Value.Profiles.SingleOrDefault(x => x.ProfileId == saved.Value.LastSuccessfulProfileId);
            if (last is null)
                return null;
            return await Sessions.SwitchAsync(last, () => Task.FromResult(UnsavedDecision.Discard), () => Task.FromResult(true), token);
        }
        catch (OperationCanceledException) { return OperationResult<BookSession>.Fail("Startup connection cancelled.", Outcome.Cancelled); }
    }
}

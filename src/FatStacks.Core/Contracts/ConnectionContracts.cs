using FatStacks.Core.Connections;
namespace FatStacks.Core.Contracts;

public interface IProfileStore
{
    Task<OperationResult<ProfileConfiguration>> LoadAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<bool>> SaveAsync(ProfileConfiguration configuration, CancellationToken cancellationToken = default);
}
public interface IBookConnectionService
{
    Task<OperationResult<BookInfo>> TestAsync(ConnectionSettings settings, CancellationToken cancellationToken = default);
    Task<OperationResult<BookInfo>> OpenAsync(ConnectionProfile profile, CancellationToken cancellationToken = default);
}
public interface IDatabaseSetup
{
    Task<OperationResult<BookInfo>> InitializeAsync(ConnectionSettings maintenance, BookSetup book, CancellationToken cancellationToken = default);
}
public interface ISecretProtector
{
    string Protect(string value);
    string Unprotect(string value);
}

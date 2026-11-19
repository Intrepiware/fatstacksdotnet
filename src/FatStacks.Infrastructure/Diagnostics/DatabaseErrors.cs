using FatStacks.Core.Contracts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace FatStacks.Infrastructure.Diagnostics;

public static class DatabaseErrors
{
    public static OperationResult<T> Result<T>(Exception error) => error switch
    {
        OperationCanceledException => OperationResult<T>.Fail("Operation cancelled.", Outcome.Cancelled),
        DbUpdateConcurrencyException => OperationResult<T>.Fail("The record changed. Reload it before saving.", Outcome.Conflict),
        SqlException { Number: 18456 } => OperationResult<T>.Fail("SQL sign-in failed. Check the selected authentication method and credentials."),
        SqlException { Number: 229 } => OperationResult<T>.Fail("SQL permission was denied. Check the runtime role or maintenance identity."),
        SqlException { Number: -2 } => OperationResult<T>.Fail("SQL did not respond in time. Check the server connection."),
        _ => OperationResult<T>.Fail("Database access failed. Check the server, database, permissions, and connection settings.")
    };
}

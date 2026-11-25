namespace FatStacks.Core.Contracts;

public record FieldError(string Field, string Message, int? LineOrder = null);
public enum Outcome { Success, ValidationFailure, Conflict, Failed, Cancelled, IdentityMismatch }
public sealed record OperationResult<T>(Outcome Outcome, T? Value = default, string Message = "", IReadOnlyList<string>? Warnings = null, IReadOnlyList<FieldError>? Errors = null)
{
    public bool IsSuccess => Outcome == Outcome.Success;
    public static OperationResult<T> Success(T value, IReadOnlyList<string>? warnings = null) => new(Outcome.Success, value, Warnings: warnings);
    public static OperationResult<T> Fail(string message, Outcome outcome = Outcome.Failed) => new(outcome, Message: message);
}

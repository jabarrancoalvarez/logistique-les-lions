namespace LogistiqueLesLions.Application.Common.Models;

/// <summary>
/// Encapsula el resultado de una operación de negocio.
/// Evita usar excepciones para flujos de negocio esperados.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public IEnumerable<string> Errors { get; }

    private Result(bool isSuccess, T? value, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error, [error]);
    public static Result<T> Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new(false, default, errorList.FirstOrDefault(), errorList);
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error ?? "Unknown error");
}

/// <summary>Resultado sin valor de retorno (operaciones void).</summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public IEnumerable<string> Errors { get; }

    private Result(bool isSuccess, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error, [error]);
    public static Result Failure(IEnumerable<string> errors)
    {
        var errorList = errors.ToList();
        return new(false, errorList.FirstOrDefault(), errorList);
    }
}

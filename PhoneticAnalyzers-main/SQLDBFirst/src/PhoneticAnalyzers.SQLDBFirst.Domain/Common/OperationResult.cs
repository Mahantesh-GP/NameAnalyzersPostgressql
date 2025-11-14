namespace PhoneticAnalyzers.SQLDBFirst.Domain.Common;

/// <summary>
/// Generic operation result with success/failure status and messages.
/// </summary>
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public List<string> Messages { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();

    public static OperationResult<T> Success(T data, params string[] messages)
    {
        return new OperationResult<T>
        {
            IsSuccess = true,
            Data = data,
            Messages = messages.ToList()
        };
    }

    public static OperationResult<T> Failure(params string[] errors)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            Errors = errors.ToList()
        };
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public void AddMessage(string message)
    {
        Messages.Add(message);
    }
}

namespace BillingSys.Shared.Models;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ServiceResult<T> Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public class ServiceResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static ServiceResult Ok() => new() { Success = true };
    public static ServiceResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}

public class BatchResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public bool HasErrors => FailureCount > 0;
}

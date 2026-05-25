namespace EmployeeDirectory.Models;

/// <summary>
/// Standard JSON contract for admin AJAX endpoints.
/// </summary>
public class AdminJsonResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }

    /// <summary>
    /// Creates a successful response payload.
    /// </summary>
    public static AdminJsonResponse Ok(string message, object? data = null)
    {
        return new AdminJsonResponse { Success = true, Message = message, Data = data };
    }

    /// <summary>
    /// Creates an error response payload.
    /// </summary>
    public static AdminJsonResponse Fail(string message, object? data = null)
    {
        return new AdminJsonResponse { Success = false, Message = message, Data = data };
    }
}

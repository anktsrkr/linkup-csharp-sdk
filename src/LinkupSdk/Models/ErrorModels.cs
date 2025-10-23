using System.Text.Json.Serialization;

namespace LinkupSdk.Models;

/// <summary>
/// Model for individual error details in validation errors
/// </summary>
public class ErrorDetail
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Model for API error responses from Linkup API
/// </summary>
public class LinkupApiError
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("details")]
    public List<ErrorDetail> Details { get; set; } = [];
}

/// <summary>
/// Complete error response structure from Linkup API
/// </summary>
public class LinkupErrorResponse
{
    [JsonPropertyName("error")]
    public LinkupApiError Error { get; set; } = new();

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }
}

/// <summary>
/// Custom exception for Linkup API errors with structured information for LLM understanding
/// </summary>
public class LinkupException : Exception
{
    /// <summary>
    /// HTTP status code from the API response
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Error code from the API (e.g., "UNAUTHORIZED", "NOT_FOUND")
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Additional error details from the API
    /// </summary>
    public List<ErrorDetail> ErrorDetails { get; }

    /// <summary>
    /// Suggested recovery action based on the error type
    /// </summary>
    public string? RecoverySuggestion { get; }

    /// <summary>
    /// Creates a LinkupApiException from an API error response
    /// </summary>
    public LinkupException(LinkupErrorResponse errorResponse)
        : base(errorResponse.Error.Message)
    {
        StatusCode = errorResponse.StatusCode;
        ErrorCode = errorResponse.Error.Code;
        ErrorDetails = errorResponse.Error.Details;
        RecoverySuggestion = GetRecoverySuggestion(errorResponse);
    }

    /// <summary>
    /// Creates a LinkupApiException for cases where error response couldn't be parsed
    /// </summary>
    public LinkupException(int statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = "UNKNOWN";
        ErrorDetails = [];
        RecoverySuggestion = GetRecoverySuggestion(statusCode);
    }

    private static string? GetRecoverySuggestion(LinkupErrorResponse error)
    {
        return error.Error.Code switch
        {
            "UNAUTHORIZED" => "Check your API key and ensure it has the required permissions",
            "NOT_FOUND" => "Verify the endpoint URL and parameters are correct",
            "BAD_REQUEST" => "Review the request parameters and ensure they are valid",
            "RATE_LIMITED" => "Wait before retrying or check your usage limits",
            "INTERNAL_SERVER_ERROR" => "The API is experiencing issues, try again later",
            "VALIDATION_ERROR" => $"Please fix these issues : {string.Join(" and ", error.Error.Details.Select(d => d.Message))}.",
            _ => null
        };
    }

    private static string? GetRecoverySuggestion(int statusCode)
    {
        return statusCode switch
        {
            401 => "Check your API key and ensure it has the required permissions",
            403 => "Your API key doesn't have permission for this operation",
            404 => "Verify the endpoint URL and parameters are correct",
            400 => "Review the request parameters and ensure they are valid",
            429 => "Wait before retrying or check your usage limits",
            500 => "The API is experiencing issues, try again later",
            502 or 503 or 504 => "The API is temporarily unavailable, try again later",
            _ => null
        };
    }
}
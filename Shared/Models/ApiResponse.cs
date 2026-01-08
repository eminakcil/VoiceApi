namespace VoiceApi.Shared.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    public ApiResponse() { }

    public ApiResponse(T data, string message = "")
    {
        Success = true;
        Data = data;
        Message = message;
    }

    public ApiResponse(string message)
    {
        Success = true;
        Message = message;
    }

    public static ApiResponse<T> Fail(string errorMessage, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = errorMessage,
            Errors = errors,
        };
    }

    public static ApiResponse<T> Ok(T data, string message = "")
    {
        return new ApiResponse<T>(data, message);
    }
}

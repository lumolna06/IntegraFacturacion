namespace IntegraPro.AppLogic.Utils;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool Result { get; set; }

    public ApiResponse(bool result, string message, T? data = default)
    {
        Result = result;
        Message = message;
        Data = data;
    }
}

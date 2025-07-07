using SmartCache.Application.Common.Enums;

namespace SmartCache.Application.Common.Response
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }

        public ApiResponse(int code, string message, T? data = default)
        {
            Code = code;
            Message = message;
            Data = data;
        }
        public ApiResponse(ResponseCode code, string message, T? data = default)
        {
            Code = (int)code;
            Message = message;
            Data = data;
        }
    }
}
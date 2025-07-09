using SmartCache.Application.Common.Enums;
using System.Text.Json.Serialization;

namespace SmartCache.Application.Common.Response
{
    public class ApiResponse<T>
    {
        public int Code { get; set; }
        public string? Message { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Version { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public T? Data { get; set; }

        public ApiResponse(ResponseCode code, string message, T? data = default, int? version = null)
        {
            Code = (int)code;
            Message = message;
            Data = data;
            Version = version;
        }
        public ApiResponse(ResponseCode code, string message, int version)
            : this(code, message, default, version)
        {
        }

        public ApiResponse(ResponseCode code, string message)
            : this(code, message, default, null)
        {
        }
    }
}
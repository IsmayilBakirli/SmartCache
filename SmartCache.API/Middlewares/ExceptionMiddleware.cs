using SmartCache.Application.Common.Enums;
using SmartCache.Application.Common.Response;
using SmartCache.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace SmartCache.API.Middlewares
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unhandled exception: {ex.Message}");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            ResponseCode statusCode;
            string message;

            if (exception is NotFoundException)
            {
                statusCode = ResponseCode.NotFound;
                message = exception.Message;
            }
            else if(exception is BadRequestException)
            {
                statusCode = ResponseCode.BadRequest;
                message = exception.Message;
            }
            else if (exception is VersionNotModifiedException)
            {
                statusCode = ResponseCode.NotModified;
                message = exception.Message;
            }
            else
            {
                statusCode = ResponseCode.InternalServerError; 
                message = exception.Message;
            }

            var response = new ApiResponse<object>(statusCode, message, null);
            context.Response.StatusCode =(int) statusCode;

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

    }
}

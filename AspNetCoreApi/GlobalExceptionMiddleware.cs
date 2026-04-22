using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text.Json;

namespace AspNetCoreApi
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode status = HttpStatusCode.InternalServerError;
            string message = "An unexpected error occurred.";

            _logger.LogError(exception, "An unhandled exception occurred.");

            if (exception is ArgumentException || exception is InvalidOperationException)
            {
                status = HttpStatusCode.BadRequest;
                message = exception.Message;
            }
            else if (exception is KeyNotFoundException)
            {
                status = HttpStatusCode.NotFound;
                message = exception.Message;
            }

            var problemDetails = new
            {
                type = "about:blank",
                title = ReasonPhrases.GetReasonPhrase((int)status),
                status = (int)status,
                detail = message
            };

            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/problem+json";

            return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}

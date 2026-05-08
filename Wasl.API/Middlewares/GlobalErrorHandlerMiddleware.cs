using Wasl.Application.Common;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Wasl.api.Middlewares
{
    public class GlobalErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalErrorHandlerMiddleware> _logger;

        public GlobalErrorHandlerMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlerMiddleware> logger)
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
                _logger.LogError(ex, "Something went wrong: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);

            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = ApiResponse<string>.Failure(exception.Message);


            switch (exception)
            {
                case KeyNotFoundException e:   
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                case UnauthorizedAccessException e:          
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    break;
                case ArgumentException e:
                    
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case ValidationException validationException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                    var errorMessages = string.Join(" | ", validationException.Errors.Select(e => e.ErrorMessage));
                    response = ApiResponse<string>.Failure(errorMessages);
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "Internal Server Error";
                    break;
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(response, jsonOptions);

            await context.Response.WriteAsync(json);
        }
    }
}
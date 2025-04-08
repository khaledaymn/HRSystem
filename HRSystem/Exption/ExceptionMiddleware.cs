using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // Proceed with the request
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex); // Handle the exception
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log the exception with details
        _logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        var statusCode = HttpStatusCode.InternalServerError; // Default 500
        var message = "An internal server error occurred";

        // Customize the response based on the exception type
        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest; // 400
                message = argEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized; // 401
                message = "You are not authorized to access this resource";
                break;

            case InvalidOperationException invOpEx:
                statusCode = HttpStatusCode.BadRequest; // 400
                message = $"Invalid operation: {invOpEx.Message}";
                break;

            case KeyNotFoundException keyEx:
                statusCode = HttpStatusCode.NotFound; // 404
                message = $"Resource not found: {keyEx.Message}";
                break;

            case FileNotFoundException fileEx:
                statusCode = HttpStatusCode.NotFound; // 404
                message = $"File not found: {fileEx.Message}";
                break;

            case NullReferenceException nullEx:
                statusCode = HttpStatusCode.InternalServerError; // 500
                message = "A null reference error occurred. Please contact support.";
                break;

            case FormatException formatEx:
                statusCode = HttpStatusCode.BadRequest; // 400
                message = $"Invalid format: {formatEx.Message}";
                break;

            case TimeoutException timeoutEx:
                statusCode = HttpStatusCode.RequestTimeout; // 408
                message = $"Request timed out: {timeoutEx.Message}";
                break;

            case NotImplementedException notImplEx:
                statusCode = HttpStatusCode.NotImplemented; // 501
                message = $"This feature is not implemented yet: {notImplEx.Message}";
                break;

            case DbUpdateException dbEx:
                statusCode = HttpStatusCode.InternalServerError; // 500
                message = $"Database update failed: {dbEx.InnerException?.Message ?? dbEx.Message}";
                break;

            case HttpRequestException httpEx:
                statusCode = HttpStatusCode.ServiceUnavailable; // 503
                message = $"External service request failed: {httpEx.Message}";
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError; // 500
                message = "An unexpected error occurred. Please try again later.";
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        // Include stack trace only in development environment
        var result = JsonConvert.SerializeObject(new
        {
            StatusCode = statusCode,
            Message = message,
            Detailed = _env.IsDevelopment() ? exception.StackTrace : null // Hide in production
        });

        return context.Response.WriteAsync(result);
    }
}
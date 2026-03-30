using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;

using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationEx => (HttpStatusCode.BadRequest, validationEx.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Payment not found"),
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.BadRequest =>
               (HttpStatusCode.BadRequest, "Bank rejected the request due to invalid data"),

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.ServiceUnavailable =>
                (HttpStatusCode.ServiceUnavailable, "Bank service is temporarily unavailable"),

            HttpRequestException =>
                (HttpStatusCode.BadGateway, "Bank service is currently unavailable"),

            TaskCanceledException =>
                (HttpStatusCode.GatewayTimeout, "Bank service request timed out"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        _logger.LogError(exception, "Error occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            message,
            statusCode = (int)statusCode,
            status = PaymentStatus.Rejected.ToString(),
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
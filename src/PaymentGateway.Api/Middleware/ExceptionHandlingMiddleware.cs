using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

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
        var (statusCode, title, detail) = exception switch
        {
            ValidationException validationEx => (HttpStatusCode.BadRequest, "Invalid data", validationEx.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Payment not found", "The payment you are looking for does not exist."),
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.BadRequest =>
               (HttpStatusCode.BadRequest, "Bank rejected the request", "The bank rejected the request due to invalid data"),

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.ServiceUnavailable =>
                (HttpStatusCode.ServiceUnavailable, "Bank service unavailable", "The bank service is temporarily unavailable. Please try again later."),

            HttpRequestException =>
                (HttpStatusCode.BadGateway, "Bank service currently unavailable", "The bank service is currently unavailable. Please try again later."),

            TaskCanceledException =>
                (HttpStatusCode.GatewayTimeout, "Bank service request timed out", "The request to the bank service timed out."),
            _ => (HttpStatusCode.InternalServerError, "Internal server error", "An unexpected error occurred on the server.")
        };

        _logger.LogError(exception, "Error occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (exception is ValidationException)
        {
            problemDetails.Extensions["paymentStatus"] = "Rejected";
        }

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
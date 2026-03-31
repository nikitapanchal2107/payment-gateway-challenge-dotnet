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
            ValidationException validationEx =>
                (HttpStatusCode.BadRequest, "Validation Failed", validationEx.Message),

            ArgumentNullException argNullEx =>  // ? Handle null arguments
                (HttpStatusCode.BadRequest, "Invalid Request", $"Required parameter is missing: {argNullEx.ParamName}"),

            NullReferenceException =>  // ? Catch unexpected nulls
                (HttpStatusCode.InternalServerError, "Internal Error", "An unexpected null reference occurred"),

            KeyNotFoundException =>
                (HttpStatusCode.NotFound, "Not Found", "Payment not found"),

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.ServiceUnavailable =>
                (HttpStatusCode.ServiceUnavailable, "Service Unavailable", "Bank service is temporarily unavailable"),

            HttpRequestException =>
                (HttpStatusCode.BadGateway, "Bad Gateway", "Bank service is currently unavailable"),

            TaskCanceledException =>
                (HttpStatusCode.GatewayTimeout, "Gateway Timeout", "Bank service request timed out"),

            _ =>
                (HttpStatusCode.InternalServerError, "Internal Server Error", "An unexpected error occurred")
        };

        _logger.LogError(exception, "Error occurred: {Title} - {Detail}", title, detail);

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (exception is ValidationException)
        {
            problemDetails.Extensions["paymentStatus"] = PaymentStatus.Rejected.ToString();
        }

        context.Response.StatusCode = (int)statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NotesApp.Api.Services;

namespace NotesApp.Api.Middleware
{
    /// <summary>
    /// Catches all unhandled exceptions, logs them safely (no sensitive data,
    /// no stack traces leaked to the client), and returns a unified
    /// RFC 7807 ProblemDetails JSON payload.
    /// </summary>
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
            catch (AuthValidationException ex)
            {
                await WriteProblemAsync(context, HttpStatusCode.BadRequest, "Authentication Error", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized access attempt: {Message}", ex.Message);
                await WriteProblemAsync(context, HttpStatusCode.Unauthorized, "Unauthorized", "Access denied.");
            }
            catch (Exception ex)
            {
                // Log full detail server-side only; never leak internals to the client.
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await WriteProblemAsync(
                    context,
                    HttpStatusCode.InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred. Please try again later.");
            }
        }

        private static async Task WriteProblemAsync(
            HttpContext context, HttpStatusCode statusCode, string title, string detail)
        {
            var problem = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}

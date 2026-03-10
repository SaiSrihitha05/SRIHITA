// InsuranceAPI/Middleware/GlobalExceptionHandler.cs
using Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceAPI.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception,
                "Exception occurred: {Message}", exception.Message);

            var (statusCode, title) = exception switch
            {
                NotFoundException ex => (StatusCodes.Status404NotFound,
                                               "Not Found"),
                BadRequestException ex => (StatusCodes.Status400BadRequest,
                                               "Bad Request"),
                ConflictException ex => (StatusCodes.Status409Conflict,
                                               "Conflict"),
                UnauthorizedException ex => (StatusCodes.Status401Unauthorized,
                                               "Unauthorized"),
                ForbiddenException ex => (StatusCodes.Status403Forbidden,
                                               "Forbidden"),
                _ => (StatusCodes.Status500InternalServerError,
                                               "Internal Server Error")
            };

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception is not Exception { } e ||
                         statusCode == 500
                            ? "An unexpected error occurred."  // hide internals
                            : exception.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
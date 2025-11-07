using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Azure.Functions.Worker.Http;

namespace PhoneticAnalyzers.Functions.Search.Middleware;

/// <summary>
/// Middleware that converts FluentValidation exceptions into Problem Details (RFC 7807) 400 responses.
/// </summary>
public sealed class ValidationExceptionMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException vex)
        {
            var req = await context.GetHttpRequestDataAsync();
            if (req is null)
            {
                throw; // Not an HTTP-triggered function; rethrow
            }

            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.BadRequest;

            var problem = new ProblemDetailsLike
            {
                Title = "Validation failed",
                Status = (int)HttpStatusCode.BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = req.Url?.AbsolutePath ?? string.Empty,
                Errors = vex.Errors
                    .Where(e => e is not null)
                    .Select(e => new ValidationError { Field = e.PropertyName, Message = e.ErrorMessage })
                    .ToList()
            };

            var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await response.WriteStringAsync(JsonSerializer.Serialize(problem, jsonOptions));

            context.GetInvocationResult().Value = response;
        }
    }

    private sealed class ProblemDetailsLike
    {
        public string Title { get; set; } = string.Empty;
        public int Status { get; set; }
        public string Detail { get; set; } = string.Empty;
        public string Instance { get; set; } = string.Empty;
        public List<ValidationError> Errors { get; set; } = new();
    }

    public sealed class ValidationError
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

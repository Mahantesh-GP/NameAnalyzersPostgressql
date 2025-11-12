using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System.Net;

namespace PhoneticAnalyzers.Functions.Ingestion.Middleware;

/// <summary>
/// Middleware to handle CORS for Azure Functions isolated worker
/// </summary>
public class CorsMiddleware : IFunctionsWorkerMiddleware
{
    /// <summary>
    /// Invokes the CORS middleware to handle preflight and add CORS headers
    /// </summary>
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        
        if (requestData != null)
        {
            // Handle preflight OPTIONS request
            if (requestData.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                var response = requestData.CreateResponse(HttpStatusCode.OK);
                AddCorsHeaders(response, requestData);
                context.GetInvocationResult().Value = response;
                return;
            }
        }

        // Continue to next middleware/function
        await next(context);

        // Add CORS headers to the response
        var httpReqData = await context.GetHttpRequestDataAsync();
        if (httpReqData != null)
        {
            var invocationResult = context.GetInvocationResult();
            if (invocationResult?.Value is HttpResponseData responseData)
            {
                AddCorsHeaders(responseData, httpReqData);
            }
        }
    }

    private static void AddCorsHeaders(HttpResponseData response, HttpRequestData request)
    {
        var origin = request.Headers.TryGetValues("Origin", out var originValues) 
            ? originValues.FirstOrDefault() 
            : "*";

        // Allow WebUI origins
        var allowedOrigins = new[] 
        { 
            "http://localhost:5243", 
            "http://localhost:5280", 
            "http://127.0.0.1:5243", 
            "http://127.0.0.1:5280" 
        };

        if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
        {
            response.Headers.Add("Access-Control-Allow-Origin", origin);
            response.Headers.Add("Access-Control-Allow-Credentials", "true");
        }
        else
        {
            response.Headers.Add("Access-Control-Allow-Origin", "*");
        }

        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        response.Headers.Add("Access-Control-Max-Age", "86400");
    }
}

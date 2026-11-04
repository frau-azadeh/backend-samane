using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using samane.Models;
namespace samane.Security
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ApiExceptionMiddleware> logger;
        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                // Host must validate its anti-CSRF token before authenticated cookie mutations.
                // Same-origin fetches additionally require a custom header, forcing CORS preflight
                // cross-origin.
                if (context.Request.Path.StartsWithSegments("/api/personnel-reviews") &&
                    context.Request.Method != "GET" && context.Request.Method != "OPTIONS" &&
                    context.Request.Headers["X-Requested-With"] != "PersonnelReview")
                    throw new ApiException(400, "هدر درخواست نامعتبر است");
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
            }
            catch (Exception e)
            {
                var known = e as ApiException;
                var sql = e as SqlException;
                int status =
                    known?.Status ??
                    (sql != null && (sql.Number == 547 || sql.Number == 2628 || sql.Number == 8152) ? 400
                                                                                                    : 500);
                if (status == 500)
                    logger.LogError(e, "PersonnelReview API failure");
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new {
                    message =
                        known?.Message ??
                        (status == 400
                             ? "داده‌ها با قواعد پایگاه داده سازگار نیستند"
                             : "خطای داخلی سرور"),
                    traceId = context.TraceIdentifier
                }));
            }
        }
    }
}

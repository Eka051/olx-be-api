using System.Net;
using System.Text.Json;
using olx_be_api.Helpers;

namespace API_Manajemen_Barang.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);

                if (!context.Response.HasStarted)
                {
                    if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiErrorResponse
                        {
                            success = false,
                            message = "Belum terautentikasi. Login terlebih dahulu"
                        }));
                    }
                    else if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(JsonSerializer.Serialize(new ApiErrorResponse
                        {
                            success = false,
                            message = "Akses ditolak. Anda tidak memiliki izin untuk mengakses data ini"
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    var errorResponse = new ApiErrorResponse
                    {
                        success = false,
                        message = "Terjadi kesalahan pada server",
                        errors = e.Message
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                }
                _logger.LogError(e, "An error occurred while processing the request");
            }
        }
    }
}
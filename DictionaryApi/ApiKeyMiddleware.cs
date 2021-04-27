using System.Threading.Tasks;
using DictionaryApi.Config;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DictionaryApi
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeaderName = "ApiKey";
        private readonly RequestDelegate _next;
        private readonly SecurityConfig _securityConfig;

        public ApiKeyMiddleware(RequestDelegate next, IOptions<SecurityConfig> securityConfig)
        {
            _next = next;
            _securityConfig = securityConfig.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var keyFromHeader))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Api Key was not provided.");
                return;
            }


            if (!keyFromHeader.Equals(_securityConfig.ApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client.");
                return;
            }

            await _next(context);
        }
    }
}
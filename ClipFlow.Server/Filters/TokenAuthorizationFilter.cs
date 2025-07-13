using ClipFlow.Server.Models;
using ClipFlow.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ClipFlow.Server.Filters
{
    public class TokenAuthorizationFilter : IAuthorizationFilter
    {
        private readonly AppSettings _appSettings;

        public TokenAuthorizationFilter(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("X-Auth-Token", out var token) || 
                !ValidateToken(token))
            {
                var response = ApiResponse<object>.Error(401, "未授权访问，请检查认证令牌");
                context.Result = new UnauthorizedObjectResult(response);
            }
        }
        private bool ValidateToken(string token)
        {
            return !string.IsNullOrEmpty(token) && _appSettings.Tokens.Contains(token);
        }
    }
} 
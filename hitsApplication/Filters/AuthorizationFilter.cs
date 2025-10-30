using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using hitsApplication.AuthServices;

namespace hitsApplication.Filters
{
    public class AuthorizationFilter : IAuthorizationFilter
    {
        private readonly IJwtTokenService _jwtTokenService;

        public AuthorizationFilter(IJwtTokenService jwtTokenService)
        {
            _jwtTokenService = jwtTokenService;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {

            if (context.ActionDescriptor.EndpointMetadata.Any(em => em is Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute))
                return;

            var authorizationHeader = context.HttpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authorizationHeader))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    ErrorMessage = "Authorization header is required"
                });
                return;
            }

            try
            {
                var userId = _jwtTokenService.GetUserIdFromToken(authorizationHeader);
                if (string.IsNullOrEmpty(userId))
                {
                    context.Result = new UnauthorizedObjectResult(new
                    {
                        Success = false,
                        ErrorMessage = "Invalid or expired token"
                    });
                    return;
                }

                context.HttpContext.Items["UserId"] = userId;
            }
            catch (Exception ex)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    ErrorMessage = "Token validation failed"
                });
            }
        }
    }
}
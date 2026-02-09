using hitsApplication.AuthServices;
using hitsApplication.Enums;
using hitsApplication.Filters;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace hitsApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ILogger<CartController> _logger;
        private readonly IBugCaseLoggingService _bugCaseLogger; 

        public CartController(
            ICartService cartService,
            IJwtTokenService jwtTokenService,
            ILogger<CartController> logger,
            IBugCaseLoggingService bugCaseLogger) 
        {
            _cartService = cartService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
            _bugCaseLogger = bugCaseLogger; 
        }

        private string GetUserIdFromHttpContext()
        {
            return HttpContext.Items["UserId"]?.ToString();
        }

        private string GetUserIdForLogging()
        {
            var userId = GetUserIdFromHttpContext();
            return string.IsNullOrEmpty(userId) ? "anonymous" : userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] string basketId = null)
        {
            const string endpoint = "/api/cart";
            var userId = GetUserIdForLogging();

            try
            {
                var result = await _cartService.GetCart(basketId);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 200, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for basket {BasketId}", basketId);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 500, userId);

                return StatusCode(500, new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, [FromHeader] string basketId)
        {
            const string endpoint = "/api/cart/add";
            var userId = GetUserIdForLogging();

            try
            {
                if (!ModelState.IsValid)
                {
                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data"
                    });
                }

                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.AddToCart(basketId, request);

                _bugCaseLogger.LogBackendRequest("POST", endpoint,
                    result.Success ? 200 : 400, userId);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("POST", endpoint, 500, userId);

                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to add item to cart"
                });
            }
        }

        [HttpPost("create-order")]
        [ServiceFilter(typeof(RequireAuthorizationAttribute))]
        public async Task<IActionResult> CreateOrderFromCart(
            [FromBody] CreateOrderRequest request,
            [FromHeader] string basketId)
        {
            const string methodName = nameof(CreateOrderFromCart);
            const string endpoint = "/api/cart/create-order";
            var userId = GetUserIdForLogging();

            try
            {
                var validationResult = ValidateCreateOrderRequest(request, basketId);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("CART CONTROLLER - Validation failed in {Method}: {Error}",
                        methodName, validationResult.ErrorMessage);

                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 400, userId);

                    return BadRequest(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage
                    });
                }

                if (string.IsNullOrEmpty(userId) || userId == "anonymous")
                {
                    _logger.LogWarning("CART CONTROLLER - User ID not found in context for basket {BasketId}", basketId);

                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 401, "anonymous");

                    return Unauthorized(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "User authentication failed"
                    });
                }

                var tokenStatus = await CheckTokenStatusAsync();
                if (tokenStatus == TokenStatus.Expired)
                {
                    _logger.LogWarning("CART CONTROLLER - Token expired for user {UserId}, basket {BasketId}",
                        userId, basketId);

                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 401, userId);

                    return Unauthorized(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Token has expired. Please log in again.",
                        ErrorCode = "TOKEN_EXPIRED"
                    });
                }

                LogOrderCreationDetails(basketId, userId, request);

                var result = await _cartService.CreateOrderFromCart(basketId, userId, request);

                _logger.LogInformation(
                    "CART CONTROLLER - Order creation completed for basket {BasketId}. Success: {Success}, Message: {Message}",
                    basketId, result.Success, result.ErrorMessage ?? "No error");

                _bugCaseLogger.LogBackendRequest("POST", endpoint,
                    result.Success ? 200 : 400, userId);

                return result.Success
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CART CONTROLLER - Unexpected error in {Method} for basket {BasketId}",
                    methodName, basketId);

                _bugCaseLogger.LogBackendRequest("POST", endpoint, 500, userId);

                return StatusCode(500, new OrderCreationResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error when creating order"
                });
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateCreateOrderRequest(CreateOrderRequest request, string basketId)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return (false, $"Invalid request data: {errors}");
            }

            if (string.IsNullOrEmpty(basketId))
            {
                return (false, "Basket ID is required");
            }

            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                return (false, "Phone number is required");
            }

            if (string.IsNullOrEmpty(request.Address))
            {
                return (false, "Address is required");
            }

            if (string.IsNullOrEmpty(request.PaymentMethod))
            {
                return (false, "Payment method is required");
            }

            return (true, null);
        }

        private async Task<TokenStatus> CheckTokenStatusAsync()
        {
            try
            {
                var authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authorizationHeader))
                {
                    return _jwtTokenService.GetTokenStatus(authorizationHeader);
                }
                return TokenStatus.Missing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CART CONTROLLER - Error checking token status");
                return TokenStatus.Invalid;
            }
        }

        private void LogOrderCreationDetails(string basketId, string userId, CreateOrderRequest request)
        {
            _logger.LogInformation(
                "CART CONTROLLER - Order creation started:\n" +
                "  Basket: {BasketId}\n" +
                "  User: {UserId}\n" +
                "  Payment: {PaymentMethod}\n" +
                "  Phone: {PhoneNumber}\n" +
                "  Address: {Address}\n" +
                "  Comment: {Comment}",
                basketId,
                userId,
                request.PaymentMethod,
                request.PhoneNumber,
                request.Address,
                request.Comment ?? "No comment"
            );
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request, [FromHeader] string basketId)
        {
            const string endpoint = "/api/cart/update";
            var userId = GetUserIdForLogging();

            try
            {
                if (!ModelState.IsValid)
                {
                    _bugCaseLogger.LogBackendRequest("PUT", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data"
                    });
                }

                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("PUT", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.UpdateQuantity(basketId, request.DishId.ToString(), request.Quantity);

                _bugCaseLogger.LogBackendRequest("PUT", endpoint,
                    result.Success ? 200 : 400, userId);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("PUT", endpoint, 500, userId);

                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to update quantity"
                });
            }
        }

        [HttpDelete("remove/{dishId}")]
        public async Task<IActionResult> RemoveFromCart(string dishId, [FromHeader] string basketId)
        {
            var endpoint = $"/api/cart/remove/{dishId}";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("DELETE", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.RemoveFromCart(basketId, dishId);

                _bugCaseLogger.LogBackendRequest("DELETE", endpoint,
                    result.Success ? 200 : 400, userId);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("DELETE", endpoint, 500, userId);

                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to remove item from cart"
                });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart([FromHeader] string basketId)
        {
            const string endpoint = "/api/cart/clear";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("DELETE", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.ClearCart(basketId);

                _bugCaseLogger.LogBackendRequest("DELETE", endpoint,
                    result.Success ? 200 : 400, userId);

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("DELETE", endpoint, 500, userId);

                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to clear cart"
                });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetCartSummary([FromHeader] string basketId)
        {
            const string endpoint = "/api/cart/summary";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("GET", endpoint, 400, userId);
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.GetCartSummary(basketId);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 200, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("GET", endpoint, 500, userId);

                return Ok(new CartSummaryResponse { Success = false });
            }
        }

        [HttpGet("check/{dishId}")]
        public async Task<IActionResult> IsInCart(string dishId, [FromHeader] string basketId)
        {
            var endpoint = $"/api/cart/check/{dishId}";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("GET", endpoint, 400, userId);
                    return BadRequest(new { IsInCart = false, Message = "Basket ID is required" });
                }

                var isInCart = await _cartService.IsInCart(basketId, dishId);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 200, userId);

                return Ok(new { IsInCart = isInCart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("GET", endpoint, 500, userId);

                return Ok(new { IsInCart = false });
            }
        }

        [HttpGet("debug-cart")]
        public async Task<IActionResult> DebugCart([FromHeader] string basketId)
        {
            const string endpoint = "/api/cart/debug-cart";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    _bugCaseLogger.LogBackendRequest("GET", endpoint, 400, userId);
                    return BadRequest(new { Success = false, ErrorMessage = "Basket ID is required" });
                }

                var cart = await _cartService.GetCart(basketId);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 200, userId);

                return Ok(new
                {
                    Success = cart.Success,
                    IsEmpty = cart.IsEmpty,
                    HasItems = cart.HasItems,
                    ItemCount = cart.ItemCount,
                    Total = cart.Total,
                    ItemsCount = cart.Items?.Count ?? 0,
                    Items = cart.Items,
                    BasketId = basketId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging cart for basket {BasketId}", basketId);
                _bugCaseLogger.LogBackendRequest("GET", endpoint, 500, userId);

                return StatusCode(500, new { Success = false, ErrorMessage = ex.Message });
            }
        }

        [HttpPost("test-parse")]
        public IActionResult TestTokenParse([FromBody] TokenTestRequest request)
        {
            const string endpoint = "/api/cart/test-parse";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    _bugCaseLogger.LogBackendRequest("POST", endpoint, 400, userId);
                    return BadRequest(new { Error = "Token is required" });
                }

                var userIdFromToken = _jwtTokenService.GetUserIdFromToken(request.Token);
                var allClaims = _jwtTokenService.GetAllClaims(request.Token);
                var isValid = _jwtTokenService.IsTokenValid(request.Token);

                _bugCaseLogger.LogBackendRequest("POST", endpoint, 200, userId);

                return Ok(new
                {
                    IsValid = isValid,
                    UserId = userIdFromToken,
                    AllClaims = allClaims,
                    Message = "Token parsed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token parsing");
                _bugCaseLogger.LogBackendRequest("POST", endpoint, 500, userId);

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("debug-token")]
        public IActionResult DebugToken([FromHeader] string authorization)
        {
            const string endpoint = "/api/cart/debug-token";
            var userId = GetUserIdForLogging();

            try
            {
                if (string.IsNullOrEmpty(authorization))
                {
                    _bugCaseLogger.LogBackendRequest("GET", endpoint, 400, userId);
                    return BadRequest(new { Error = "Authorization header is required" });
                }

                var userIdFromToken = _jwtTokenService.GetUserIdFromToken(authorization);
                var allClaims = _jwtTokenService.GetAllClaims(authorization);
                var isValid = _jwtTokenService.IsTokenValid(authorization);

                _bugCaseLogger.LogBackendRequest("GET", endpoint, 200, userId);

                return Ok(new
                {
                    IsValid = isValid,
                    UserId = userIdFromToken,
                    AllClaims = allClaims,
                    RawToken = authorization
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token debugging");
                _bugCaseLogger.LogBackendRequest("GET", endpoint, 500, userId);

                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("test-logging")]
        public IActionResult TestLogging()
        {
            const string endpoint = "/api/cart/test-logging";
            var userId = GetUserIdForLogging();

            _bugCaseLogger.LogBackendRequest("POST", endpoint, 200, userId);
            _bugCaseLogger.LogBackendRequest("POST", endpoint, 400, userId);
            _bugCaseLogger.LogBackendRequest("POST", endpoint, 500, userId);

            return Ok(new
            {
                Success = true,
                Message = "Test log entries created",
                UserId = userId,
                LogFilePath = "logs/backend_bugcase.log"
            });
        }
    }
}
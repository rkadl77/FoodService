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

        public CartController(
            ICartService cartService,
            IJwtTokenService jwtTokenService,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
        }

        private string GetUserIdFromHttpContext()
        {
            return HttpContext.Items["UserId"]?.ToString();
        }

        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] string basketId = null)
        {
            try
            {
                var result = await _cartService.GetCart(basketId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for basket {BasketId}", basketId);
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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data"
                    });
                }

                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.AddToCart(basketId, request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for basket {BasketId}", basketId);
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

            try
            {
                var validationResult = ValidateCreateOrderRequest(request, basketId);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("CART CONTROLLER - Validation failed in {Method}: {Error}",
                        methodName, validationResult.ErrorMessage);
                    return BadRequest(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = validationResult.ErrorMessage
                    });
                }

                var userId = GetUserIdFromHttpContext();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("CART CONTROLLER - User ID not found in context for basket {BasketId}", basketId);
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

                return result.Success
                    ? Ok(result)
                    : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CART CONTROLLER - Unexpected error in {Method} for basket {BasketId}",
                    methodName, basketId);

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
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data"
                    });
                }

                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.UpdateQuantity(basketId, request.DishId.ToString(), request.Quantity);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for basket {BasketId}", basketId);
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
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.RemoveFromCart(basketId, dishId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for basket {BasketId}", basketId);
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
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.ClearCart(basketId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for basket {BasketId}", basketId);
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
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var result = await _cartService.GetCartSummary(basketId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for basket {BasketId}", basketId);
                return Ok(new CartSummaryResponse { Success = false });
            }
        }

        [HttpGet("check/{dishId}")]
        public async Task<IActionResult> IsInCart(string dishId, [FromHeader] string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new { IsInCart = false, Message = "Basket ID is required" });
                }

                var isInCart = await _cartService.IsInCart(basketId, dishId);
                return Ok(new { IsInCart = isInCart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart for basket {BasketId}", basketId);
                return Ok(new { IsInCart = false });
            }
        }

        [HttpGet("debug-cart")]
        public async Task<IActionResult> DebugCart([FromHeader] string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new { Success = false, ErrorMessage = "Basket ID is required" });
                }

                var cart = await _cartService.GetCart(basketId);

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
                return StatusCode(500, new { Success = false, ErrorMessage = ex.Message });
            }
        }

        [HttpPost("test-parse")]
        public IActionResult TestTokenParse([FromBody] TokenTestRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { Error = "Token is required" });
                }

                var userId = _jwtTokenService.GetUserIdFromToken(request.Token);
                var allClaims = _jwtTokenService.GetAllClaims(request.Token);
                var isValid = _jwtTokenService.IsTokenValid(request.Token);

                return Ok(new
                {
                    IsValid = isValid,
                    UserId = userId,
                    AllClaims = allClaims,
                    Message = "Token parsed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token parsing");
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("debug-token")]
        public IActionResult DebugToken([FromHeader] string authorization)
        {
            try
            {
                if (string.IsNullOrEmpty(authorization))
                {
                    return BadRequest(new { Error = "Authorization header is required" });
                }

                var userId = _jwtTokenService.GetUserIdFromToken(authorization);
                var allClaims = _jwtTokenService.GetAllClaims(authorization);
                var isValid = _jwtTokenService.IsTokenValid(authorization);

                return Ok(new
                {
                    IsValid = isValid,
                    UserId = userId,
                    AllClaims = allClaims,
                    RawToken = authorization
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token debugging");
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
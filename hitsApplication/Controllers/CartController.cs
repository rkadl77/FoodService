using hitsApplication.AuthServices;
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

        private string GetUserIdFromHeader(string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return null;

            return _jwtTokenService.GetUserIdFromToken(authorization);
        }

        [HttpGet]
        public IActionResult GetCart([FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.GetCart(userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart");
                return StatusCode(500, new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error"
                });
            }
        }

        [HttpPost("add")]
        public IActionResult AddToCart([FromBody] AddToCartRequest request, [FromHeader] string authorization)
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

                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.AddToCart(userId, request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart");
                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to add item to cart"
                });
            }
        }

        [HttpPost("create-order")]
        public async Task<IActionResult> CreateOrderFromCart(
            [FromBody] CreateOrderRequest request,
            [FromHeader] string authorization)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid request data"
                    });
                }

                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = await _cartService.CreateOrderFromCart(userId, request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart");
                return StatusCode(500, new OrderCreationResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error when creating order"
                });
            }
        }

        [HttpPut("update")]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request, [FromHeader] string authorization)
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

                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.UpdateQuantity(userId, request.DishId.ToString(), request.Quantity);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity");
                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to update quantity"
                });
            }
        }

        [HttpDelete("remove/{dishId}")]
        public IActionResult RemoveFromCart(string dishId, [FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.RemoveFromCart(userId, dishId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to remove item from cart"
                });
            }
        }

        [HttpDelete("clear")]
        public IActionResult ClearCart([FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.ClearCart(userId);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return BadRequest(new CartSummaryResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to clear cart"
                });
            }
        }

        [HttpGet("summary")]
        public IActionResult GetCartSummary([FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new CartSummaryResponse
                    {
                        Success = false,
                        ErrorMessage = "Invalid or missing authorization token"
                    });
                }

                var result = _cartService.GetCartSummary(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return Ok(new CartSummaryResponse { Success = false });
            }
        }

        [HttpGet("check/{dishId}")]
        public IActionResult IsInCart(string dishId, [FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { IsInCart = false, Message = "Invalid or missing authorization token" });
                }

                var isInCart = _cartService.IsInCart(userId, dishId);
                return Ok(new { IsInCart = isInCart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart");
                return Ok(new { IsInCart = false });
            }
        }

        [HttpGet("debug-cart")]
        public IActionResult DebugCart([FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Success = false, ErrorMessage = "Invalid or missing authorization token" });
                }

                var cart = _cartService.GetCart(userId);

                return Ok(new
                {
                    Success = cart.Success,
                    IsEmpty = cart.IsEmpty,
                    HasItems = cart.HasItems,
                    ItemCount = cart.ItemCount,
                    Total = cart.Total,
                    ItemsCount = cart.Items?.Count ?? 0,
                    Items = cart.Items,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging cart");
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

    public class TokenTestRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
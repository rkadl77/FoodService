using hitsApplication.AuthServices;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using hitsApplication.Filters;

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
        public IActionResult GetCart([FromQuery] string basketId = null)
        {
            try
            {
                var result = _cartService.GetCart(basketId);
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
        public IActionResult AddToCart([FromBody] AddToCartRequest request, [FromHeader] string basketId)
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

                var result = _cartService.AddToCart(basketId, request);
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

                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    });
                }

                var userId = GetUserIdFromHttpContext();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "User ID not found in context"
                    });
                }

                var result = await _cartService.CreateOrderFromCart(basketId, userId, request);
                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart for basket {BasketId}", basketId);
                return StatusCode(500, new OrderCreationResponse
                {
                    Success = false,
                    ErrorMessage = "Internal server error when creating order"
                });
            }
        }

        [HttpPut("update")]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request, [FromHeader] string basketId)
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

                var result = _cartService.UpdateQuantity(basketId, request.DishId.ToString(), request.Quantity);
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
        public IActionResult RemoveFromCart(string dishId, [FromHeader] string basketId)
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

                var result = _cartService.RemoveFromCart(basketId, dishId);
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
        public IActionResult ClearCart([FromHeader] string basketId)
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

                var result = _cartService.ClearCart(basketId);
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
        public IActionResult GetCartSummary([FromHeader] string basketId)
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

                var result = _cartService.GetCartSummary(basketId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for basket {BasketId}", basketId);
                return Ok(new CartSummaryResponse { Success = false });
            }
        }

        [HttpGet("check/{dishId}")]
        public IActionResult IsInCart(string dishId, [FromHeader] string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new { IsInCart = false, Message = "Basket ID is required" });
                }

                var isInCart = _cartService.IsInCart(basketId, dishId);
                return Ok(new { IsInCart = isInCart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart for basket {BasketId}", basketId);
                return Ok(new { IsInCart = false });
            }
        }

        [HttpGet("debug-cart")]
        public IActionResult DebugCart([FromHeader] string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    return BadRequest(new { Success = false, ErrorMessage = "Basket ID is required" });
                }

                var cart = _cartService.GetCart(basketId);

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

    public class TokenTestRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
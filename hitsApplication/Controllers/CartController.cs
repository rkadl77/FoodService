using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Services.Interfaces;
using hitsApplication.Models.DTOs.Requests;
using Microsoft.AspNetCore.Mvc;

namespace hitsApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetCart()
        {
            try
            {
                var result = _cartService.GetCart();
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
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
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

                var result = _cartService.AddToCart(request);
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

        [HttpPut("update")]
        public IActionResult UpdateQuantity([FromBody] UpdateQuantityRequest request)
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

                var result = _cartService.UpdateQuantity(request.DishId, request.Quantity);
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
        public IActionResult RemoveFromCart(int dishId)
        {
            try
            {
                var result = _cartService.RemoveFromCart(dishId);
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
        public IActionResult ClearCart()
        {
            try
            {
                var result = _cartService.ClearCart();
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
        public IActionResult GetCartSummary()
        {
            try
            {
                var result = _cartService.GetCartSummary();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary");
                return Ok(new CartSummaryResponse { Success = false });
            }
        }

        [HttpGet("check/{dishId}")]
        public IActionResult IsInCart(int dishId)
        {
            try
            {
                var isInCart = _cartService.IsInCart(dishId);
                return Ok(new { IsInCart = isInCart });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart");
                return Ok(new { IsInCart = false });
            }
        }
    }
}
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
        private readonly HttpClient _httpClient;

        public CartController(
            ICartService cartService,
            IJwtTokenService jwtTokenService,
            ILogger<CartController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _cartService = cartService;
            _jwtTokenService = jwtTokenService;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        private string GetUserIdFromHeader(string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return null;

            return _jwtTokenService.GetUserIdFromToken(authorization);
        }

        private JavaOrderRequest ConvertToJavaOrder(CartSummaryResponse cart, string userId)
        {
            return new JavaOrderRequest
            {
                Success = cart.Success,
                ErrorMessage = cart.ErrorMessage,
                UserId = Guid.Parse(userId),
                ItemCount = cart.ItemCount,
                Total = (double)cart.Total,
                Items = cart.Items?.Select(item => new JavaOrderItem
                {
                    DishId = Guid.Parse(item.DishId), 
                    Name = item.Name,
                    Price = (double)item.Price,
                    Quantity = item.Quantity,
                    ImageUrl = item.ImageUrl
                }).ToList() ?? new List<JavaOrderItem>(),
                IsEmpty = cart.IsEmpty,
                HasItems = cart.HasItems,
                PhoneNumber = cart.PhoneNumber,
                Address = cart.Address,
                PaymentMethod = cart.PaymentMethod,
                Comment = cart.Comment
            };
        }

        [HttpGet("get-cart-full")]
        public IActionResult GetCartFull([FromHeader] string authorization)
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
                return Ok(new
                {
                    Method = "GetCart",
                    Success = result.Success,
                    IsEmpty = result.IsEmpty,
                    HasItems = result.HasItems,
                    ItemCount = result.ItemCount,
                    Total = result.Total,
                    Items = result.Items,
                    UserId = userId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting full cart");
                return StatusCode(500, new { Success = false, ErrorMessage = ex.Message });
            }
        }

        [HttpPost("add-and-send-to-java")]
        public async Task<IActionResult> AddToCartAndSendToJava([FromBody] AddToCartRequest request, [FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Success = false, ErrorMessage = "Invalid or missing authorization token" });
                }

                var cartResult = _cartService.AddToCart(userId, request);
                if (!cartResult.Success)
                {
                    return BadRequest(cartResult);
                }

                var javaOrder = new JavaOrderRequest
                {
                    Success = cartResult.Success,
                    ErrorMessage = cartResult.ErrorMessage,
                    UserId = Guid.Parse(userId),
                    ItemCount = cartResult.ItemCount,
                    Total = (double)cartResult.Total,
                    Items = cartResult.Items?.Select(item => new JavaOrderItem
                    {
                        DishId = Guid.Parse(item.DishId),
                        Name = item.Name,
                        Price = (double)item.Price,
                        Quantity = item.Quantity,
                        ImageUrl = item.ImageUrl
                    }).ToList() ?? new List<JavaOrderItem>(),
                    IsEmpty = cartResult.IsEmpty,
                    HasItems = cartResult.HasItems,
                    PhoneNumber = cartResult.PhoneNumber,
                    Address = cartResult.Address,
                    PaymentMethod = cartResult.PaymentMethod,
                    Comment = cartResult.Comment
                };

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(javaOrder, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:8080/order/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Order successfully created in Java system");

                    return Ok(new
                    {
                        Success = true,
                        Message = "Item added to cart and order sent to Java system",
                        CartData = cartResult,
                        JavaResponse = responseContent
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Java API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    return Ok(new
                    {
                        Success = true,
                        Message = "Item added to cart but failed to send to Java",
                        CartData = cartResult,
                        JavaError = $"Java API error: {response.StatusCode}",
                        JavaDetails = errorContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in add-and-send-to-java");
                return StatusCode(500, new
                {
                    Success = false,
                    ErrorMessage = "Internal server error",
                    Details = ex.Message
                });
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

                var cart = _cartService.GetCartSummary(userId);

                return Ok(new
                {
                    Success = cart.Success,
                    IsEmpty = cart.IsEmpty,
                    HasItems = cart.HasItems,
                    ItemCount = cart.ItemCount,
                    Total = cart.Total,
                    ItemsCount = cart.Items?.Count ?? 0,
                    Items = cart.Items,
                    UserId = userId,
                    RawCartData = cart 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error debugging cart");
                return StatusCode(500, new { Success = false, ErrorMessage = ex.Message });
            }
        }

        [HttpPost("create-order-in-java")]
        public async Task<IActionResult> CreateOrderInJava([FromHeader] string authorization)
        {
            try
            {
                var userId = GetUserIdFromHeader(authorization);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { Success = false, ErrorMessage = "Invalid or missing authorization token" });
                }

                var cart = _cartService.GetCart(userId);
                if (!cart.Success || cart.IsEmpty)
                {
                    return BadRequest(new { Success = false, ErrorMessage = "Cart is empty or not found" });
                }

                var javaOrder = ConvertToJavaOrder(cart, userId);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(javaOrder, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:8080/order/create", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Order successfully created in Java system");

                    return Ok(new
                    {
                        Success = true,
                        Message = "Order successfully created in Java system",
                        JavaResponse = responseContent
                    });
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Java API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                    return BadRequest(new
                    {
                        Success = false,
                        ErrorMessage = $"Java API error: {response.StatusCode}",
                        Details = errorContent
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order in Java system");
                return StatusCode(500, new
                {
                    Success = false,
                    ErrorMessage = "Internal server error when connecting to Java system",
                    Details = ex.Message
                });
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
    }

    public class TokenTestRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
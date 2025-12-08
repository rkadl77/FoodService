using hitsApplication.Data;
using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.Entities;
using hitsApplication.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace hitsApplication.Services
{
    public partial class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly BuggyFeaturesService _buggyService;
        private readonly FeatureFlags _featureFlags;

        public CartService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger,
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            BuggyFeaturesService buggyService,
            IOptions<FeatureFlags> featureFlags)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _context = context;
            _buggyService = buggyService;
            _featureFlags = featureFlags.Value;
        }

        private string GenerateBasketId()
        {
            return $"basket_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        }

        private CartSummaryResponse MapToResponse(List<CartItem> cartItems, string basketId, bool includeItems = true)
        {
            var cart = new Cart { Items = cartItems };

            return new CartSummaryResponse
            {
                Success = true,
                BasketId = basketId,
                ItemCount = cart.TotalItems,
                Total = cart.Total,
                Items = includeItems ? cartItems.Select(item => new CartItemResponse
                {
                    DishId = item.DishId.ToString(),
                    Name = item.Name,
                    Price = item.Price,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity
                }).ToList() : null
            };
        }

        private CartSummaryResponse ErrorResponse(string errorMessage)
        {
            return new CartSummaryResponse
            {
                Success = false,
                ErrorMessage = errorMessage,
                ItemCount = 0,
                Total = 0,
                Items = null
            };
        }

        public async Task<CartSummaryResponse> GetCart(string basketId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    basketId = GenerateBasketId();
                    return new CartSummaryResponse
                    {
                        Success = true,
                        BasketId = basketId,
                        ItemCount = 0,
                        Total = 0,
                        Items = new List<CartItemResponse>()
                    };
                }

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                return MapToResponse(cartItems, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public async Task<CartSummaryResponse> AddToCart(string basketId, AddToCartRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                if (request.Quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var modifiedRequest = _buggyService.ApplyBugsToRequest(request);

                if (_buggyService.ShouldSkipValidation(request.DishId.ToString()))
                {
                    _logger.LogWarning("Validation skipped for product {DishId}", request.DishId);
                }

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == modifiedRequest.DishId);

                if (existingItem != null)
                {

                    var quantityToAdd = _buggyService.ApplyQuantityBug(modifiedRequest.Quantity);

                    if (existingItem.Quantity + quantityToAdd > _featureFlags.CartItemLimit)
                    {
                        quantityToAdd = Math.Max(1, _featureFlags.CartItemLimit - existingItem.Quantity);
                    }

                    existingItem.Quantity += quantityToAdd;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var imageUrl = _buggyService.ApplyImageUrlBug(modifiedRequest.ImageUrl);

                    var newItem = new CartItem
                    {
                        Id = Guid.NewGuid(),
                        BasketId = basketId,
                        DishId = modifiedRequest.DishId,
                        Name = modifiedRequest.Name,
                        Price = modifiedRequest.Price,
                        ImageUrl = imageUrl,
                        Quantity = modifiedRequest.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                var response = MapToResponse(cartItems, basketId, includeItems: true);

                return _buggyService.ApplyResponseBug(response);
            }
            catch (Exception ex)
            {
                if (_buggyService.ShouldLogSensitiveInfo())
                {
                    _logger.LogError(ex,
                        "Error adding item to cart. Basket: {BasketId}, DishId: {DishId}",
                        basketId, request.DishId);
                }
                else
                {
                    _logger.LogError(ex, "Error adding item to cart for basket {BasketId}", basketId);
                }

                return ErrorResponse($"Ошибка при добавлении в корзину: {ex.Message}");
            }
        }
        public async Task<CartSummaryResponse> RemoveFromCart(string basketId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                if (!Guid.TryParse(dishId, out var dishGuid))
                    return ErrorResponse("Неверный формат DishId");

                var itemToRemove = await _context.CartItems
                    .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == dishGuid);

                if (itemToRemove == null)
                    return ErrorResponse("Товар не найден в корзине");

                _context.CartItems.Remove(itemToRemove);
                await _context.SaveChangesAsync();

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                return MapToResponse(cartItems, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при удалении из корзины: {ex.Message}");
            }
        }

        public async Task<CartSummaryResponse> UpdateQuantity(string basketId, string dishId, int quantity)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                if (quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                if (!Guid.TryParse(dishId, out var dishGuid))
                    return ErrorResponse("Неверный формат DishId");

                var item = await _context.CartItems
                    .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == dishGuid);

                if (item == null)
                    return ErrorResponse("Товар не найден в корзине");

                item.Quantity = quantity;
                item.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                return MapToResponse(cartItems, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при обновлении количества: {ex.Message}");
            }
        }

        public async Task<CartSummaryResponse> ClearCart(string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                var itemsToRemove = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                _context.CartItems.RemoveRange(itemsToRemove);
                await _context.SaveChangesAsync();

                return new CartSummaryResponse
                {
                    Success = true,
                    BasketId = basketId,
                    ItemCount = 0,
                    Total = 0,
                    Items = new List<CartItemResponse>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при очистке корзины: {ex.Message}");
            }
        }

        public async Task<CartSummaryResponse> GetCartSummary(string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                return MapToResponse(cartItems, basketId, includeItems: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public async Task<bool> IsInCart(string basketId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId) || !Guid.TryParse(dishId, out var dishGuid))
                    return false;

                return await _context.CartItems
                    .AnyAsync(x => x.BasketId == basketId && x.DishId == dishGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart for basket {BasketId}", basketId);
                return false;
            }
        }

        public async Task<OrderCreationResponse> CreateOrderFromCart(string basketId, string userId, CreateOrderRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Basket ID is required"
                    };

                if (string.IsNullOrEmpty(userId) ||
                    string.IsNullOrEmpty(request.PhoneNumber) ||
                    string.IsNullOrEmpty(request.Address) ||
                    string.IsNullOrEmpty(request.PaymentMethod))
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Не все обязательные поля заполнены"
                    };
                }

                if (!IsValidRussianPhoneNumber(request.PhoneNumber))
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Некорректный формат номера телефона"
                    };
                }

                if (!IsValidPaymentMethod(request.PaymentMethod))
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Некорректный способ оплаты"
                    };
                }

                var cartItems = await _context.CartItems
                    .Where(x => x.BasketId == basketId)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Корзина пуста"
                    };
                }

                _logger.LogInformation(
                    "НОВЫЙ ЗАКАЗ ДЛЯ СОЗДАНИЯ В JAVA-СИСТЕМЕ\n" +
                    "Пользователь: {UserId}\n" +
                    "Корзина: {BasketId}\n" +
                    "Телефон: {PhoneNumber}\n" +
                    "Адрес: {Address}\n" +
                    "Способ оплаты: {PaymentMethod}\n" +
                    "Комментарий: {Comment}\n" +
                    "Количество позиций: {ItemCount}\n" +
                    "Общая сумма: {Total} руб.",
                    userId, basketId, request.PhoneNumber, request.Address,
                    request.PaymentMethod, request.Comment ?? "нет комментария",
                    cartItems.Count, cartItems.Sum(x => x.Price * x.Quantity)
                );

                var javaSuccess = await SendOrderToJavaService(basketId, userId, request, cartItems);

                if (javaSuccess)
                {
                    await ClearCart(basketId);
                    return new OrderCreationResponse
                    {
                        Success = true,
                        Message = "Заказ успешно создан"
                    };
                }
                else
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Ошибка при создании заказа в системе"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа для корзины {BasketId}", basketId);
                return new OrderCreationResponse
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при создании заказа: {ex.Message}"
                };
            }
        }

        private async Task<bool> SendOrderToJavaService(string basketId, string userId, CreateOrderRequest request, List<CartItem> cartItems)
        {
            try
            {
                var authorizationHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authorizationHeader))
                {
                    _logger.LogError("No authorization token found in current request");
                    return false;
                }

                if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    authorizationHeader = "Bearer " + authorizationHeader;
                }

                var javaPaymentMethod = request.PaymentMethod?.ToUpperInvariant();

                var javaOrderRequest = new
                {
                    success = true,
                    errorMessage = (string?)null,
                    userId = Guid.Parse(userId),
                    itemCount = cartItems.Count,
                    total = (double)cartItems.Sum(x => x.Price * x.Quantity),
                    items = cartItems.Select(item => new
                    {
                        id = item.DishId, 
                        name = item.Name,
                        price = (double)item.Price,
                        imageUrl = !string.IsNullOrEmpty(item.ImageUrl)
                            ? new List<string> { item.ImageUrl }
                            : new List<string>(),
                        quantity = item.Quantity
                    }).ToList(),
                    isEmpty = cartItems.Count == 0,
                    hasItems = cartItems.Count > 0,
                    phoneNumber = request.PhoneNumber,
                    address = request.Address,
                    paymentMethod = javaPaymentMethod,
                    comment = request.Comment
                };

                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(javaOrderRequest, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending to Java: {Json}", json);

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost:8096/order/create");
                httpRequest.Content = content;
                httpRequest.Headers.Add("Authorization", authorizationHeader);

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Order successfully created in Java system for basket {BasketId}", basketId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Java API error for basket {BasketId}: {StatusCode} - {Error}",
                        basketId, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order to Java system for basket {BasketId}", basketId);
                return false;
            }
        }

        private bool IsValidRussianPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var cleaned = phoneNumber
                .Replace("+", "")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("(", "")
                .Replace(")", "");

            return cleaned.Length == 11 &&
                   (cleaned.StartsWith("7") || cleaned.StartsWith("8"));
        }

        private bool IsValidPaymentMethod(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return false;

            var validMethods = new[] {
        "CARD_ONLINE",      
        "CARD_COURIER",     
        "CASH_COURIER"     
    };

            return validMethods.Contains(paymentMethod.ToUpperInvariant());
        }
    }
}
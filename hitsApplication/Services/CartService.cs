using hitsApplication.Services.Interfaces;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.Entities;
using System.Text.Json;

namespace hitsApplication.Services
{
    public class CartService : ICartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartService> _logger;

        public CartService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session ??
            throw new InvalidOperationException("Session is not available");

        private string GetCartSessionKey(string userId)
        {
            return $"ShoppingCart_{userId}";
        }

        private Cart GetCartEntity(string userId)
        {
            try
            {
                var cartKey = GetCartSessionKey(userId);
                var cartJson = Session.GetString(cartKey);
                return string.IsNullOrEmpty(cartJson)
                    ? new Cart()
                    : JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
            }
            catch (Exception)
            {
                return new Cart();
            }
        }

        private void SaveCartEntity(string userId, Cart cart)
        {
            var cartKey = GetCartSessionKey(userId);
            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(cartKey, cartJson);
        }

        private CartSummaryResponse MapToResponse(Cart cart, bool includeItems = true)
        {
            return new CartSummaryResponse
            {
                Success = true,
                ItemCount = cart.TotalItems,
                Total = cart.Total,
                Items = includeItems ? cart.Items.Select(item => new CartItemResponse
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

        public CartSummaryResponse GetCart(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                var cart = GetCartEntity(userId);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse AddToCart(string userId, AddToCartRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                if (request.Quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity(userId);

                var dishIdString = request.DishId.ToString();
                var existingItem = cart.Items.FirstOrDefault(item => item.DishId.ToString() == dishIdString);

                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                }
                else
                {
                    cart.Items.Add(new CartItem
                    {
                        DishId = request.DishId,
                        Name = request.Name,
                        Price = request.Price,
                        ImageUrl = request.ImageUrl,
                        Quantity = request.Quantity
                    });
                }

                SaveCartEntity(userId, cart);

                var result = MapToResponse(cart, includeItems: true);
                result.UserId = userId;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for user {UserId}", userId);
                return ErrorResponse($"Ошибка при добавлении в корзину: {ex.Message}");
            }
        }

        public CartSummaryResponse RemoveFromCart(string userId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                var cart = GetCartEntity(userId);
                var itemToRemove = cart.Items.FirstOrDefault(item => item.DishId.ToString() == dishId);

                if (itemToRemove == null)
                    return ErrorResponse("Товар не найден в корзине");

                cart.Items.Remove(itemToRemove);
                SaveCartEntity(userId, cart);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for user {UserId}", userId);
                return ErrorResponse($"Ошибка при удалении из корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse UpdateQuantity(string userId, string dishId, int quantity)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                if (quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity(userId);
                var item = cart.Items.FirstOrDefault(item => item.DishId.ToString() == dishId);

                if (item == null)
                    return ErrorResponse("Товар не найден в корзине");

                item.Quantity = quantity;
                SaveCartEntity(userId, cart);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for user {UserId}", userId);
                return ErrorResponse($"Ошибка при обновлении количества: {ex.Message}");
            }
        }

        public CartSummaryResponse ClearCart(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                var cartKey = GetCartSessionKey(userId);
                Session.Remove(cartKey);

                return new CartSummaryResponse
                {
                    Success = true,
                    ItemCount = 0,
                    Total = 0,
                    Items = new List<CartItemResponse>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart for user {UserId}", userId);
                return ErrorResponse($"Ошибка при очистке корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse GetCartSummary(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return ErrorResponse("User ID is required");

                var cart = GetCartEntity(userId);
                return MapToResponse(cart, includeItems: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for user {UserId}", userId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public bool IsInCart(string userId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    return false;

                var cart = GetCartEntity(userId);
                return cart.Items.Any(item => item.DishId.ToString() == dishId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if item is in cart for user {UserId}", userId);
                return false;
            }
        }

        public async Task<OrderCreationResponse> CreateOrderFromCart(string userId, CreateOrderRequest request)
        {
            try
            {
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

                var cart = GetCartEntity(userId);

                if (cart.Items.Count == 0)
                {
                    return new OrderCreationResponse
                    {
                        Success = false,
                        ErrorMessage = "Корзина пуста"
                    };
                }

                // Здесь бд ,ну мб не понадобится 

                // Пока просто логируем создание заказа
                _logger.LogInformation(
                    "Order created for user {UserId}. Items: {ItemCount}, Total: {Total}, Phone: {Phone}, Address: {Address}",
                    userId, cart.Items.Count, cart.Total, request.PhoneNumber, request.Address);

                ClearCart(userId);

                return new OrderCreationResponse
                {
                    Success = true,
                    Message = "Заказ успешно создан"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart for user {UserId}", userId);
                return new OrderCreationResponse
                {
                    Success = false,
                    ErrorMessage = $"Ошибка при создании заказа: {ex.Message}"
                };
            }
        }
    }
}
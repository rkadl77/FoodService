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

        private string GetCartSessionKey(string basketId)
        {
            return $"ShoppingCart_{basketId}";
        }

        private Cart GetCartEntity(string basketId)
        {
            try
            {
                var cartKey = GetCartSessionKey(basketId);
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

        private void SaveCartEntity(string basketId, Cart cart)
        {
            var cartKey = GetCartSessionKey(basketId);
            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(cartKey, cartJson);
        }

        private string GenerateBasketId()
        {
            return $"basket_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
        }

        private CartSummaryResponse MapToResponse(Cart cart, string basketId, bool includeItems = true)
        {
            return new CartSummaryResponse
            {
                Success = true,
                BasketId = basketId,
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

        public CartSummaryResponse GetCart(string basketId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                {
                    basketId = GenerateBasketId();
                    var newCart = new Cart();
                    SaveCartEntity(basketId, newCart);

                    return new CartSummaryResponse
                    {
                        Success = true,
                        BasketId = basketId,
                        ItemCount = 0,
                        Total = 0,
                        Items = new List<CartItemResponse>()
                    };
                }

                var cart = GetCartEntity(basketId);
                return MapToResponse(cart, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse AddToCart(string basketId, AddToCartRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                if (request.Quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity(basketId);

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

                SaveCartEntity(basketId, cart);
                return MapToResponse(cart, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding item to cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при добавлении в корзину: {ex.Message}");
            }
        }

        public CartSummaryResponse RemoveFromCart(string basketId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                var cart = GetCartEntity(basketId);
                var itemToRemove = cart.Items.FirstOrDefault(item => item.DishId.ToString() == dishId);

                if (itemToRemove == null)
                    return ErrorResponse("Товар не найден в корзине");

                cart.Items.Remove(itemToRemove);
                SaveCartEntity(basketId, cart);
                return MapToResponse(cart, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при удалении из корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse UpdateQuantity(string basketId, string dishId, int quantity)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                if (quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity(basketId);
                var item = cart.Items.FirstOrDefault(item => item.DishId.ToString() == dishId);

                if (item == null)
                    return ErrorResponse("Товар не найден в корзине");

                item.Quantity = quantity;
                SaveCartEntity(basketId, cart);
                return MapToResponse(cart, basketId, includeItems: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при обновлении количества: {ex.Message}");
            }
        }

        public CartSummaryResponse ClearCart(string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                var cartKey = GetCartSessionKey(basketId);
                Session.Remove(cartKey);

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

        public CartSummaryResponse GetCartSummary(string basketId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return ErrorResponse("Basket ID is required");

                var cart = GetCartEntity(basketId);
                return MapToResponse(cart, basketId, includeItems: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart summary for basket {BasketId}", basketId);
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public bool IsInCart(string basketId, string dishId)
        {
            try
            {
                if (string.IsNullOrEmpty(basketId))
                    return false;

                var cart = GetCartEntity(basketId);
                return cart.Items.Any(item => item.DishId.ToString() == dishId);
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

                var cart = GetCartEntity(basketId);

                if (cart.Items.Count == 0)
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
                    "Общая сумма: {Total} руб.\n" +
                    "Состав заказа:\n{OrderItems}",
                    userId,
                    basketId,
                    request.PhoneNumber,
                    request.Address,
                    request.PaymentMethod,
                    request.Comment ?? "нет комментария",
                    cart.Items.Count,
                    cart.Total,
                    string.Join("\n", cart.Items.Select((item, index) =>
                        $"{index + 1}. {item.Name} - {item.Quantity} x {item.Price} руб. = {item.Subtotal} руб."))
                );

                ClearCart(basketId);

                return new OrderCreationResponse
                {
                    Success = true,
                    Message = "Заказ отправлен менеджеру, с вами свяжутся по указанному телефону для подтверждения заказа"
                };
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
                   (cleaned.StartsWith("79") || cleaned.StartsWith("89"));
        }

        private bool IsValidPaymentMethod(string paymentMethod)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
                return false;

            var validMethods = new[] {
                "card_online",
                "card_courier",
                "cash_courier"
            };

            return validMethods.Contains(paymentMethod);
        }
    }
}
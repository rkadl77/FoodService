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
        private const string CartSessionKey = "ShoppingCart";

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext?.Session ??
            throw new InvalidOperationException("Session is not available");

        private Cart GetCartEntity()
        {
            try
            {
                var cartJson = Session.GetString(CartSessionKey);
                return string.IsNullOrEmpty(cartJson)
                    ? new Cart()
                    : JsonSerializer.Deserialize<Cart>(cartJson) ?? new Cart();
            }
            catch (Exception)
            {
                return new Cart();
            }
        }

        private void SaveCartEntity(Cart cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            Session.SetString(CartSessionKey, cartJson);
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
                    DishId = item.DishId,
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

        public CartSummaryResponse GetCart()
        {
            try
            {
                var cart = GetCartEntity();
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse AddToCart(AddToCartRequest request)
        {
            try
            {
                if (request.Quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity();
                var existingItem = cart.Items.FirstOrDefault(item => item.DishId == request.DishId);

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

                SaveCartEntity(cart);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Ошибка при добавлении в корзину: {ex.Message}");
            }
        }

        public CartSummaryResponse RemoveFromCart(int dishId)
        {
            try
            {
                var cart = GetCartEntity();
                var itemToRemove = cart.Items.FirstOrDefault(item => item.DishId == dishId);

                if (itemToRemove == null)
                    return ErrorResponse("Товар не найден в корзине");

                cart.Items.Remove(itemToRemove);
                SaveCartEntity(cart);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Ошибка при удалении из корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse UpdateQuantity(int dishId, int quantity)
        {
            try
            {
                if (quantity < 1)
                    return ErrorResponse("Количество должно быть не менее 1");

                var cart = GetCartEntity();
                var item = cart.Items.FirstOrDefault(item => item.DishId == dishId);

                if (item == null)
                    return ErrorResponse("Товар не найден в корзине");

                item.Quantity = quantity;
                SaveCartEntity(cart);
                return MapToResponse(cart, includeItems: true);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Ошибка при обновлении количества: {ex.Message}");
            }
        }

        public CartSummaryResponse ClearCart()
        {
            try
            {
                Session.Remove(CartSessionKey);
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
                return ErrorResponse($"Ошибка при очистке корзины: {ex.Message}");
            }
        }

        public CartSummaryResponse GetCartSummary()
        {
            try
            {
                var cart = GetCartEntity();
                return MapToResponse(cart, includeItems: false);
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Ошибка при получении корзины: {ex.Message}");
            }
        }

        public bool IsInCart(int dishId)
        {
            try
            {
                var cart = GetCartEntity();
                return cart.Items.Any(item => item.DishId == dishId);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
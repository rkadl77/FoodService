using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.DTOs.Requests;

namespace hitsApplication.Services.Interfaces
{
    public interface ICartService
    {
        CartSummaryResponse GetCart(string userId);
        CartSummaryResponse AddToCart(string userId, AddToCartRequest request);
        CartSummaryResponse RemoveFromCart(string userId, string dishId);
        CartSummaryResponse UpdateQuantity(string userId, string dishId, int quantity);
        CartSummaryResponse ClearCart(string userId);
        CartSummaryResponse GetCartSummary(string userId);
        bool IsInCart(string userId, string dishId);
        Task<OrderCreationResponse> CreateOrderFromCart(string userId, CreateOrderRequest request);
    }
}
using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.DTOs.Requests;

namespace hitsApplication.Services.Interfaces
{
    public interface ICartService
    {
        CartSummaryResponse GetCart(string basketId = null);
        CartSummaryResponse AddToCart(string basketId, AddToCartRequest request);
        CartSummaryResponse RemoveFromCart(string basketId, string dishId);
        CartSummaryResponse UpdateQuantity(string basketId, string dishId, int quantity);
        CartSummaryResponse ClearCart(string basketId);
        CartSummaryResponse GetCartSummary(string basketId);
        bool IsInCart(string basketId, string dishId);
        Task<OrderCreationResponse> CreateOrderFromCart(string basketId, string userId, CreateOrderRequest request);
    }
}
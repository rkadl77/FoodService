using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.DTOs.Requests;

namespace hitsApplication.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartSummaryResponse> GetCart(string basketId = null);
        Task<CartSummaryResponse> AddToCart(string basketId, AddToCartRequest request);
        Task<CartSummaryResponse> RemoveFromCart(string basketId, string dishId);
        Task<CartSummaryResponse> UpdateQuantity(string basketId, string dishId, int quantity);
        Task<CartSummaryResponse> ClearCart(string basketId);
        Task<CartSummaryResponse> GetCartSummary(string basketId);
        Task<bool> IsInCart(string basketId, string dishId);
        Task<OrderCreationResponse> CreateOrderFromCart(string basketId, string userId, CreateOrderRequest request);
    }
}
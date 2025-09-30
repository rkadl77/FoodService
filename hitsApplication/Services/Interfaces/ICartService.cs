using hitsApplication.Models.DTOs.Responses;
using hitsApplication.Models.DTOs.Requests;

namespace hitsApplication.Services.Interfaces
{
    public interface ICartService
    {
        CartSummaryResponse GetCart();
        CartSummaryResponse AddToCart(AddToCartRequest request);
        CartSummaryResponse RemoveFromCart(int dishId);
        CartSummaryResponse UpdateQuantity(int dishId, int quantity);
        CartSummaryResponse ClearCart();
        CartSummaryResponse GetCartSummary();
        bool IsInCart(int dishId);
    }
}
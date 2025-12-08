using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using Microsoft.Extensions.Options;

namespace hitsApplication.Services
{
    public class BuggyFeaturesService
    {
        private readonly FeatureFlags _flags;

        public BuggyFeaturesService(IOptions<FeatureFlags> flags)
        {
            _flags = flags?.Value ?? new FeatureFlags(); 
        }

        public AddToCartRequest ApplyBugsToRequest(AddToCartRequest original)
        {
            if (_flags == null || !_flags.EnableCalculationBug)
                return original;

            return new AddToCartRequest
            {
                DishId = original.DishId,
                Name = original.Name,
                Price = original.Price + original.Quantity, 
                ImageUrl = original.ImageUrl,
                Quantity = original.Quantity
            };
        }

        public int ApplyQuantityBug(int originalQuantity)
        {
            if (_flags == null || !_flags.EnableOverflowBug)
                return originalQuantity;

            return originalQuantity * 100;
        }

        public string ApplyImageUrlBug(string originalUrl)
        {
            if (_flags == null || !_flags.EnableImageUrlBug || string.IsNullOrEmpty(originalUrl))
                return originalUrl;

            if (Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
                return originalUrl;

            return $"http://localhost:5000{originalUrl}";
        }

        public CartSummaryResponse ApplyResponseBug(CartSummaryResponse response)
        {
            if (_flags == null || !_flags.EnableResponseBug)
                return response;

            return new CartSummaryResponse
            {
                Success = response.Success,
                BasketId = response.BasketId,
                ErrorMessage = response.ErrorMessage,
                ItemCount = response.ItemCount,
                Total = response.Total * 1.1m, 
                Items = response.Items?.Select(item => new CartItemResponse
                {
                    DishId = item.DishId,
                    Name = item.Name,
                    Price = item.Price,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity
                }).ToList() ?? new List<CartItemResponse>()
            };
        }

        public bool ShouldLogSensitiveInfo()
        {
            return _flags?.EnableInfoLeakBug == true;
        }

        public bool ShouldSkipValidation(string dishId)
        {
            if (_flags == null || !_flags.EnableValidationBug || string.IsNullOrEmpty(dishId))
                return false;

            return dishId.EndsWith("9", StringComparison.Ordinal);
        }
    }
}
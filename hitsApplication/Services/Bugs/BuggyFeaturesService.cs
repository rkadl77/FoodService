using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using Microsoft.Extensions.Options;

namespace hitsApplication.Services

{
    // Enum для типов багов
    public enum OrderServiceBugType
    {
        None = 0,
        ReturnFalseImmediately = 1,      // Просто возвращает false
        ThrowException = 2,              // Бросает исключение
        InfiniteTimeout = 3,             // Бесконечная задержка
        WrongUrl = 4,                    // Неправильный URL
        InvalidData = 5,                 // Неправильные данные
        FakeSuccess = 6,                 // Фейковый успех
        WrongHttpMethod = 7,             // Неправильный HTTP метод
        WrongHeaders = 8,                // Неправильные заголовки
        HideErrors = 9                   // Прячет ошибки
    }
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

        public bool ShouldBreakOrderCreation()
        {
            return _flags?.BreakOrderCreation == true;
        }

        // БАГ 3: Не изменять количество при добавлении
        public bool ShouldNotChangeQuantityOnAdd()
        {
            return _flags?.NoQuantityChangeOnAdd == true;
        }

        // БАГ 4: Не изменять количество при удалении
        public bool ShouldNotChangeQuantityOnRemove()
        {
            return _flags?.NoQuantityChangeOnRemove == true;
        }

        // БАГ 5: Не очищать корзину
        public bool ShouldNotClearCartAfterOrder()
        {
            return _flags?.NoCartClearAfterOrder == true;
        }

        public string ApplyOrderServiceBug(string originalUrl)
        {
            if (_flags?.BreakOrderCreation != true)
                return originalUrl;

            // Разные варианты сломаного URL:
            var bugType = new Random().Next(1, 6);

            return bugType switch
            {
                1 => "http://non-existent-service:9999/broken-endpoint", // Несуществующий сервис
                2 => "http://order-service-wrong:8096/order/create",     // Неправильное имя хоста
                3 => "http://order-service:9999/order/create",           // Неправильный порт
                4 => "http://order-service:8096/wrong-endpoint",         // Неправильный эндпоинт
                5 => "https://order-service:8096/order/create",          // HTTPS вместо HTTP
                _ => "invalid-url-without-protocol"                       // Совсем неправильный URL
            };
        }
        public OrderServiceBugType GetOrderServiceBugType()
        {
            if (_flags?.BreakOrderCreation != true)
                return OrderServiceBugType.None;

            var random = new Random();
            var values = Enum.GetValues(typeof(OrderServiceBugType));
            return (OrderServiceBugType)values.GetValue(random.Next(values.Length));
        }
        
    }
}
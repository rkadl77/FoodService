using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Models.DTOs.Responses;
using Microsoft.Extensions.Options;

namespace hitsApplication.Services
{
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

        public bool ShouldNotChangeQuantityOnAdd()
        {
            return _flags?.NoQuantityChangeOnAdd == true;
        }

        public bool ShouldNotChangeQuantityOnRemove()
        {
            return _flags?.NoQuantityChangeOnRemove == true;
        }

        public bool ShouldNotClearCartAfterOrder()
        {
            return _flags?.NoCartClearAfterOrder == true;
        }

        public string ApplyOrderServiceBug(string originalUrl)
        {
            if (_flags?.BreakOrderCreation != true)
                return originalUrl;

            var bugType = new Random().Next(1, 6);

            return bugType switch
            {
                1 => "http://non-existent-service:9999/broken-endpoint",
                2 => "http://order-service-wrong:8096/order/create",
                3 => "http://order-service:9999/order/create",
                4 => "http://order-service:8096/wrong-endpoint",
                5 => "https://order-service:8096/order/create",
                _ => "invalid-url-without-protocol"
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

        // =============== НОВЫЕ МЕТОДЫ ДЛЯ ДОБАВЛЕННЫХ БАГОВ ===============

        // a) Баг: Создание новой записи вместо увеличения количества существующей
        // При добавлении товара с одинаковым DishId создается дубликат вместо увеличения Quantity
        public bool ShouldCreateDuplicateCartItem()
        {
            return _flags?.EnableDuplicateCartItemBug == true;
        }

        // b) Баг: Неправильный расчет суммы при обновлении количества
        // Товар стоит 100₽, Quantity = 2 → должно быть 200₽, но возвращает 400₽
        public decimal ApplyQuantityUpdateBug(decimal price, int quantity)
        {
            if (_flags == null || !_flags.EnableQuantityUpdateBug)
                return price * quantity;

            // Баг: умножаем итоговую сумму на 2
            return price * quantity * 2;
        }

        // c) Баг: Возврат данных другого пользователя при получении корзины
        // GetCartSummary возвращает данные чужой корзины при использовании того же BasketId
        public string GetWrongBasketId(string originalBasketId)
        {
            if (_flags == null || !_flags.EnableWrongBasketBug)
                return originalBasketId;

            // Баг: возвращаем фиксированный BasketId (например, корзину тестового пользователя)
            // Это приведет к смешиванию данных между разными пользователями
            return "fixed-test-basket-id-12345";
        }

        // d) Баг: Частичная очистка корзины
        // При очистке корзины через ClearCart удаляются только товары с четными DishId
        // Товары с нечетными DishId остаются в корзине

        // Свойство для доступа к флагу
        public bool EnablePartialClearBug => _flags?.EnablePartialClearBug == true;

        public bool ShouldSkipOddDishIdOnClear(int dishId)
        {
            if (_flags == null || !_flags.EnablePartialClearBug)
                return false;

            // Баг: пропускаем товары с нечетными DishId при очистке
            // Возвращаем true для нечетных DishId - значит, их нужно пропустить
            return dishId % 2 != 0;
        }

        // e) Баг: Пропуск проверки наличия товаров на складе
        // При создании заказа не проверяется фактическое наличие товаров
        public bool ShouldSkipStockValidation()
        {
            return _flags?.EnableSkipStockValidationBug == true;
        }

        // e) Баг: Разрешение невалидных количеств
        // Возможны заказы с Quantity = 0 или отрицательным количеством
        public bool AllowInvalidQuantities(int quantity)
        {
            if (_flags == null || !_flags.EnableInvalidQuantityBug)
                return quantity > 0;  // Нормальная валидация: только положительные числа

            // Баг: разрешаем любые значения, включая 0 и отрицательные
            return true;
        }

        // Дополнительный метод для проверки наличия на складе (имитация)
        public bool SimulateStockCheck(int dishId, int requestedQuantity)
        {
            if (ShouldSkipStockValidation())
            {
                // Баг: всегда возвращаем true, даже если товара нет на складе
                return true;
            }

            // Нормальная логика (имитация):
            // Проверяем наличие на "складе" - здесь просто пример
            var stockAvailable = new Random().Next(0, 10);
            return requestedQuantity <= stockAvailable;
        }

        // Метод для получения корректного количества товара (с учетом бага)
        public int GetValidatedQuantity(int originalQuantity)
        {
            if (AllowInvalidQuantities(originalQuantity))
            {
                // Баг: возвращаем как есть, даже если количество невалидно
                return originalQuantity;
            }

            // Нормальная логика: корректируем невалидные значения
            return Math.Max(1, originalQuantity);
        }
    }
}
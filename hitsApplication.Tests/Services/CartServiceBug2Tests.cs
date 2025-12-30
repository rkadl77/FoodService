using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class CartServiceBug2Tests : CartServiceTestBase
    {
        [Fact]
        public async Task AddToCart_WhenBug2Enabled_PriceCalculationIncorrect()
        {
            // вкл БАГ 2
            var flags = new FeatureFlags
            {
                EnableCalculationBug = true, // БАГ 2 вкл
                NoQuantityChangeOnAdd = false,
                EnableResponseBug = false
            };

            var basketId = "test-basket-bug2";
            var cartService = CreateCartService(flags);

            var request = new AddToCartRequest
            {
                DishId = Guid.NewGuid(),
                Name = "Test Dish",
                Price = 100, // Исходная цена
                Quantity = 2,
                ImageUrl = "test.jpg"
            };

            var result = await cartService.AddToCart(basketId, request);

            // Цена должна быть искажена (Price + Quantity вместо Price)
            // БАГ: ApplyBugsToRequest меняет Price = 100 + 2 = 102
            var itemInDb = await Context.CartItems
                .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == request.DishId);

            Assert.NotNull(itemInDb);
            Assert.Equal(request.Price + request.Quantity, itemInDb.Price); // 102 вместо 100

            // Проверяем что в ответе тоже искаженная цена
            Assert.Equal(request.Price + request.Quantity, result.Items?.FirstOrDefault()?.Price);
        }

        [Fact]
        public async Task GetCart_WhenBug2Enabled_TotalCalculationIncorrect()
        {
            var flags = new FeatureFlags
            {
                EnableCalculationBug = true,
                EnableResponseBug = false
            };

            var basketId = "test-basket-total-bug";
            var cartService = CreateCartService(flags);

            // Добавляем 2 товара
            var request1 = new AddToCartRequest
            {
                DishId = Guid.NewGuid(),
                Name = "Dish 1",
                Price = 100,
                Quantity = 1,
                ImageUrl = "1.jpg"
            };

            var request2 = new AddToCartRequest
            {
                DishId = Guid.NewGuid(),
                Name = "Dish 2",
                Price = 200,
                Quantity = 2,
                ImageUrl = "2.jpg"
            };

            await cartService.AddToCart(basketId, request1);
            await cartService.AddToCart(basketId, request2);

            var result = await cartService.GetCart(basketId);

            // Сумма должна быть рассчитана с учетом бага
            // Без бага: (100 * 1) + (200 * 2) = 500
            // С багом: ((100+1) * 1) + ((200+2) * 2) = 101 + 404 = 505
            var expectedTotal = (request1.Price + request1.Quantity) * request1.Quantity +
                              (request2.Price + request2.Quantity) * request2.Quantity;

            Assert.Equal(expectedTotal, result.Total);
        }

        [Fact]
        public async Task GetCartSummary_WhenResponseBugEnabled_TotalIncreasedBy10Percent()
        {
            // Включаем баг ответа (тоже про подсчет)
            var flags = new FeatureFlags
            {
                EnableResponseBug = true, // +10% к итогу
                EnableCalculationBug = false
            };

            var basketId = "test-basket-response-bug";
            var cartService = CreateCartService(flags);

            var request = new AddToCartRequest
            {
                DishId = Guid.NewGuid(),
                Name = "Test",
                Price = 100,
                Quantity = 3,
                ImageUrl = "test.jpg"
            };

            await cartService.AddToCart(basketId, request);

            var result = await cartService.GetCartSummary(basketId);

            // Итог увеличен на 10%
            var expectedTotal = 100 * 3; // 300
            var expectedWithBug = expectedTotal * 1.1m; // 330

            Assert.Equal(expectedWithBug, result.Total);
        }
    }
}
using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class CartServiceBug3Tests : CartServiceTestBase
    {
        [Fact]
        public async Task AddToCart_WhenBug3Enabled_QuantityDoesNotChange()
        {
            // вкл баг 3
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnAdd = true, // БАГ 3 вкл
                EnableCalculationBug = false,
                EnableResponseBug = false,
                EnableJavaIntegration = false
            };

            // Добавляем существующий товар в корзину
            var basketId = "test-basket-1";
            var existingItem = await AddTestItemToCart(basketId);
            var initialQuantity = existingItem.Quantity; // 2

            var cartService = CreateCartService(flags);

            var request = new AddToCartRequest
            {
                DishId = existingItem.DishId,
                Name = "Test Dish",
                Price = 100,
                Quantity = 3, // Пытаемся добавить еще 3
                ImageUrl = "test.jpg"
            };

            var result = await cartService.AddToCart(basketId, request);

            // Количество не должно измениться 
            var itemInDb = await Context.CartItems
                .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == existingItem.DishId);

            Assert.NotNull(itemInDb);
            Assert.Equal(initialQuantity, itemInDb.Quantity); // Осталось 2, не 5
            Assert.True(result.Success);

            // Проверяем что в ответе тоже правильное количество
            Assert.Equal(initialQuantity, result.Items?.FirstOrDefault()?.Quantity);
        }

        [Fact]
        public async Task AddToCart_WhenBug3Disabled_QuantityChangesCorrectly()
        {
            // выкл БАГ 3
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnAdd = false, // БАГ 3 выкл
                EnableCalculationBug = false,
                EnableResponseBug = false
            };

            var basketId = "test-basket-2";
            var existingItem = await AddTestItemToCart(basketId);
            var initialQuantity = existingItem.Quantity; // 2

            var cartService = CreateCartService(flags);

            var request = new AddToCartRequest
            {
                DishId = existingItem.DishId,
                Name = "Test Dish",
                Price = 100,
                Quantity = 3,
                ImageUrl = "test.jpg"
            };

            var result = await cartService.AddToCart(basketId, request);

            // Количество ДОЛЖНО измениться баг соот-но не работает
            var itemInDb = await Context.CartItems
                .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == existingItem.DishId);

            Assert.NotNull(itemInDb);
            Assert.Equal(initialQuantity + request.Quantity, itemInDb.Quantity); // 2 + 3 = 5
            Assert.True(result.Success);
        }

        [Fact]
        public async Task AddToCart_WhenBug3Enabled_NewItemStillAdded()
        {
            // Проверяем что баг работает ТОЛЬКО для существующих товаров
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnAdd = true, // БАГ 3 ВКЛ
                EnableCalculationBug = false
            };

            var basketId = "test-basket-3";
            var cartService = CreateCartService(flags);

            var request = new AddToCartRequest
            {
                DishId = Guid.NewGuid(), // Новый товар
                Name = "New Dish",
                Price = 200,
                Quantity = 1,
                ImageUrl = "new.jpg"
            };

            var result = await cartService.AddToCart(basketId, request);

            // Assert: Новый товар ДОЛЖЕН добавиться
            var itemInDb = await Context.CartItems
                .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == request.DishId);

            Assert.NotNull(itemInDb); // Товар добавлен
            Assert.Equal(request.Quantity, itemInDb.Quantity); // Количество правильное
            Assert.True(result.Success);
        }
    }
}
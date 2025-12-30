using hitsApplication.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class CartServiceBug4Tests : CartServiceTestBase
    {
        [Fact]
        public async Task RemoveFromCart_WhenBug4Enabled_ItemNotRemoved()
        {
            // Вкл БАГ 4
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnRemove = true, // БАГ 4 вкл
                EnableResponseBug = false
            };

            var basketId = "test-basket-4";
            var itemToRemove = await AddTestItemToCart(basketId);

            // Подсчитываем сколько товаров было
            var initialCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            var cartService = CreateCartService(flags);

            var result = await cartService.RemoveFromCart(basketId, itemToRemove.DishId.ToString());

            // Товар не должен удалиться
            var finalCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(initialCount, finalCount); // Количество не изменилось
            Assert.True(result.Success);

            // проверка что товар все еще существует
            var itemStillExists = await Context.CartItems
                .AnyAsync(x => x.Id == itemToRemove.Id);
            Assert.True(itemStillExists);
        }

        [Fact]
        public async Task RemoveFromCart_WhenBug4Disabled_ItemRemoved()
        {
            // выкл БАГ 4
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnRemove = false, // БАГ 4 выкл
                EnableResponseBug = false
            };

            var basketId = "test-basket-5";
            var itemToRemove = await AddTestItemToCart(basketId);

            var initialCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            var cartService = CreateCartService(flags);

            var result = await cartService.RemoveFromCart(basketId, itemToRemove.DishId.ToString());

            // Товар ДОЛЖЕН удалиться
            var finalCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(initialCount - 1, finalCount); // На 1 меньше
            Assert.True(result.Success);

            // Проверяем что товара больше нет
            var itemStillExists = await Context.CartItems
                .AnyAsync(x => x.Id == itemToRemove.Id);
            Assert.False(itemStillExists);
        }

        [Fact]
        public async Task RemoveFromCart_WhenItemNotFound_ReturnsError()
        {
            var flags = new FeatureFlags
            {
                NoQuantityChangeOnRemove = true // БАГ вкл, но не важен
            };

            var cartService = CreateCartService(flags);

            // Пытаемся удалить несуществующий товар
            var result = await cartService.RemoveFromCart("non-existent-basket", Guid.NewGuid().ToString());

            // Должна быть ошибка
            Assert.False(result.Success);
            Assert.Contains("не найден", result.ErrorMessage);
        }
    }
}
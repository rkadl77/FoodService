using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class CartServiceIntegrationTests : CartServiceTestBase
    {
        [Theory]
        [InlineData(true, false, false, false, "Только баг 1")]
        [InlineData(false, true, false, false, "Только баг 3")]
        [InlineData(false, false, true, false, "Только баг 4")]
        [InlineData(false, false, false, true, "Только баг 5")]
        [InlineData(true, true, true, true, "Все баги")]
        [InlineData(false, false, false, false, "Без багов")]
        public async Task TestMultipleBugScenarios(
            bool breakOrderCreation,
            bool noQuantityChangeOnAdd,
            bool noQuantityChangeOnRemove,
            bool noCartClearAfterOrder,
            string scenario)
        {
            // Arrange
            Console.WriteLine($"Тестируем сценарий: {scenario}");

            var flags = new FeatureFlags
            {
                BreakOrderCreation = breakOrderCreation,
                NoQuantityChangeOnAdd = noQuantityChangeOnAdd,
                NoQuantityChangeOnRemove = noQuantityChangeOnRemove,
                NoCartClearAfterOrder = noCartClearAfterOrder,
                EnableJavaIntegration = !breakOrderCreation, // Если баг 1 включен, интеграция все равно включена
                EnableCalculationBug = false,
                EnableResponseBug = false
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer test-token";
            HttpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            // Настраиваем HttpClient если баг 1 выключен
            if (!breakOrderCreation)
            {
                SetupHttpClientForSuccess();
            }

            var basketId = $"integration-basket-{Guid.NewGuid()}";
            var cartService = CreateCartService(flags);

            // Тест 1: Добавление товара
            var dishId = Guid.NewGuid();
            var addRequest = new AddToCartRequest
            {
                DishId = dishId,
                Name = "Integration Dish",
                Price = 100,
                Quantity = 2,
                ImageUrl = "int.jpg"
            };

            // Добавляем товар
            var addResult = await cartService.AddToCart(basketId, addRequest);
            Assert.True(addResult.Success);

            // Добавляем тот же товар еще раз
            var addAgainResult = await cartService.AddToCart(basketId, addRequest);
            Assert.True(addAgainResult.Success);

            // Проверяем баг 3
            var item = await Context.CartItems
                .FirstOrDefaultAsync(x => x.BasketId == basketId && x.DishId == dishId);

            if (noQuantityChangeOnAdd)
            {
                // БАГ 3: количество не должно измениться
                Assert.Equal(addRequest.Quantity, item?.Quantity); // Осталось 2
            }
            else
            {
                // Без бага: количество должно увеличиться
                Assert.Equal(addRequest.Quantity * 2, item?.Quantity); // Стало 4
            }

            // Тест 3: Удаление товара
            var removeResult = await cartService.RemoveFromCart(basketId, dishId.ToString());
            Assert.True(removeResult.Success);

            // Проверяем баг 4
            var itemExists = await Context.CartItems
                .AnyAsync(x => x.BasketId == basketId && x.DishId == dishId);

            if (noQuantityChangeOnRemove)
            {
                // БАГ 4: товар не должен удалиться
                Assert.True(itemExists);
            }
            else
            {
                // Без бага: товар должен удалиться
                Assert.False(itemExists);
            }

            // Тест 4: Создание заказа (если товары еще есть)
            if (itemExists)
            {
                var orderRequest = new CreateOrderRequest
                {
                    PhoneNumber = "79991234567",
                    Address = "Integration Test",
                    PaymentMethod = "CARD_ONLINE"
                };

                var orderResult = await cartService.CreateOrderFromCart(
                    basketId, Guid.NewGuid().ToString(), orderRequest);

                // Проверяем баги 1 и 5
                if (breakOrderCreation)
                {
                    // БАГ 1: заказ должен упасть
                    Assert.False(orderResult.Success);
                }
                else
                {
                    // Без бага 1: заказ может успешно создаться
                    // Проверяем баг 5
                    var itemsAfterOrder = await Context.CartItems
                        .Where(x => x.BasketId == basketId)
                        .CountAsync();

                    if (noCartClearAfterOrder)
                    {
                        // БАГ 5: корзина не очистилась
                        Assert.True(itemsAfterOrder > 0);
                    }
                    else
                    {
                        // Без бага: корзина очистилась
                        Assert.Equal(0, itemsAfterOrder);
                    }
                }
            }

            Console.WriteLine($"Сценарий {scenario} завершен успешно");
        }

        private void SetupHttpClientForSuccess()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            HttpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
        }
    }
}
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
    public class CartServiceBug5Tests : CartServiceTestBase
    {
        private void SetupHttpClientForSuccess()
        {
            // Настраиваем HttpClient для успешного ответа от order-service
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"success\":true,\"orderId\":\"test-order-123\"}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            HttpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);
        }

        private void SetupHttpContextWithToken()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer test-token-123";

            HttpContextAccessorMock
                .Setup(x => x.HttpContext)
                .Returns(httpContext);
        }

        [Fact]
        public async Task CreateOrderFromCart_WhenBug5Enabled_CartNotCleared()
        {
            // вкл БАГ 5
            var flags = new FeatureFlags
            {
                NoCartClearAfterOrder = true, // БАГ 5 вкл
                BreakOrderCreation = false, // Отключаем баг 1
                EnableJavaIntegration = true,
                EnableCalculationBug = false
            };

            SetupHttpClientForSuccess();
            SetupHttpContextWithToken();

            var basketId = "test-basket-bug5";

            await AddTestItemToCart(basketId);
            await AddTestItemToCart(basketId, Guid.NewGuid());

            var initialCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test Address, 123",
                PaymentMethod = "CARD_ONLINE",
                Comment = "Test order"
            };

            var userId = Guid.NewGuid().ToString();

            var result = await cartService.CreateOrderFromCart(basketId, userId, request);

            // Корзина НЕ должна очиститься
            var finalCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(initialCount, finalCount); // Количество товаров не изменилось
            Assert.True(result.Success); 
            Assert.Contains("успешно создан", result.Message);

            var itemsStillExist = await Context.CartItems
                .AnyAsync(x => x.BasketId == basketId);
            Assert.True(itemsStillExist);
        }

        [Fact]
        public async Task CreateOrderFromCart_WhenBug5Disabled_CartCleared()
        {
            // Arrange: выкл БАГ 5
            var flags = new FeatureFlags
            {
                NoCartClearAfterOrder = false, // БАГ 5 выкл
                BreakOrderCreation = false,
                EnableJavaIntegration = true
            };

            SetupHttpClientForSuccess();
            SetupHttpContextWithToken();

            var basketId = "test-basket-no-bug5";

            await AddTestItemToCart(basketId);
            await AddTestItemToCart(basketId, Guid.NewGuid());

            var initialCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(2, initialCount); 

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test Address",
                PaymentMethod = "CARD_COURIER"
            };

            var result = await cartService.CreateOrderFromCart(basketId, Guid.NewGuid().ToString(), request);

            var finalCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(0, finalCount); // Корзина пуста
            Assert.True(result.Success);
            Assert.Contains("успешно создан", result.Message);
        }

        [Fact]
        public async Task CreateOrderFromCart_WhenJavaServiceFails_CartNotClearedRegardlessOfBug5()
        {
            var flags = new FeatureFlags
            {
                NoCartClearAfterOrder = false, // БАГ вкл, но не важно
                BreakOrderCreation = false,
                EnableJavaIntegration = true
            };

            // Настраиваем HttpClient для ошибки
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("{\"error\":\"Server error\"}")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            HttpClientFactoryMock
                .Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            SetupHttpContextWithToken();

            var basketId = "test-basket-java-fail";
            await AddTestItemToCart(basketId);

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test",
                PaymentMethod = "CASH_COURIER"
            };

            var result = await cartService.CreateOrderFromCart(basketId, Guid.NewGuid().ToString(), request);

            // При ошибке Java-сервиса корзина не очищается в любом случае
            var itemsCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();

            Assert.Equal(1, itemsCount); // Товар остался
            Assert.False(result.Success); // Заказ не создан
            Assert.Contains("Ошибка при создании заказа", result.ErrorMessage);
        }
    }
}
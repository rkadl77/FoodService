using hitsApplication.Models;
using hitsApplication.Models.DTOs.Requests;
using hitsApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class CartServiceBug1Tests : CartServiceTestBase
    {
        [Fact]
        public async Task CreateOrderFromCart_WhenBug1Enabled_OrderFails()
        {
            // вкл БАГ 1
            var flags = new FeatureFlags
            {
                BreakOrderCreation = true, // БАГ 1 вкл
                EnableJavaIntegration = true,
                NoCartClearAfterOrder = false
            };

            SetupHttpContextWithToken();

            var basketId = "test-basket-bug1";
            await AddTestItemToCart(basketId);

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test",
                PaymentMethod = "CARD_ONLINE"
            };

            var result = await cartService.CreateOrderFromCart(basketId, Guid.NewGuid().ToString(), request);

            // Заказ должен упасть из-за бага
            Assert.False(result.Success);
            Assert.Contains("Ошибка при создании заказа", result.ErrorMessage);

            // Корзина не должна очиститься при ошибке
            var itemsCount = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .CountAsync();
            Assert.Equal(1, itemsCount);
        }

        [Fact]
        public async Task SendOrderToJavaService_WhenBug1Enabled_ReturnsFalse()
        {
            // Этот тест требует reflection для тестирования приватного метода
            var flags = new FeatureFlags
            {
                BreakOrderCreation = true,
                EnableJavaIntegration = true
            };

            SetupHttpContextWithToken();

            var basketId = "test-basket-private";
            var item = await AddTestItemToCart(basketId);

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test",
                PaymentMethod = "CARD_ONLINE"
            };

            var cartItems = await Context.CartItems
                .Where(x => x.BasketId == basketId)
                .ToListAsync();

            // Используем reflection для вызова приватного метода
            var method = typeof(CartService).GetMethod("SendOrderToJavaService",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var result = await (Task<bool>)method.Invoke(cartService, new object[]
            {
                basketId,
                Guid.NewGuid().ToString(),
                request,
                cartItems
            });

            // При баге должен вернуть false
            Assert.False(result);
        }

        [Fact]
        public async Task CreateOrderFromCart_WhenBug1Disabled_OrderCanSucceed()
        {
            // выкл БАГ 1
            var flags = new FeatureFlags
            {
                BreakOrderCreation = false, // БАГ 1 выкл
                EnableJavaIntegration = true,
                NoCartClearAfterOrder = false
            };

            SetupHttpClientForSuccess();
            SetupHttpContextWithToken();

            var basketId = "test-basket-no-bug1";
            await AddTestItemToCart(basketId);

            var cartService = CreateCartService(flags);

            var request = new CreateOrderRequest
            {
                PhoneNumber = "79991234567",
                Address = "Test Address",
                PaymentMethod = "CARD_ONLINE"
            };

            var result = await cartService.CreateOrderFromCart(basketId, Guid.NewGuid().ToString(), request);

            Assert.True(result.Success);
        }

        private void SetupHttpContextWithToken()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Authorization"] = "Bearer test-token";
            HttpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
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
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace hitsApplication
{
    public class CartEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CartEndpointsTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AddToCart_ReturnsSuccess()
        {
            var basketId = $"basket-{Guid.NewGuid()}";
            var request = new
            {
                dishId = Guid.NewGuid(),
                name = "Пицца",
                price = 15.5m,
                imageUrl = "/pizza.jpg",
                quantity = 2
            };

            var response = await _client.PostAsJsonAsync(
                $"/api/cart/add?basketId={basketId}", request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<CartResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(basketId, result.BasketId);
            Assert.Equal(31.0m, result.Total); 
        }
        [Fact]
        public async Task GetCart_CreatesNewBasket()
        {
            var response = await _client.GetAsync("/api/cart");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<CartResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.BasketId));
            Assert.Equal(0, result.ItemCount);
        }

        private class CartResponse
        {
            public bool Success { get; set; }
            public string BasketId { get; set; }
            public int ItemCount { get; set; }
            public decimal Total { get; set; }
        }
    }
}
using hitsApplication.Data;
using hitsApplication.Models;
using hitsApplication.Models.Entities;
using hitsApplication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace hitsApplication.Tests.Services
{
    public abstract class CartServiceTestBase : IDisposable
    {
        protected ApplicationDbContext Context { get; private set; }
        protected Mock<ILogger<CartService>> LoggerMock { get; private set; }
        protected Mock<IHttpClientFactory> HttpClientFactoryMock { get; private set; }
        protected Mock<IHttpContextAccessor> HttpContextAccessorMock { get; private set; }

        public CartServiceTestBase()
        {
            // InMemory для каждого теста
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Уникальное имя для каждого теста
                .Options;

            Context = new ApplicationDbContext(options);
            LoggerMock = new Mock<ILogger<CartService>>();
            HttpClientFactoryMock = new Mock<IHttpClientFactory>();
            HttpContextAccessorMock = new Mock<IHttpContextAccessor>();
        }

        protected BuggyFeaturesService CreateBuggyService(FeatureFlags flags)
        {
            var optionsMock = new Mock<IOptions<FeatureFlags>>();
            optionsMock.Setup(x => x.Value).Returns(flags);
            return new BuggyFeaturesService(optionsMock.Object);
        }

        protected CartService CreateCartService(FeatureFlags flags)
        {
            var buggyService = CreateBuggyService(flags);

            return new CartService(
                HttpContextAccessorMock.Object,
                LoggerMock.Object,
                HttpClientFactoryMock.Object,
                Context,
                buggyService,
                Options.Create(flags));
        }

        protected async Task<CartItem> AddTestItemToCart(string basketId, Guid? dishId = null)
        {
            var item = new CartItem
            {
                Id = Guid.NewGuid(),
                BasketId = basketId,
                DishId = dishId ?? Guid.NewGuid(),
                Name = "Test Dish",
                Price = 100,
                Quantity = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Context.CartItems.Add(item);
            await Context.SaveChangesAsync();
            return item;
        }

        public void Dispose()
        {
            Context?.Dispose();
        }
    }
}
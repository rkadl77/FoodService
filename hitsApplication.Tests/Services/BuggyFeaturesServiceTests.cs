using hitsApplication.Models;
using hitsApplication.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace hitsApplication.Tests.Services
{
    public class BuggyFeaturesServiceTests
    {
        [Fact]
        public void GetOrderServiceBugType_WhenBreakOrderCreationFalse_ReturnsNone()
        {
            var flags = new FeatureFlags { BreakOrderCreation = false };
            var optionsMock = new Mock<IOptions<FeatureFlags>>();
            optionsMock.Setup(x => x.Value).Returns(flags);
            var service = new BuggyFeaturesService(optionsMock.Object);

            var result = service.GetOrderServiceBugType();

            Assert.Equal(OrderServiceBugType.None, result);
        }

        [Fact]
        public void GetOrderServiceBugType_WhenBreakOrderCreationTrue_ReturnsBugType()
        {
            var flags = new FeatureFlags { BreakOrderCreation = true };
            var optionsMock = new Mock<IOptions<FeatureFlags>>();
            optionsMock.Setup(x => x.Value).Returns(flags);
            var service = new BuggyFeaturesService(optionsMock.Object);

            var result = service.GetOrderServiceBugType();

            Assert.NotEqual(OrderServiceBugType.None, result);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ShouldNotChangeQuantityOnAdd_ReturnsCorrectValue(bool flagValue, bool expected)
        {
            var flags = new FeatureFlags { NoQuantityChangeOnAdd = flagValue };
            var optionsMock = new Mock<IOptions<FeatureFlags>>();
            optionsMock.Setup(x => x.Value).Returns(flags);
            var service = new BuggyFeaturesService(optionsMock.Object);

            var result = service.ShouldNotChangeQuantityOnAdd();

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public void ShouldNotClearCartAfterOrder_ReturnsCorrectValue(bool flagValue, bool expected)
        {
            // Arrange
            var flags = new FeatureFlags { NoCartClearAfterOrder = flagValue };
            var optionsMock = new Mock<IOptions<FeatureFlags>>();
            optionsMock.Setup(x => x.Value).Returns(flags);
            var service = new BuggyFeaturesService(optionsMock.Object);

            // Act
            var result = service.ShouldNotClearCartAfterOrder();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
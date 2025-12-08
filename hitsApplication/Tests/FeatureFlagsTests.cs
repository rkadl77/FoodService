using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace hitsApplication
{
    public class FeatureFlagsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public FeatureFlagsTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetFeatureFlags_ReturnsFlags()
        {
            var response = await _client.GetAsync("/api/Features/flags");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<FeatureFlagsResponse>();
            Assert.NotNull(result);
            Assert.NotNull(result.BugFlags);
            Assert.NotNull(result.FeatureFlags);

            Assert.True(result.BugFlags.EnableCalculationBug == true ||
                        result.BugFlags.EnableCalculationBug == false);
        }

        [Fact]
        public async Task ToggleBug_ChangesFlag()
        {
            var flagsResponse = await _client.GetAsync("/api/Features/flags");
            var flags = await flagsResponse.Content.ReadFromJsonAsync<FeatureFlagsResponse>();
            var initialValue = flags.BugFlags.EnableCalculationBug;

            try
            {
                var toggleResponse = await _client.PostAsync(
                    $"/api/Features/bugs/calculation/toggle?enable={!initialValue}", null);

                Assert.Equal(HttpStatusCode.OK, toggleResponse.StatusCode);

                flagsResponse = await _client.GetAsync("/api/Features/flags");
                flags = await flagsResponse.Content.ReadFromJsonAsync<FeatureFlagsResponse>();

                Assert.Equal(!initialValue, flags.BugFlags.EnableCalculationBug);
            }
            finally
            {
                await _client.PostAsync(
                    $"/api/Features/bugs/calculation/toggle?enable={initialValue}", null);
            }
        }

        private class FeatureFlagsResponse
        {
            public BugFlags BugFlags { get; set; }
            public AppFeatureFlags FeatureFlags { get; set; }
        }

        private class BugFlags
        {
            public bool EnableCalculationBug { get; set; }
            public bool EnableOverflowBug { get; set; }
            public bool EnableImageUrlBug { get; set; }
            public bool EnableResponseBug { get; set; }
            public bool EnableInfoLeakBug { get; set; }
            public bool EnableValidationBug { get; set; }
        }

        private class AppFeatureFlags
        {
            public bool EnableNewCartLogic { get; set; }
            public bool EnableJavaIntegration { get; set; }
            public int CartItemLimit { get; set; }
            public string JavaServiceUrl { get; set; }
        }
    }
}
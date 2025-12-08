using hitsApplication.Models;
using hitsApplication.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace hitsApplication.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FeaturesController : ControllerBase
    {
        private readonly FeatureFlags _featureFlags;
        private readonly BuggyFeaturesService _buggyService;

        public FeaturesController(
            IOptions<FeatureFlags> featureFlags,
            BuggyFeaturesService buggyService)
        {
            _featureFlags = featureFlags.Value;
            _buggyService = buggyService;
        }

        [HttpGet("flags")]
        public IActionResult GetFeatureFlags()
        {
            return Ok(new
            {
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                CurrentTime = DateTime.UtcNow,

                BugFlags = new
                {
                    EnableCalculationBug = _featureFlags.EnableCalculationBug,
                    EnableOverflowBug = _featureFlags.EnableOverflowBug,
                    EnableImageUrlBug = _featureFlags.EnableImageUrlBug,
                    EnableResponseBug = _featureFlags.EnableResponseBug,
                    EnableInfoLeakBug = _featureFlags.EnableInfoLeakBug,
                    EnableValidationBug = _featureFlags.EnableValidationBug
                },

                FeatureFlags = new
                {
                    EnableNewCartLogic = _featureFlags.EnableNewCartLogic,
                    EnableJavaIntegration = _featureFlags.EnableJavaIntegration,
                    CartItemLimit = _featureFlags.CartItemLimit,
                    JavaServiceUrl = _featureFlags.JavaServiceUrl
                }
            });
        }

        [HttpPost("bugs/{bugName}/toggle")]
        public IActionResult ToggleBug(string bugName, [FromQuery] bool enable)
        {
            if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                return Forbid();
            }

            switch (bugName.ToLower())
            {
                case "calculation":
                    _featureFlags.EnableCalculationBug = enable;
                    break;
                case "overflow":
                    _featureFlags.EnableOverflowBug = enable;
                    break;
                case "imageurl":
                    _featureFlags.EnableImageUrlBug = enable;
                    break;
                case "response":
                    _featureFlags.EnableResponseBug = enable;
                    break;
                case "infoleak":
                    _featureFlags.EnableInfoLeakBug = enable;
                    break;
                case "validation":
                    _featureFlags.EnableValidationBug = enable;
                    break;
                default:
                    return BadRequest($"Unknown bug: {bugName}");
            }

            return Ok(new { Bug = bugName, Enabled = enable, Message = "Bug flag updated" });
        }
    }
}
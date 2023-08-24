using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using imcd_api_response_speed.Services;
using imcd_api_response_speed.Models;
using Newtonsoft.Json;

namespace imcd_api_response_speed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutomationController : Controller
    {
        private readonly CloudHubIntegrationService _cloudHubIntegration;
        private readonly OpenSearchIntegrationService _openSearchIntegration;

        public AutomationController(CloudHubIntegrationService cloudHubIntegration, OpenSearchIntegrationService openSearchIntegration)
        {
            _cloudHubIntegration = cloudHubIntegration;
            _openSearchIntegration = openSearchIntegration;
        }

        [HttpGet("automate")]
        public async Task<IActionResult> Automate()
        {
            try
            {
                // Step 1: Authenticate
                var bearerToken = await _cloudHubIntegration.Authenticate();

                if (string.IsNullOrEmpty(bearerToken))
                {
                    return StatusCode(500, "Authentication failed");
                }

                // Step 2: Get API info
                var apiInfoJson = await _cloudHubIntegration.GetApiInfoList();

                // Deserialize JSON response to ApiInfoResponseModel
                var apiInfoList = JsonConvert.DeserializeObject<List<ApiInfoResponseModel>>(apiInfoJson);

                foreach (var apiInfo in apiInfoList)
                {
                    // Step 3: Get Dashboard Stats
                    var dashboardStatsResponse = await _cloudHubIntegration.GetDashboardStats(apiInfo.domain);

                    // Index API info and dashboard stats
                    var apiInfoIndexingIsSuccessful = await _openSearchIntegration.IndexApiInfo(apiInfo);
                    var dashboardStatsIndexingIsSuccessful = await _openSearchIntegration.IndexDashboardStats(dashboardStatsResponse, apiInfo.domain);

                    if (!apiInfoIndexingIsSuccessful || !dashboardStatsIndexingIsSuccessful)
                    {
                        return StatusCode(500, "Indexing failed");
                    }
                }

                return Ok("Automation process completed successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}

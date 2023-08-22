using imcd_api_response_speed.Models;
using imcd_api_response_speed.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace imcd_api_response_speed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CloudHubIntegrationController : Controller
    {
        private readonly CloudHubIntegrationService _cloudHubIntegration;
        private readonly OpenSearchIntegrationService _openSearchIntegration;

        public CloudHubIntegrationController(CloudHubIntegrationService cloudHubIntegration, OpenSearchIntegrationService openSearchIntegration) 
        {
            _cloudHubIntegration = cloudHubIntegration;
            _openSearchIntegration = openSearchIntegration;
        }

        //1. Authenticate Controller
        [HttpGet("authenticate")]
        public async Task<IActionResult> Authenticate()
        {
            var bearerToken = await _cloudHubIntegration.Authenticate();
            return Ok(bearerToken);
        }

        //2. API Info Controller
        [HttpGet("api-info")]
        public async Task<IActionResult> GetApiInfoList()
        {
            var apiInfoJson = await _cloudHubIntegration.GetApiInfoList();
            var apiInfoList = JsonConvert.DeserializeObject<List<ApiInfoResponseModel>>(apiInfoJson);

            foreach (var apiInfo in apiInfoList)
            {
                var apiInfoIndexingIsSuccessful = await _openSearchIntegration.IndexApiInfo(apiInfo);

                if (!apiInfoIndexingIsSuccessful)
                {
                    return StatusCode(500, "Failed to index API info in OpenSearch");
                }
            }
            return Ok(apiInfoList);
        }

        //3. Dashboard Stats Controller
        [HttpGet("dashboard-stats/{appId}")]
        public async Task<IActionResult> GetDashboardStats(string appId)
        {
            var dashboardStats = await _cloudHubIntegration.GetDashboardStats(appId);

            var dashboardStatsIndexingIsSuccessful = await _openSearchIntegration.IndexDashboardStats(dashboardStats, appId);

            if (dashboardStatsIndexingIsSuccessful)
            {
                return Ok(dashboardStats);
            }
            else
            {
                return StatusCode(500, "Failed to index dashboard stats in OpenSearch");
            }
        }
    }
}

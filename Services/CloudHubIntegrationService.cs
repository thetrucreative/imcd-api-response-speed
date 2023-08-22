using imcd_api_response_speed.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace imcd_api_response_speed.Services
{
    public class CloudHubIntegrationService
    {
        //store the bearerToken across requests
        private string _bearerToken;

        //logging
        private readonly ILogger<CloudHubIntegrationService> _logger;

        //START: store cookies & login session
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient = new();

        public CloudHubIntegrationService(IConfiguration configuration, ILogger<CloudHubIntegrationService> logger)
        {
            _configuration = configuration;
            var baseUrl = _configuration["CloudHubIntegration:BaseUrl"];
            _httpClient.BaseAddress = new Uri(baseUrl);
            _logger = logger;
        }
        //END: store cookies & login session

        //1. Authenticate with CloudHub using provided username and password - stored in the appsettings.json file
        public async Task<string> Authenticate()
        {
            _logger.LogInformation("Calling Authenticate method...");
            var username = _configuration["CloudHubIntegration:Username"];
            var password = _configuration["CloudHubIntegration:Password"];

            var loginData = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var loginResponse = await _httpClient.PostAsync("/accounts/login", loginData);

            //generate the bearer token
            if (loginResponse.IsSuccessStatusCode)
            {
                var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();
                var loginResponseObject = JsonConvert.DeserializeObject<LoginResponseModel>(loginResponseBody);
                _bearerToken = loginResponseObject.access_token;
                _logger.LogInformation("Authentication successful");
                return _bearerToken;
            }
            return string.Empty;
        }

        //2. Get the list of all APIs stored in Cloudhub
        public async Task<string> GetApiInfoList() 
        {
            _logger.LogInformation("Calling GetApiInfoList method...");
            var bearerToken = _bearerToken;//fetch bearer token dynamically
            var orgId = _configuration["CloudHubIntegration:OrgId"];
            var envId = _configuration["CloudHubIntegration:EnvId"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            _httpClient.DefaultRequestHeaders.Add("X-ANYPNT-ORG-ID", orgId);
            _httpClient.DefaultRequestHeaders.Add("X-ANYPNT-ENV-ID", envId);

            // Log headers for debugging
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }

            var apiInfoResponse = await _httpClient.GetAsync("/cloudhub/api/v2/applications");
            apiInfoResponse.EnsureSuccessStatusCode();

            var apiInfoResponseBody = await apiInfoResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("GetApiInfoList method is complete.");
            return apiInfoResponseBody;
        }

        //3. Get dashboard stats of all APIs
        public async Task<DashboardStatsResponseModel> GetDashboardStats(string appId)
        {
            _logger.LogInformation("Calling GetDashboardStats method...");
            var bearerToken = _bearerToken;
            var orgId = _configuration["CloudHubIntegration:OrgId"];
            var envId = _configuration["CloudHubIntegration:EnvId"];

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            _httpClient.DefaultRequestHeaders.Add("X-ANYPNT-ORG-ID", orgId);
            _httpClient.DefaultRequestHeaders.Add("X-ANYPNT-ENV-ID", envId);

            var dashboardStatsResponse = await _httpClient.GetAsync($"/cloudhub/api/v2/applications/{appId}/dashboardStats");
            dashboardStatsResponse.EnsureSuccessStatusCode();

            var dashboardStatsResponseBody = await dashboardStatsResponse.Content.ReadAsStringAsync();
            var response = JsonConvert.DeserializeObject<DashboardStatsResponseModel>(dashboardStatsResponseBody);

            // Decode and assign CPU data
            foreach (var worker in response.workerStatistics)
            {
                if (worker.statistics.ContainsKey("cpu") && worker.statistics["cpu"] is JObject cpuData)
                {
                    var decodedCpuData = new Dictionary<long, double>();
                    foreach (var cpuItem in cpuData)
                    {
                        if (long.TryParse(cpuItem.Key, out long timestampMs))
                        {
                            decodedCpuData[timestampMs] = cpuItem.Value.Value<double>();
                        }
                    }
                    worker.CpuTimestamps = decodedCpuData;
                }
            }

            _logger.LogInformation("GetDashboardStats method is complete.");
            return response;
        }
    }
}

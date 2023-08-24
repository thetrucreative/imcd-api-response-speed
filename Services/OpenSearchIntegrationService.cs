using imcd_api_response_speed.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using imcd_api_response_speed.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Nest;

namespace imcd_api_response_speed.Services
{
    public class OpenSearchIntegrationService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenSearchIntegrationService> _logger;
        private readonly ElasticClient _elasticClient;

        public OpenSearchIntegrationService(IConfiguration configuration, ILogger<OpenSearchIntegrationService> logger, ElasticClient elasticClient)
        {
            _configuration = configuration;
            _logger = logger;
            _elasticClient = elasticClient;
        }

        //check to handle retrieval of indexed data from OS
        public IEnumerable<DashboardStatsDocumentModel> GetIndexedData()
        {
            try
            {
                var searchResponse = _elasticClient.Search<DashboardStatsDocumentModel>(s => s
                    .Index("non-prod-api-response-speed-dashboardstats") // Update with your index name
                    .Size(10000) // Adjust the size based on your needs
                    .Query(q => q.MatchAll())
                );

                if (!searchResponse.IsValid)
                {
                    // Handle Elasticsearch query errors
                    throw new Exception($"Elasticsearch query failed: {searchResponse.OriginalException?.Message}");
                }

                return searchResponse.Documents;
            }
            catch (Exception ex)
            {
                // Handle any errors that might occur during the Elasticsearch query
                throw new Exception($"An error occurred while retrieving indexed data: {ex.Message}");
            }
        }
        //end of check

        //1. Post API info to Opensearch and index accordingly
        public async Task<bool> IndexApiInfo(ApiInfoResponseModel apiInfo)
        {
            try
            {
                _logger.LogInformation("Calling IndexApiInfo method...");
                var jsonApiInfo = JsonConvert.SerializeObject(apiInfo);
                var content = new StringContent(jsonApiInfo, Encoding.UTF8, "application/json");

                var endpoint = _configuration["OpenSearch:Endpoint"];
                var nonProdIndexNameApiInfo = _configuration["OpenSearch:NonProdIndexnameApiInfo"];
                var prodIndexName = _configuration["OpenSearch:ProdIndexname"];
                var username = _configuration["OpenSearch:Username"];
                var password = _configuration["OpenSearch:Password"];

                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue
                    ("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

                //NonProd
                //Use 'prodIndexName' once ready to go live on the below line
                var indexUrl = $"{endpoint}/{nonProdIndexNameApiInfo}/_doc";
                var response = await _httpClient.PostAsync(indexUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("API info indexing successful.");
                    return true;
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"API info indexing failed with status code: {response.StatusCode}");
                    _logger.LogError($"Response content: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error indexing API info: {ex.Message}");
                return false;
            }
        }

        // 2. Post Dashboard stats info alongside the appId to Opensearch and index accordingly
        public async Task<bool> IndexDashboardStats(DashboardStatsResponseModel dashboardStats, string appId)
        {
            try
            {
                _logger.LogInformation("Calling IndexDashboardStats method...");

                // Check if dashboardStats contains data
                bool hasDashboardStatsData = dashboardStats.workerStatistics != null && dashboardStats.workerStatistics.Count > 0;

                // list to store indexed data for each worker
                var indexedDataList = new List<DashboardStatsDocumentModel>();

                // Iterate through each worker's statistics
                foreach (var worker in dashboardStats.workerStatistics)
                {
                    var cpuDataList = new List<CpuData>();

                    // Check if 'CpuTimestamps' property exists and is not null
                    if (worker.CpuTimestamps != null && worker.CpuTimestamps.Count > 0)
                    {
                        foreach (var cpuItem in worker.CpuTimestamps)
                        {
                            // Convert Unix timestamp to DateTime
                            var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(cpuItem.Key).UtcDateTime;

                            cpuDataList.Add(new CpuData { DateTime = timestamp, Value = cpuItem.Value });
                        }
                    }

                    // Create a DashboardStatsDocument object for indexing
                    var indexedData = new DashboardStatsDocumentModel
                    {
                        AppId = appId,
                        WorkerId = worker.id,
                        IpAddress = worker.ipAddress,
                        MemoryTotalMax = worker.memoryTotalMax ?? 0,
                        CpuDataList = cpuDataList
                    };

                    // Add the indexed data for this worker to the list
                    indexedDataList.Add(indexedData);
                }

                // Index the list of DashboardStatsDocument objects if dashboardStats has data
                if (hasDashboardStatsData)
                {
                    if (indexedDataList.Count > 0)
                    {
                        // Index the list of DashboardStatsDocument objects
                        var indexResponse = _elasticClient.IndexMany(indexedDataList, "non-prod-api-response-speed-dashboardstats");

                        if (!indexResponse.IsValid)
                        {
                            // Handle Elasticsearch indexing errors
                            throw new Exception($"Elasticsearch indexing failed: {indexResponse.DebugInformation}");
                        }

                        _logger.LogInformation("Dashboard stats indexing successful.");
                        return true;
                    }

                    _logger.LogInformation("No data to index for Dashboard stats.");
                    return false;
                }
                else
                {
                    _logger.LogInformation("No Dashboard stats data to index.");
                    //return indexedDataList.Count > 0; // Return whether payload data was indexed
                    return true; //bulk indexing needs to be successful
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error indexing Dashboard Stats for APIs: {ex.Message}");
                return false;
            }
        }
    }
}
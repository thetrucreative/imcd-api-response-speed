using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using imcd_api_response_speed.Models;
using imcd_api_response_speed.Services;

namespace imcd_api_response_speed.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisualizationController : Controller
    {
        private readonly OpenSearchIntegration _openSearchIntegration;
        private readonly ILogger<VisualizationController> _logger;

        public VisualizationController(OpenSearchIntegration openSearchIntegration, ILogger<VisualizationController> logger)
        {
            _openSearchIntegration = openSearchIntegration;
            _logger = logger;
        }

        [HttpGet("cpu-usage")]
        public IActionResult GetCpuUsageVisualization()
        {
            try
            {
                // Retrieve the indexed data from OpenSearch
                var indexedData = _openSearchIntegration.GetIndexedData();

                // Process the data and prepare it for visualization
                var timestamps = new List<DateTime>();
                var cpuValues = new List<double>();

                foreach (var data in indexedData)
                {
                    if (data.CpuDataList != null)
                    {
                        foreach (var cpuEntry in data.CpuDataList)
                        {
                            timestamps.Add(cpuEntry.DateTime);
                            cpuValues.Add(cpuEntry.Value);
                        }
                    }
                }

                // Pass the data to the view
                ViewBag.Timestamps = timestamps;
                ViewBag.CpuValues = cpuValues;

                return View("GetCpuUsageVisualization");
            }
            catch (Exception ex)
            {
                // Handle errors
                return StatusCode(500, new { Message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
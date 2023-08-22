using Newtonsoft.Json;

namespace imcd_api_response_speed.Models
{
    public class DashboardStatsDocumentModel
    {
        public string AppId { get; set; }
        public string WorkerId { get; set; }
        public string IpAddress { get; set; }
        public double MemoryTotalMax { get; set; }
        public List<CpuData> CpuDataList { get; set; } = new List<CpuData>();
    }

    public class CpuData
    {
        // Convert the DateTime value to Unix timestamp in milliseconds
        [JsonProperty("timestamp")]
        public long UnixTimestamp => (long)(DateTime - new DateTime(1970, 1, 1)).TotalMilliseconds;

        [JsonIgnore] // Exclude this property from JSON serialization
        public DateTime DateTime { get; set; }

        public double Value { get; set; }
    }

}

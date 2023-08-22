namespace imcd_api_response_speed.Models
{
    public class IndexedDataModel
    {
        // properties that match the OpenSearch indexed data structure
        public string AppId { get; set; }
        public string WorkerId { get; set; }
        public string IpAddress { get; set; }
        public double? MemoryTotalMax { get; set; }
        public Dictionary<DateTime, double> Cpu { get; set; }
    }
}

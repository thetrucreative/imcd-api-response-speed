namespace imcd_api_response_speed.Models
{
    public class SearchResponse
    {
        // Define properties that match the OpenSearch search response structure
        // For example:
        public SearchHits Hits { get; set; }
    }

    public class SearchHits
    {
        public List<SearchHit> Hits { get; set; }
    }

    public class SearchHit
    {
        public SearchHitSource Source { get; set; }
    }

    public class SearchHitSource
    {
        // properties that match the structure of a single search hit source
        public string AppId { get; set; }
        public string WorkerId { get; set; }
        public string IpAddress { get; set; }
        public double? MemoryTotalMax { get; set; }
        public Dictionary<DateTime, double> Cpu { get; set; }
    }
}
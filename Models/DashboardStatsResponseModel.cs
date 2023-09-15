namespace imcd_api_response_speed.Models
{
    public class DashboardStatsResponseModel
    {
        public List<WorkerStatistic>? workerStatistics { get; set; }
        public Dictionary<long, double>? CpuTimestamps { get; set; } 
    }

    public class WorkerStatistic
    {
        public string? id { get; set; }
        public Dictionary<string, object>? statistics { get; set; }
        public string? ipAddress { get; set; }
        public double? memoryTotalMax { get; set; }
        public Dictionary<long, double>? CpuTimestamps { get; set; } 

    }
}

namespace MarathonManager.API.DTOs.Result
{
    public class ResultSummaryDto
    {
        public int Id { get; set; }
        public string RunnerName { get; set; } = "";
        public string DistanceName { get; set; } = "";
        public string? CompletionTime { get; set; }
        public int? OverallRank { get; set; }
        public string Status { get; set; } = "";
    }
}

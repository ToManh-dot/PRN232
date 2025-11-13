namespace MarathonManager.API.DTOs.Result
{
    public class ResultDto
    {
        public int Id { get; set; }
        public int RegistrationId { get; set; }
        public string RunnerName { get; set; } = "";
        public string RaceName { get; set; } = "";
        public string DistanceName { get; set; } = "";
        public decimal DistanceInKm { get; set; }
        public string? CompletionTime { get; set; }
        public int? OverallRank { get; set; }
        public int? GenderRank { get; set; }
        public int? AgeCategoryRank { get; set; }
        public string Status { get; set; } = "";
    }
}

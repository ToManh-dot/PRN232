namespace MarathonManager.API.DTOs.Result
{
    public class UpdateResultDto
    {
        public string? CompletionTime { get; set; } // "hh:mm:ss"
        public int? OverallRank { get; set; }
        public int? GenderRank { get; set; }
        public int? AgeCategoryRank { get; set; }
        public string? Status { get; set; } // ví dụ: "Completed", "DNF", "Pending"
    }
}

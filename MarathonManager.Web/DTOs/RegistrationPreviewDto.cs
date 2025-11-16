namespace MarathonManager.Web.DTOs
{
    public class RegistrationPreviewDto
    {
        public int RaceId { get; set; }
        public int RaceDistanceId { get; set; }

        public string RaceName { get; set; } = null!;
        public string Location { get; set; } = null!;
        public DateTime RaceDate { get; set; }

        public string DistanceName { get; set; } = null!;
        public decimal DistanceKm { get; set; }
        public decimal Fee { get; set; }
        public DateTime StartTime { get; set; }

        public string UserFullName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
    }
}

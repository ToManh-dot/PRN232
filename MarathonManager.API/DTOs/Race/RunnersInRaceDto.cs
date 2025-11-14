namespace MarathonManager.API.DTOs.Race
{
    public class RunnersInRaceDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
    }
}

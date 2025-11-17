namespace MarathonManager.API.DTOs.Race
{
    // DTO chính: Chứa thông tin chi tiết của giải chạy VÀ
    // một danh sách các cự ly (sử dụng DTO ở trên)
    public class RaceDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime RaceDate { get; set; }
        public string ImageUrl { get; set; }
        public string Status { get; set; }

        public string OrganizerName { get; set; }

        public List<RaceDistanceDto> Distances { get; set; } = new List<RaceDistanceDto>();
    }
}
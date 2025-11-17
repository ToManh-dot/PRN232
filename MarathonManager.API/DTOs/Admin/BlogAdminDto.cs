// Trong MarathonManager.API/DTOs/Admin/BlogAdminDto.cs
namespace MarathonManager.API.DTOs.Admin
{
    public class BlogAdminDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; } 
        public string AuthorName { get; set; } 
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
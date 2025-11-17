
namespace MarathonManager.API.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }

        public DateOnly? DateOfBirth { get; set; }
        public string Gender { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
}
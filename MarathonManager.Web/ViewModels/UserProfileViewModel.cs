namespace MarathonManager.Web.ViewModels
{
    public class UserProfileViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public bool IsActive { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

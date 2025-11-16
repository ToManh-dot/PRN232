namespace MarathonManager.Web.ViewModels
{
    public class EditProfileViewModel
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Gender { get; set; }
    }
}

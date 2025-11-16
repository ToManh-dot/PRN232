namespace MarathonManager.API.DTOs.Auth
{
    public class DebugResetPasswordDto
    {
        public string Email { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}

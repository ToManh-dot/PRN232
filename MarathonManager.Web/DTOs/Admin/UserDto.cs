using System; // Cần cho DateTime?
using System.Collections.Generic;

namespace MarathonManager.Web.DTOs
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

        // THÊM 2 DÒNG NÀY
        public DateOnly? DateOfBirth { get; set; } // Use DateOnly?
        public string Gender { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
    }
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

}
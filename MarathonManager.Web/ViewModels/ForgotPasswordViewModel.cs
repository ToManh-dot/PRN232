using System.ComponentModel.DataAnnotations;

namespace MarathonManager.Web.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email đăng ký")]
        public string Email { get; set; } = string.Empty;
    }
}

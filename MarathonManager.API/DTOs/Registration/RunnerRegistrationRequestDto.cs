using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Registration
{
    public class RunnerRegistrationRequestDto
    {
        [Required(ErrorMessage = "Vui lòng chọn cự ly.")]
        [Range(1, int.MaxValue, ErrorMessage = "ID cự ly không hợp lệ.")]
        public int RaceDistanceId { get; set; }

        
    }
}
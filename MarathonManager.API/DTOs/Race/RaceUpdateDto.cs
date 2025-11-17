using System.ComponentModel.DataAnnotations;

namespace MarathonManager.API.DTOs.Race
{
    public class RaceUpdateDto
    {
        [Required]
        public int Id { get; set; } 

        [Required(ErrorMessage = "Tên giải chạy không được để trống")]
        [StringLength(200, ErrorMessage = "Tên giải chạy không quá 200 ký tự")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa điểm không được để trống")]
        public string Location { get; set; }

        [Required(ErrorMessage = "Ngày tổ chức không được để trống")]
        public DateTime RaceDate { get; set; }

        public string? ImageUrl { get; set; }
    }
}
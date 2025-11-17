using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MarathonManager.Web.DTOs
{
    public class RaceCreateDto
    {
        [Required(ErrorMessage = "Tên giải chạy là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "Địa điểm là bắt buộc")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày giờ chạy là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime RaceDate { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập khoảng cách chạy (VD: 5,10,21).")]
        [Display(Name = "Khoảng cách (cách nhau bằng dấu phẩy)")]
        public string DistancesInput { get; set; } = string.Empty;

        public string? DistancesCsv { get; set; }

        [Display(Name = "Ảnh minh họa")]
        public IFormFile? ImageFile { get; set; }
    }
}

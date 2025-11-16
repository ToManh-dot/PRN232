using MarathonManager.Web.DTOs;
using MarathonManager.Web.DTOs.Race;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class RacesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RacesController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        // GET: /Races/Create
        [HttpGet]
        public IActionResult Create() => View();

        // POST: /Races/Create  --> API: POST /api/Races  (multipart/form-data)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RaceCreateDto model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("MarathonApi");

            // Lấy JWT từ cookie
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Chuẩn hoá DistancesCsv: ưu tiên model.DistancesCsv; nếu bạn đang dùng DistancesInput thì chuyển sang CSV
            string distancesCsv = model.DistancesCsv;
            if (string.IsNullOrWhiteSpace(distancesCsv) && !string.IsNullOrWhiteSpace(model.DistancesInput))
            {
                var items = model.DistancesInput
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                distancesCsv = string.Join(",", items);
            }

            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(model.Name ?? ""), nameof(RaceCreateDto.Name));
            content.Add(new StringContent(model.Description ?? ""), nameof(RaceCreateDto.Description));
            content.Add(new StringContent(model.Location ?? ""), nameof(RaceCreateDto.Location));
            content.Add(new StringContent(model.RaceDate.ToString("o")), nameof(RaceCreateDto.RaceDate));
            if (!string.IsNullOrWhiteSpace(distancesCsv))
                content.Add(new StringContent(distancesCsv), nameof(RaceCreateDto.DistancesCsv));

            // Ảnh: field name PHẢI là "imageFile" theo API
            var file = imageFile ?? model.ImageFile; // hỗ trợ cả khi View bind vào model.ImageFile
            if (file != null && file.Length > 0)
            {
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "imageFile", file.FileName);
            }

            var response = await client.PostAsync("/api/Races", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo giải chạy thành công! Giải đang chờ Admin duyệt.";
                // Điều hướng về trang quản lý của Organizer
                return RedirectToAction("Index", "Organizer");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError(string.Empty, $"Tạo giải chạy thất bại: {error}");
            return View(model);
        }

        // GET: /Races/Detail/5  (public)
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var response = await client.GetAsync($"/api/Races/{id}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var raceDetail = JsonConvert.DeserializeObject<RaceDetailDto>(jsonString);
                return View(raceDetail);
            }

            return NotFound("Không tìm thấy giải chạy.");
        }

        [HttpGet]
        public async Task<IActionResult> Runners(int raceId)
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");

            // Lấy JWT từ cookie
            var token = _httpContextAccessor.HttpContext?.Request?.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Gọi API backend
            var response = await client.GetAsync($"/api/races/{raceId}/runners");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var runners = JsonConvert.DeserializeObject<List<RunnersInRaceDto>>(jsonString);
                return View(runners); // View sẽ nhận List<RunnersInRaceDto>
            }

            TempData["ErrorMessage"] = "Không thể lấy danh sách runner.";
            return RedirectToAction("Runners", new { id = raceId });
        }
    }
}

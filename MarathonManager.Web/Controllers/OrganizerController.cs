using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Organizer")]
    public class OrganizerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OrganizerController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            List<RaceSummaryDto> myRaces = new List<RaceSummaryDto>();
            try
            {
                var response = await client.GetAsync("api/organizer/races/my-races");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    myRaces = JsonConvert.DeserializeObject<List<RaceSummaryDto>>(json) ?? new List<RaceSummaryDto>();
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể tải danh sách giải chạy ({(int)response.StatusCode}): {errorBody}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
            }
            return View(myRaces);
        }

        public IActionResult Create()
        {
            var model = new RaceCreateDto
            {
                RaceDate = DateTime.Now.AddMonths(1)
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RaceCreateDto dto, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            if (imageFile != null)
            {
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("imageFile", "Kích thước file ảnh không được vượt quá 5MB.");
                    return View(dto);
                }
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).");
                    return View(dto);
                }
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            using var content = new MultipartFormDataContent();

            content.Add(new StringContent(dto.Name ?? ""), nameof(RaceCreateDto.Name));
            content.Add(new StringContent(dto.Description ?? ""), nameof(RaceCreateDto.Description));
            content.Add(new StringContent(dto.Location ?? ""), nameof(RaceCreateDto.Location));
            content.Add(new StringContent(dto.RaceDate.ToString("o")), nameof(RaceCreateDto.RaceDate));
            if (!string.IsNullOrWhiteSpace(dto.DistancesCsv))
                content.Add(new StringContent(dto.DistancesCsv), nameof(RaceCreateDto.DistancesCsv));

            if (imageFile != null && imageFile.Length > 0)
            {
                var stream = imageFile.OpenReadStream();
                var fileContent = new StreamContent(stream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);
                content.Add(fileContent, "imageFile", imageFile.FileName);
            }

            HttpResponseMessage response = null;
            try
            {
                response = await client.PostAsync("api/organizer/races", content);
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi kết nối đến API: " + ex.Message);
                return View(dto);
            }

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Tạo giải chạy thành công! Giải đang chờ Admin duyệt.";
                return RedirectToAction("Index");
            }
            else
            {
                string errorMsg = "Tạo giải chạy thất bại.";
                try
                {
                    var errorJson = await response.Content.ReadAsStringAsync();
                    var validationErrors = JsonConvert.DeserializeObject<ValidationProblemDetails>(errorJson);
                    if (validationErrors != null && validationErrors.Errors.Any())
                    {
                        errorMsg = "Vui lòng kiểm tra lại thông tin nhập.";
                        foreach (var error in validationErrors.Errors)
                        {
                            ModelState.AddModelError(error.Key, string.Join("; ", error.Value));
                        }
                    }
                    else
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(errorJson);
                        errorMsg += $" Lý do: {errorObj?.message ?? errorJson}";
                    }
                }
                catch { }

                ModelState.AddModelError(string.Empty, errorMsg);
                return View(dto);
            }
        }

        public async Task<IActionResult> ManageDistances(int raceId)
        {
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            List<RaceDistanceDto> distances = new List<RaceDistanceDto>();
            try
            {
                var response = await client.GetAsync($"/api/organizer/races/{raceId}/distances");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    distances = JsonConvert.DeserializeObject<List<RaceDistanceDto>>(json) ?? new List<RaceDistanceDto>();
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Không thể tải danh sách cự ly ({(int)response.StatusCode}): {errorBody}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
                return RedirectToAction("Index");
            }

            ViewBag.RaceId = raceId;
            return View(distances);
        }

        public IActionResult AddDistance(int raceId)
        {
            if (raceId <= 0) return BadRequest("Race ID không hợp lệ.");
            var model = new RaceDistanceCreateDto
            {
                RaceId = raceId,
                StartTime = DateTime.Now.Date.AddHours(6)
            };
            ViewBag.RaceId = raceId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDistance(RaceDistanceCreateDto dto)
        {
            if (dto.RaceId <= 0) ModelState.AddModelError(nameof(dto.RaceId), "Race ID không hợp lệ.");
            if (!ModelState.IsValid)
            {
                ViewBag.RaceId = dto.RaceId;
                return View(dto);
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
            try
            {
                response = await client.PostAsync($"/api/organizer/races/{dto.RaceId}/distances", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Thêm cự ly thành công!";
                    return RedirectToAction("ManageDistances", new { raceId = dto.RaceId });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Thêm cự ly thất bại ({(int)response.StatusCode}): {errorBody}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi kết nối hoặc xử lý: " + ex.Message);
            }

            ViewBag.RaceId = dto.RaceId;
            return View(dto);
        }

        public async Task<IActionResult> EditDistance(int distanceId, int raceId)
        {
            if (distanceId <= 0 || raceId <= 0) return BadRequest("ID không hợp lệ.");
            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            RaceDistanceDto distanceDto = null;
            try
            {
                var response = await client.GetAsync($"/api/distances/{distanceId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    distanceDto = JsonConvert.DeserializeObject<RaceDistanceDto>(json);
                    if (distanceDto == null) throw new Exception("Không thể đọc dữ liệu cự ly.");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy cự ly hoặc bạn không có quyền sửa.";
                    return RedirectToAction("ManageDistances", new { raceId = raceId });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Lỗi API ({(int)response.StatusCode}): {errorBody}");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải cự ly để sửa: " + ex.Message;
                return RedirectToAction("ManageDistances", new { raceId = raceId });
            }

            var model = new RaceDistanceUpdateDto
            {
                Id = distanceDto.Id,
                RaceId = raceId,
                Name = distanceDto.Name,
                DistanceInKm = distanceDto.DistanceInKm,
                RegistrationFee = distanceDto.RegistrationFee,
                MaxParticipants = distanceDto.MaxParticipants,
                StartTime = distanceDto.StartTime
            };
            ViewBag.RaceId = raceId;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDistance(RaceDistanceUpdateDto dto)
        {
            if (dto.Id <= 0 || dto.RaceId <= 0) ModelState.AddModelError(string.Empty, "ID không hợp lệ.");
            if (!ModelState.IsValid)
            {
                ViewBag.RaceId = dto.RaceId;
                return View(dto);
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var jsonContent = new StringContent(JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");
            HttpResponseMessage response = null;
            try
            {
                response = await client.PutAsync($"/api/Races/{dto.RaceId}/distances/{dto.Id}", jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Cập nhật cự ly thành công!";
                    return RedirectToAction("ManageDistances", new { raceId = dto.RaceId });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Cập nhật thất bại ({(int)response.StatusCode}): {errorBody}");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Lỗi kết nối hoặc xử lý: " + ex.Message);
            }

            ViewBag.RaceId = dto.RaceId;
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDistance(int distanceId, int raceId)
        {
            if (distanceId <= 0 || raceId <= 0)
            {
                TempData["ErrorMessage"] = "ID không hợp lệ.";
                return RedirectToAction("Index");
            }

            var client = CreateAuthenticatedHttpClient();
            if (client == null) return RedirectToAction("Login", "Account");

            HttpResponseMessage response = null;
            try
            {
                response = await client.DeleteAsync($"/api/Races/{raceId}/distances/{distanceId}");
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Xóa cự ly thành công!";
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Xóa thất bại ({(int)response.StatusCode}): {errorBody}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi kết nối hoặc xử lý: " + ex.Message;
            }

            return RedirectToAction("ManageDistances", new { raceId = raceId });
        }

        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            try
            {
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];

                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                client.DefaultRequestHeaders.Authorization = null;
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                return client;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating authenticated HttpClient: {ex.Message}");
                return null;
            }
        }
    }
}

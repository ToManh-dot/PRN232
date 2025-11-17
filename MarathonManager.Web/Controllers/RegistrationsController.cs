using MarathonManager.Web.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace MarathonManager.Web.Controllers
{
    [Authorize] 
    public class RegistrationsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RegistrationsController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Authorize(Roles = "Runner")]
        public async Task<IActionResult> Register(RegistrationCreateDto dto)
        {
            var client = CreateAuthenticatedHttpClient();
            var apiDto = new { RaceDistanceId = dto.RaceDistanceId }; 
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/api/Registrations", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công!";
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                var errorDto = JsonConvert.DeserializeObject<ApiErrorDto>(errorBody);
                TempData["ErrorMessage"] = "Đăng ký thất bại: " + errorDto?.Message;
            }

            return RedirectToAction("Detail", "Races", new { id = dto.RaceId });
        }


        private HttpClient CreateAuthenticatedHttpClient()
        {
            var client = _httpClientFactory.CreateClient("MarathonApi");
            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("Không tìm thấy Token.");
            }
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            return client;
        }


        [Authorize(Roles = "Runner")]
        public async Task<IActionResult> Preview(int raceId, int distanceId)
        {
            var client = CreateAuthenticatedHttpClient();

            var response = await client.GetAsync($"/api/RaceDistances/{distanceId}");
            if (!response.IsSuccessStatusCode) return NotFound();

            var json = await response.Content.ReadAsStringAsync();
            var distanceDto = JsonConvert.DeserializeObject<RaceDistanceDto>(json);

            var user = _httpContextAccessor.HttpContext.User;
            var preview = new RegistrationPreviewDto
            {
                RaceId = raceId,
                RaceDistanceId = distanceId,
                RaceName = distanceDto!.RaceName,
                Location = distanceDto.RaceLocation,
                RaceDate = distanceDto.RaceDate,
                DistanceName = distanceDto.Name,
                DistanceKm = distanceDto.DistanceInKm,
                Fee = distanceDto.RegistrationFee,
                StartTime = distanceDto.StartTime,
                UserFullName = user.FindFirst(ClaimTypes.Name)?.Value ?? "",
                UserEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? ""
            };

            return View(preview);
        }

        [HttpPost]
        [Authorize(Roles = "Runner")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayRegistration(int registrationId)
        {
            var client = CreateAuthenticatedHttpClient();

            var response = await client.PostAsync($"/api/Registrations/{registrationId}/pay", null);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonConvert.DeserializeObject<RegistrationPayResponseDto>(responseBody);
                TempData["SuccessMessage"] = result?.Message ?? "Thanh toán thành công!";

                int raceId = 0; 
                return RedirectToAction("Index", "Runner", new { tab = "my-registrations" });
            }
            else
            {
                var errorDto = JsonConvert.DeserializeObject<ApiErrorDto>(responseBody);
                TempData["ErrorMessage"] = "Thanh toán thất bại: " + errorDto?.Message;
                return RedirectToAction("Preview", new { raceId = 0, distanceId = registrationId }); 
            }
        }

        private class RegistrationPayResponseDto
        {
            public string Message { get; set; } = string.Empty;
            public int RegistrationId { get; set; }
        }

        private class RaceDistanceDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = null!;
            public decimal DistanceInKm { get; set; }
            public decimal RegistrationFee { get; set; }
            public DateTime StartTime { get; set; }
            public string RaceName { get; set; } = null!;
            public string RaceLocation { get; set; } = null!;
            public DateTime RaceDate { get; set; }
        }




        private class ApiErrorDto { public string Message { get; set; } }

     
    }
}
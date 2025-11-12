using MarathonManager.API.DTOs;
using MarathonManager.Web.DTOs;
using System.Net.Http.Headers;

namespace MarathonManager.Web.Services
{
    /// <summary>
    /// Giao tiếp giữa Web và MarathonManager.Api dành cho Runner
    /// </summary>
    public class RunnerApiService : IRunnerApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RunnerApiService> _logger;

        public RunnerApiService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<RunnerApiService> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        private void SetAuthorizationHeader()
        {
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<ApiResponseDto<RunnerDashboardDto>> GetDashboardAsync()
            => await GetAsync<RunnerDashboardDto>("/api/runner/dashboard");

        public async Task<ApiResponseDto<PaginatedResponseDto<AvailableRaceDto>>> GetAvailableRacesAsync(int pageNumber = 1, int pageSize = 6)
            => await GetAsync<PaginatedResponseDto<AvailableRaceDto>>($"/api/runner/available?pageNumber={pageNumber}&pageSize={pageSize}");

        public async Task<ApiResponseDto<RaceDetailDto>> GetRaceDetailsAsync(int raceId)
            => await GetAsync<RaceDetailDto>($"/api/runner/races/{raceId}/details");

        public async Task<ApiResponseDto<MyRegistrationDto>> RegisterForRaceAsync(RegisterForRaceRequest request)
            => await PostAsync<MyRegistrationDto>("/api/runner/register", request);

        public async Task<ApiResponseDto<PaginatedResponseDto<MyRegistrationDto>>> GetMyRegistrationsAsync(int pageNumber = 1, int pageSize = 10)
            => await GetAsync<PaginatedResponseDto<MyRegistrationDto>>($"/api/runner/registrations?pageNumber={pageNumber}&pageSize={pageSize}");

        public async Task<ApiResponseDto<object>> CancelRegistrationAsync(int registrationId)
            => await DeleteAsync<object>($"/api/runner/registrations/{registrationId}");

        public async Task<ApiResponseDto<PaginatedResponseDto<MyResultDto>>> GetMyResultsAsync(int pageNumber = 1, int pageSize = 10)
            => await GetAsync<PaginatedResponseDto<MyResultDto>>($"/api/runner/results?pageNumber={pageNumber}&pageSize={pageSize}");

        // === Generic helper methods ===

        private async Task<ApiResponseDto<T>> GetAsync<T>(string url)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.GetAsync(url);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GET {Url}", url);
                return new ApiResponseDto<T> { Success = false, Message = ex.Message };
            }
        }

        private async Task<ApiResponseDto<T>> PostAsync<T>(string url, object payload)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.PostAsJsonAsync(url, payload);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling POST {Url}", url);
                return new ApiResponseDto<T> { Success = false, Message = ex.Message };
            }
        }

        private async Task<ApiResponseDto<T>> DeleteAsync<T>(string url)
        {
            try
            {
                SetAuthorizationHeader();
                var response = await _httpClient.DeleteAsync(url);
                return await HandleResponse<T>(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling DELETE {Url}", url);
                return new ApiResponseDto<T> { Success = false, Message = ex.Message };
            }
        }

        private static async Task<ApiResponseDto<T>> HandleResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponseDto<T>>();
                return result ?? new ApiResponseDto<T> { Success = false, Message = "Failed to parse response" };
            }

            var errorText = await response.Content.ReadAsStringAsync();
            return new ApiResponseDto<T> { Success = false, Message = $"Error {response.StatusCode}: {errorText}" };
        }
    }
}

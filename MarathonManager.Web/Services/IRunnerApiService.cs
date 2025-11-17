using MarathonManager.API.DTOs;
using MarathonManager.Web.DTOs;

namespace MarathonManager.Web.Services
{
    public interface IRunnerApiService
    {
        Task<ApiResponseDto<RunnerDashboardDto>> GetDashboardAsync();
        Task<ApiResponseDto<PaginatedResponseDto<AvailableRaceDto>>> GetAvailableRacesAsync(int pageNumber = 1, int pageSize = 6);
        Task<ApiResponseDto<RaceDetailsDto>> GetRaceDetailsAsync(int raceId);
        Task<ApiResponseDto<MyRegistrationDto>> RegisterForRaceAsync(RegisterForRaceRequest request);
        Task<ApiResponseDto<PaginatedResponseDto<MyRegistrationDto>>> GetMyRegistrationsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponseDto<object>> CancelRegistrationAsync(int registrationId);
        Task<ApiResponseDto<PaginatedResponseDto<MyResultDto>>> GetMyResultsAsync(int pageNumber = 1, int pageSize = 10);
        Task<ApiResponseDto<CreateVnPayPaymentResponseDto>> CreateVnPayPaymentUrlAsync(int registrationId);
        Task<ApiResponseDto<object>> ConfirmPaymentAsync(int registrationId, string paymentMethod, string? transactionNo);

    }
}

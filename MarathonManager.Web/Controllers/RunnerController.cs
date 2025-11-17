using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MarathonManager.Web.Services;
using MarathonManager.API.DTOs;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace MarathonManager.Web.Controllers
{
    [Authorize(Roles = "Runner")]
    public class RunnerController : Controller
    {
        private readonly IRunnerApiService _runnerApiService;
        private readonly ILogger<RunnerController> _logger;
        private readonly IConfiguration _configuration;

        public RunnerController(
            IRunnerApiService runnerApiService,
            ILogger<RunnerController> logger,
            IConfiguration configuration)
        {
            _runnerApiService = runnerApiService;
            _logger = logger;
            _configuration = configuration;
        }

        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }


        public async Task<IActionResult> Index(
            string? tab,
            int? pageAvailableRaces,
            int? pageMyRegistrations,
            int? pageMyResults)
        {
            try
            {
                ViewBag.CurrentTab = tab ?? "available-races";

                var dashboardResponse = await _runnerApiService.GetDashboardAsync();
                if (!dashboardResponse.Success || dashboardResponse.Data == null)
                {
                    ViewBag.Error = dashboardResponse.Message ?? "Failed to load dashboard";
                    return View(new RunnerDashboardViewModel());
                }

                var viewModel = new RunnerDashboardViewModel
                {
                    Statistics = dashboardResponse.Data.Statistics
                };

                int pageNumAvailable = pageAvailableRaces ?? 1;
                var availableRacesResponse = await _runnerApiService.GetAvailableRacesAsync(pageNumAvailable, 6);
                if (availableRacesResponse.Success && availableRacesResponse.Data != null)
                {
                    viewModel.AvailableRaces = availableRacesResponse.Data.Items;
                    viewModel.AvailableRacesPageNumber = availableRacesResponse.Data.PageNumber;
                    viewModel.AvailableRacesPageSize = availableRacesResponse.Data.PageSize;
                    viewModel.AvailableRacesTotalCount = availableRacesResponse.Data.TotalCount;
                    viewModel.AvailableRacesTotalPages = availableRacesResponse.Data.TotalPages;
                }

                int pageNumRegistrations = pageMyRegistrations ?? 1;
                var registrationsResponse = await _runnerApiService.GetMyRegistrationsAsync(pageNumRegistrations, 10);
                if (registrationsResponse.Success && registrationsResponse.Data != null)
                {
                    viewModel.MyRegistrations = registrationsResponse.Data.Items;
                    viewModel.MyRegistrationsPageNumber = registrationsResponse.Data.PageNumber;
                    viewModel.MyRegistrationsPageSize = registrationsResponse.Data.PageSize;
                    viewModel.MyRegistrationsTotalCount = registrationsResponse.Data.TotalCount;
                    viewModel.MyRegistrationsTotalPages = registrationsResponse.Data.TotalPages;
                }

                int pageNumResults = pageMyResults ?? 1;
                var resultsResponse = await _runnerApiService.GetMyResultsAsync(pageNumResults, 10);
                if (resultsResponse.Success && resultsResponse.Data != null)
                {
                    viewModel.MyResults = resultsResponse.Data.Items;
                    viewModel.MyResultsPageNumber = resultsResponse.Data.PageNumber;
                    viewModel.MyResultsPageSize = resultsResponse.Data.PageSize;
                    viewModel.MyResultsTotalCount = resultsResponse.Data.TotalCount;
                    viewModel.MyResultsTotalPages = resultsResponse.Data.TotalPages;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading runner dashboard");
                ViewBag.Error = "An error occurred while loading the dashboard";
                return View(new RunnerDashboardViewModel());
            }
        }

        public async Task<IActionResult> RaceDetails(int id)
        {
            try
            {
                var response = await _runnerApiService.GetRaceDetailsAsync(id);

                if (!response.Success || response.Data == null)
                {
                    TempData["ErrorMessage"] = response.Message ?? "Race not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(response.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading race details for race {RaceId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading race details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(int raceId, int raceDistanceId)
        {
            try
            {
                var request = new RegisterForRaceRequest
                {
                    RaceId = raceId,
                    RaceDistanceId = raceDistanceId
                };

                var response = await _runnerApiService.RegisterForRaceAsync(request);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = response.Message ?? "Successfully registered for the race!";
                    return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "Failed to register for the race";
                    return RedirectToAction(nameof(RaceDetails), new { id = raceId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for race {RaceId}, distance {DistanceId}", raceId, raceDistanceId);
                TempData["ErrorMessage"] = "An error occurred during registration";
                return RedirectToAction(nameof(RaceDetails), new { id = raceId });
            }
        }

        public async Task<IActionResult> RegistrationDetails(int id)
        {
            try
            {
                var registrationsResponse = await _runnerApiService.GetMyRegistrationsAsync(1, 100);

                if (!registrationsResponse.Success || registrationsResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "Failed to load registration details";
                    return RedirectToAction(nameof(Index));
                }

                var registration = registrationsResponse.Data.Items.FirstOrDefault(r => r.Id == id);

                if (registration == null)
                {
                    TempData["ErrorMessage"] = "Registration not found";
                    return RedirectToAction(nameof(Index));
                }

                var raceResponse = await _runnerApiService.GetRaceDetailsAsync(registration.RaceId);
                ViewBag.RaceDetails = raceResponse.Data;

                return View(registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading registration details for {RegistrationId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading registration details";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            try
            {
                var response = await _runnerApiService.CancelRegistrationAsync(registrationId);

                if (response.Success)
                {
                    TempData["SuccessMessage"] = response.Message ?? "Registration cancelled successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = response.Message ?? "Failed to cancel registration";
                }

                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling registration {RegistrationId}", registrationId);
                TempData["ErrorMessage"] = "An error occurred while cancelling registration";
                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
        }

        public async Task<IActionResult> ResultDetails(int id)
        {
            try
            {
                var resultsResponse = await _runnerApiService.GetMyResultsAsync(1, 100);

                if (!resultsResponse.Success || resultsResponse.Data == null)
                {
                    TempData["ErrorMessage"] = "Failed to load result details";
                    return RedirectToAction(nameof(Index));
                }

                var result = resultsResponse.Data.Items.FirstOrDefault(r => r.Id == id);

                if (result == null)
                {
                    TempData["ErrorMessage"] = "Result not found";
                    return RedirectToAction(nameof(Index));
                }

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading result details for {ResultId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading result details";
                return RedirectToAction(nameof(Index));
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithVnPay(int registrationId)
        {
            try
            {
                var response = await _runnerApiService.CreateVnPayPaymentUrlAsync(registrationId);

                if (response.Success && response.Data != null && !string.IsNullOrEmpty(response.Data.PaymentUrl))
                {
                    return Redirect(response.Data.PaymentUrl);
                }

                TempData["ErrorMessage"] = response.Message ?? "Không tạo được link thanh toán VNPAY";
                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPAY payment for registration {RegistrationId}", registrationId);
                TempData["ErrorMessage"] = "Có lỗi khi tạo thanh toán VNPAY";
                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                var vnpay = new VnPayLibrary();

                foreach (var key in Request.Query.Keys)
                {
                    var value = Request.Query[key]!;
                    if (!string.IsNullOrEmpty(value) && key.StartsWith("vnp_"))
                    {
                        vnpay.AddResponseData(key, value);
                    }
                }

                string? hashSecret = _configuration["VnPay:HashSecret"];
                if (string.IsNullOrEmpty(hashSecret))
                {
                    TempData["ErrorMessage"] = "Thiếu cấu hình HashSecret của VNPAY.";
                    return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
                }

                bool isValid = vnpay.ValidateSignature(hashSecret);
                if (!isValid)
                {
                    TempData["ErrorMessage"] = "Chữ ký VNPAY không hợp lệ.";
                    return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
                }

                string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                string txnRef = vnpay.GetResponseData("vnp_TxnRef");
                string transactionNo = vnpay.GetResponseData("vnp_TransactionNo");

                if (!int.TryParse(txnRef, out int registrationId))
                {
                    TempData["ErrorMessage"] = "Mã đơn thanh toán không hợp lệ.";
                    return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
                }

                if (responseCode == "00")
                {
                    var apiResult = await _runnerApiService
                        .ConfirmPaymentAsync(registrationId, "VNPAY", transactionNo);

                    if (apiResult.Success)
                    {
                        TempData["SuccessMessage"] = "Thanh toán VNPAY thành công.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Thanh toán thành công nhưng lỗi khi lưu hệ thống: " + apiResult.Message;
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = $"Thanh toán không thành công. Mã lỗi: {responseCode}";
                }

                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing VNPAY return");
                TempData["ErrorMessage"] = "Có lỗi khi xử lý kết quả thanh toán VNPAY.";
                return RedirectToAction(nameof(Index), new { tab = "my-registrations" });
            }
        }
    }

    public class RunnerDashboardViewModel
    {
        public RunnerStatisticsDto Statistics { get; set; } = new();

        public List<AvailableRaceDto> AvailableRaces { get; set; } = new();
        public int AvailableRacesPageNumber { get; set; } = 1;
        public int AvailableRacesPageSize { get; set; } = 6;
        public int AvailableRacesTotalCount { get; set; }
        public int AvailableRacesTotalPages { get; set; }
        public bool AvailableRacesHasPreviousPage => AvailableRacesPageNumber > 1;
        public bool AvailableRacesHasNextPage => AvailableRacesPageNumber < AvailableRacesTotalPages;

        public List<MyRegistrationDto> MyRegistrations { get; set; } = new();
        public int MyRegistrationsPageNumber { get; set; } = 1;
        public int MyRegistrationsPageSize { get; set; } = 10;
        public int MyRegistrationsTotalCount { get; set; }
        public int MyRegistrationsTotalPages { get; set; }
        public bool MyRegistrationsHasPreviousPage => MyRegistrationsPageNumber > 1;
        public bool MyRegistrationsHasNextPage => MyRegistrationsPageNumber < MyRegistrationsTotalPages;

        public List<MyResultDto> MyResults { get; set; } = new();
        public int MyResultsPageNumber { get; set; } = 1;
        public int MyResultsPageSize { get; set; } = 10;
        public int MyResultsTotalCount { get; set; }
        public int MyResultsTotalPages { get; set; }
        public bool MyResultsHasPreviousPage => MyResultsPageNumber > 1;
        public bool MyResultsHasNextPage => MyResultsPageNumber < MyResultsTotalPages;
    }
}

using MarathonManager.API.DTOs;
using MarathonManager.API.DTOs.Race;
using MarathonManager.API.DTOs.RaceDistances;
using MarathonManager.API.Models;
using MarathonManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarathonManager.API.Controllers;

[Route("api/runner")]
[ApiController]
[Authorize(Roles = "Runner")]
public class RunnerRacesController : ControllerBase
{
    private readonly MarathonManagerContext _context;
    
    private readonly IVnPayService _vnPayService;

    public RunnerRacesController(MarathonManagerContext context, IVnPayService vnPayService)
    {
        _context = context;
  
        _vnPayService = vnPayService;
    }




    // GET: api/runner/dashboard
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<RunnerDashboardDto>>> GetRunnerDashboard()
    {
        var userId = GetCurrentUserId();

        try
        {
            var allRegs = await _context.Registrations
                .Include(r => r.RaceDistance).ThenInclude(rd => rd.Race)
                .Where(r => r.RunnerId == userId)
                .ToListAsync();

            var stats = new RunnerStatisticsDto
            {
                TotalRegistrations = allRegs.Count,
                CompletedRaces = await _context.Results
                    .Where(r => r.Registration.RunnerId == userId && r.Status == "Finished")
                    .CountAsync(),
                UpcomingRaces = allRegs.Count(r => r.PaymentStatus == "Paid" && r.RaceDistance.Race.RaceDate > DateTime.Now),
                PendingRegistrations = allRegs.Count(r => r.PaymentStatus == "Pending")
            };

            return Ok(new ApiResponse<RunnerDashboardDto>
            {
                Success = true,
                Data = new RunnerDashboardDto { Statistics = stats }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<RunnerDashboardDto>
            {
                Success = false,
                Message = "An error occurred while fetching dashboard data",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // GET: api/runner/available?pageNumber=1&pageSize=6
    [HttpGet("available")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AvailableRaceDto>>>> GetAvailableRaces(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 6)
    {
        var userId = GetCurrentUserId();

        try
        {
            var registeredRaceIds = await _context.Registrations
                .Where(r => r.RunnerId == userId && r.PaymentStatus != "Cancelled")
                .Select(r => r.RaceDistance.RaceId)
                .Distinct()
                .ToListAsync();

            var q = _context.Races
                .Include(r => r.Organizer)
                .Include(r => r.RaceDistances).ThenInclude(rd => rd.Registrations)
                .Where(r => r.Status == "Approved" && r.RaceDate > DateTime.Now)
                .OrderBy(r => r.RaceDate);

            var totalCount = await q.CountAsync();
            var races = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var items = races.Select(r => new AvailableRaceDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Location = r.Location,
                RaceDate = r.RaceDate,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                OrganizerId = r.OrganizerId,
                OrganizerName = r.Organizer.FullName,
                OrganizerEmail = r.Organizer.Email,
                IsAlreadyRegistered = registeredRaceIds.Contains(r.Id),
                Distances = r.RaceDistances.Select(rd => new RaceDistanceSummaryDto
                {
                    Id = rd.Id,
                    Name = rd.Name,
                    DistanceInKm = rd.DistanceInKm,
                    RegistrationFee = rd.RegistrationFee,
                    MaxParticipants = rd.MaxParticipants,
                    StartTime = rd.StartTime,
                    CurrentParticipants = rd.Registrations.Count(x => x.PaymentStatus != "Cancelled"),
                    IsFull = rd.Registrations.Count(x => x.PaymentStatus != "Cancelled") >= rd.MaxParticipants
                }).ToList(),
                TotalParticipants = r.RaceDistances.Sum(rd => rd.Registrations.Count(x => x.PaymentStatus != "Cancelled")),
                AvailableSlots = r.RaceDistances.Sum(rd => rd.MaxParticipants - rd.Registrations.Count(x => x.PaymentStatus != "Cancelled"))
            }).ToList();

            var resp = new PaginatedResponse<AvailableRaceDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(new ApiResponse<PaginatedResponse<AvailableRaceDto>> { Success = true, Data = resp });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PaginatedResponse<AvailableRaceDto>>
            {
                Success = false,
                Message = "An error occurred while fetching available races",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // GET: api/runner/races/{id}/details
    [HttpGet("races/{id:int}/details")]
    public async Task<ActionResult<ApiResponse<RaceDetailsDto>>> GetRaceDetails(int id)
    {
        try
        {
            var race = await _context.Races
                .Include(r => r.Organizer)
                .Include(r => r.RaceDistances).ThenInclude(rd => rd.Registrations)
                .Include(r => r.BlogPosts.Where(bp => bp.Status == "Published")).ThenInclude(bp => bp.Author)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (race == null)
                return NotFound(new ApiResponse<RaceDetailsDto> { Success = false, Message = "Race not found" });

            var detail = new RaceDetailsDto
            {
                Id = race.Id,
                Name = race.Name,
                Description = race.Description,
                Location = race.Location,
                RaceDate = race.RaceDate,
                ImageUrl = race.ImageUrl,
                Status = race.Status,
                CreatedAt = race.CreatedAt,
                OrganizerId = race.OrganizerId,
                OrganizerName = race.Organizer.FullName,
                OrganizerEmail = race.Organizer.Email,
                OrganizerPhone = race.Organizer.PhoneNumber,
                Distances = race.RaceDistances.Select(rd => new RaceDistanceDetailDto
                {
                    Id = rd.Id,
                    RaceId = rd.RaceId,
                    Name = rd.Name,
                    DistanceInKm = rd.DistanceInKm,
                    RegistrationFee = rd.RegistrationFee,
                    MaxParticipants = rd.MaxParticipants,
                    StartTime = rd.StartTime,
                    CurrentParticipants = rd.Registrations.Count(r => r.PaymentStatus != "Cancelled")
                }).ToList(),
                BlogPosts = race.BlogPosts.Select(bp => new BlogPostSummaryDto
                {
                    Id = bp.Id,
                    Title = bp.Title,
                    FeaturedImageUrl = bp.FeaturedImageUrl,
                    Status = bp.Status,
                    AuthorName = bp.Author.FullName,
                    CreatedAt = bp.CreatedAt
                }).ToList()
            };

            return Ok(new ApiResponse<RaceDetailsDto> { Success = true, Data = detail });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<RaceDetailsDto>
            {
                Success = false,
                Message = "An error occurred while fetching race details",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // GET: api/runner/registrations
    [HttpGet("registrations")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<MyRegistrationDto>>>> GetMyRegistrations(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();

        try
        {
            var q = _context.Registrations
                .Include(r => r.RaceDistance).ThenInclude(rd => rd.Race)
                .Include(r => r.Result)
                .Where(r => r.RunnerId == userId)
                .OrderByDescending(r => r.RegistrationDate);

            var total = await q.CountAsync();
            var regs = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var items = regs.Select(r => new MyRegistrationDto
            {
                Id = r.Id,
                RegistrationDate = r.RegistrationDate,
                PaymentStatus = r.PaymentStatus,
                BibNumber = r.BibNumber,
                RaceId = r.RaceDistance.RaceId,
                RaceName = r.RaceDistance.Race.Name,
                Location = r.RaceDistance.Race.Location,
                RaceDate = r.RaceDistance.Race.RaceDate,
                RaceImageUrl = r.RaceDistance.Race.ImageUrl,
                RaceDistanceId = r.RaceDistanceId,
                DistanceName = r.RaceDistance.Name,
                DistanceInKm = r.RaceDistance.DistanceInKm,
                RegistrationFee = r.RaceDistance.RegistrationFee,
                StartTime = r.RaceDistance.StartTime,
                CanCancel = r.RaceDistance.Race.RaceDate > DateTime.Now && r.PaymentStatus != "Cancelled",
                HasResult = r.Result != null,
                DisplayStatus = r.PaymentStatus switch
                {
                    "Pending" => "Pending Payment",
                    "Paid" => r.RaceDistance.Race.RaceDate > DateTime.Now ? "Confirmed" : "Completed",
                    "Cancelled" => "Cancelled",
                    _ => r.PaymentStatus
                }
            }).ToList();

            var resp = new PaginatedResponse<MyRegistrationDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(new ApiResponse<PaginatedResponse<MyRegistrationDto>> { Success = true, Data = resp });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PaginatedResponse<MyRegistrationDto>>
            {
                Success = false,
                Message = "An error occurred while fetching registrations",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // DELETE: api/runner/registrations/{id} (Hủy – giữ nguyên logic cũ)
    [HttpDelete("registrations/{id:int}")]
    public async Task<ActionResult<ApiResponse<object>>> CancelRegistration(int id)
    {
        var userId = GetCurrentUserId();

        try
        {
            var reg = await _context.Registrations
                .Include(r => r.RaceDistance).ThenInclude(rd => rd.Race)
                .FirstOrDefaultAsync(r => r.Id == id && r.RunnerId == userId);

            if (reg == null)
                return NotFound(new ApiResponse<object> { Success = false, Message = "Registration not found" });

            if (reg.RaceDistance.Race.RaceDate <= DateTime.Now)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Cannot cancel registration for a race that has already occurred" });

            if (reg.PaymentStatus == "Cancelled")
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Registration is already cancelled" });

            reg.PaymentStatus = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object> { Success = true, Message = "Registration cancelled successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while cancelling registration",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    // GET: api/runner/results
    [HttpGet("results")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<MyResultDto>>>> GetMyResults(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();

        try
        {
            var q = _context.Results
                .Include(r => r.Registration).ThenInclude(reg => reg.RaceDistance).ThenInclude(rd => rd.Race)
                .Where(r => r.Registration.RunnerId == userId)
                .OrderByDescending(r => r.Registration.RaceDistance.Race.RaceDate);

            var total = await q.CountAsync();
            var list = await q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            var items = list.Select(r => new MyResultDto
            {
                Id = r.Id,
                RegistrationId = r.RegistrationId,
                CompletionTime = r.CompletionTime,
                OverallRank = r.OverallRank,
                GenderRank = r.GenderRank,
                AgeCategoryRank = r.AgeCategoryRank,
                Status = r.Status,
                RaceId = r.Registration.RaceDistance.RaceId,
                RaceName = r.Registration.RaceDistance.Race.Name,
                Location = r.Registration.RaceDistance.Race.Location,
                RaceDate = r.Registration.RaceDistance.Race.RaceDate,
                DistanceName = r.Registration.RaceDistance.Name,
                DistanceInKm = r.Registration.RaceDistance.DistanceInKm,
                FormattedTime = r.CompletionTime?.ToString(@"hh\:mm\:ss"),
                AveragePace = r.CompletionTime.HasValue && r.Registration.RaceDistance.DistanceInKm > 0
                    ? $"{(r.CompletionTime.Value.ToTimeSpan().TotalMinutes / (double)r.Registration.RaceDistance.DistanceInKm):F2} min/km"
                    : null
            }).ToList();

            var resp = new PaginatedResponse<MyResultDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(new ApiResponse<PaginatedResponse<MyResultDto>> { Success = true, Data = resp });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<PaginatedResponse<MyResultDto>>
            {
                Success = false,
                Message = "An error occurred while fetching results",
                Errors = new List<string> { ex.Message }
            });
        }
    }
    // POST: api/runner/registrations/vnpay/create
    [HttpPost("registrations/vnpay/create")]
    public async Task<ActionResult<ApiResponse<CreateVnPayPaymentResponse>>> CreateVnPayPayment(
        [FromBody] CreateVnPayPaymentRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var registration = await _context.Registrations
                .Include(r => r.RaceDistance).ThenInclude(rd => rd.Race)
                .FirstOrDefaultAsync(r => r.Id == request.RegistrationId && r.RunnerId == userId);

            if (registration == null)
            {
                return NotFound(new ApiResponse<CreateVnPayPaymentResponse>
                {
                    Success = false,
                    Message = "Registration not found"
                });
            }

            if (registration.PaymentStatus == "Paid")
            {
                return BadRequest(new ApiResponse<CreateVnPayPaymentResponse>
                {
                    Success = false,
                    Message = "Registration already paid"
                });
            }

            if (registration.PaymentStatus == "Cancelled")
            {
                return BadRequest(new ApiResponse<CreateVnPayPaymentResponse>
                {
                    Success = false,
                    Message = "Registration has been cancelled"
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            string paymentUrl = _vnPayService.CreatePaymentUrl(registration, ipAddress);

            if (registration.PaymentStatus != "Pending")
            {
                registration.PaymentStatus = "Pending";
                await _context.SaveChangesAsync();
            }

            return Ok(new ApiResponse<CreateVnPayPaymentResponse>
            {
                Success = true,
                Data = new CreateVnPayPaymentResponse
                {
                    PaymentUrl = paymentUrl
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<CreateVnPayPaymentResponse>
            {
                Success = false,
                Message = "An error occurred while creating VNPAY payment",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    [HttpPost("registrations/{id:int}/confirm-payment")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmPayment(
     int id,
     [FromBody] ConfirmPaymentRequest request)
    {
        var userId = GetCurrentUserId();

        try
        {
            var reg = await _context.Registrations
                .FirstOrDefaultAsync(r => r.Id == id && r.RunnerId == userId);

            if (reg == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Registration not found"
                });
            }

            if (reg.PaymentStatus == "Paid")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Registration already paid"
                });
            }

            if (reg.PaymentStatus == "Cancelled")
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Registration has been cancelled"
                });
            }

            reg.PaymentStatus = "Paid";
            reg.PaymentMethod = request.PaymentMethod;
            reg.TransactionNo = request.TransactionNo;
            reg.PaymentDate = DateTime.Now; 

            await _context.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Payment confirmed"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while confirming payment",
                Errors = new List<string> { ex.Message }
            });
        }
    }


    private int GetCurrentUserId()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return id;
        throw new InvalidOperationException("Không thể xác định ID người dùng từ token.");
    }
}

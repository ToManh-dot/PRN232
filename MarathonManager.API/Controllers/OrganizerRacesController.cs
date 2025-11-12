using MarathonManager.API.DTOs.Race;
using MarathonManager.API.DTOs.RaceDistances;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace MarathonManager.API.Controllers;

[Route("api/organizer/races")]
[ApiController]
[Authorize(Roles = "Organizer")]
public class OrganizerRacesController : ControllerBase
{
    private readonly MarathonManagerContext _context;
    private readonly IWebHostEnvironment _environment;

    public OrganizerRacesController(MarathonManagerContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetMyRaces()
    {
        var organizerId = GetCurrentUserId();

        var races = await _context.Races
            .Where(r => r.OrganizerId == organizerId)
            .OrderByDescending(r => r.RaceDate)
            .Select(r => new RaceSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Location = r.Location,
                RaceDate = r.RaceDate,
                ImageUrl = r.ImageUrl,
                Status = r.Status
            })
            .ToListAsync();

        if (races.Count == 0)
            return NotFound(new { message = "Không tìm thấy giải chạy nào do bạn tổ chức." });

        return Ok(races);
    }

    // POST: api/organizer/races  (multipart/form-data)
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RaceDetailDto>> CreateRace([FromForm] RaceCreateDto raceDto, IFormFile? imageFile)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var organizerId = GetCurrentUserId();
        string? imageUrlPath = null;

        // Lưu ảnh (nếu có)
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                return BadRequest(new { imageFile = "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif)." });

            if (imageFile.Length > 5 * 1024 * 1024)
                return BadRequest(new { imageFile = "Kích thước file ảnh không được vượt quá 5MB." });

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "races");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fs);
            }
            imageUrlPath = $"/images/races/{uniqueFileName}";
        }

        var newRace = new Race
        {
            Name = raceDto.Name,
            Description = raceDto.Description,
            Location = raceDto.Location,
            RaceDate = raceDto.RaceDate,
            ImageUrl = imageUrlPath,
            OrganizerId = organizerId,
            Status = "Pending"
        };
        _context.Races.Add(newRace);
        await _context.SaveChangesAsync();

        // Parse CSV distances (ví dụ: "5km,10km,21.1km")
        var createdDistances = new List<RaceDistance>();
        if (!string.IsNullOrWhiteSpace(raceDto.DistancesCsv))
        {
            var regex = new Regex(@"[0-9]+(\.[0-9]+)?");
            foreach (var raw in raceDto.DistancesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var m = regex.Match(raw);
                if (m.Success && decimal.TryParse(m.Value, out var km) && km > 0)
                {
                    createdDistances.Add(new RaceDistance
                    {
                        RaceId = newRace.Id,
                        Name = $"{km}km",
                        DistanceInKm = km,
                        RegistrationFee = 0,
                        MaxParticipants = 100,
                        StartTime = newRace.RaceDate.Date.AddHours(6)
                    });
                }
            }
            if (createdDistances.Count > 0)
            {
                _context.RaceDistances.AddRange(createdDistances);
                await _context.SaveChangesAsync();
            }
        }

        var organizer = await _context.Users.FindAsync(organizerId);
        var dto = new RaceDetailDto
        {
            Id = newRace.Id,
            Name = newRace.Name,
            Description = newRace.Description,
            Location = newRace.Location,
            RaceDate = newRace.RaceDate,
            ImageUrl = newRace.ImageUrl,
            Status = newRace.Status,
            OrganizerName = organizer?.FullName ?? "N/A",
            Distances = createdDistances.Select(d => new RaceDistanceDto
            {
                Id = d.Id,
                Name = d.Name,
                DistanceInKm = d.DistanceInKm,
                RegistrationFee = d.RegistrationFee,
                MaxParticipants = d.MaxParticipants,
                StartTime = d.StartTime
            }).ToList()
        };

        return CreatedAtAction(nameof(GetMyRaces), new { }, dto);
    }

    // PUT: api/organizer/races/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateRace(int id, [FromBody] RaceUpdateDto raceDto)
    {
        if (id != raceDto.Id) return BadRequest(new { message = "ID không khớp." });
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var race = await _context.Races.FindAsync(id);
        if (race == null) return NotFound();

        var organizerId = GetCurrentUserId();
        if (race.OrganizerId != organizerId) return Forbid();
        if (race.Status == "Completed") return BadRequest(new { message = "Không thể sửa giải đã hoàn thành." });

        race.Name = raceDto.Name;
        race.Description = raceDto.Description;
        race.Location = raceDto.Location;
        race.RaceDate = raceDto.RaceDate;
        race.Status = "Pending";

        _context.Entry(race).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // --- DISTANCES CRUD ---

    // GET: api/organizer/races/{raceId}/distances
    [HttpGet("{raceId:int}/distances")]
    public async Task<ActionResult<IEnumerable<RaceDistanceDto>>> GetDistancesForRace(int raceId)
    {
        var organizerId = GetCurrentUserId();
        var isOwner = await _context.Races.AnyAsync(r => r.Id == raceId && r.OrganizerId == organizerId);
        if (!isOwner) return Forbid("Bạn không có quyền xem cự ly của giải chạy này.");

        var distances = await _context.RaceDistances
            .Where(d => d.RaceId == raceId)
            .Select(d => new RaceDistanceDto
            {
                Id = d.Id,
                Name = d.Name,
                DistanceInKm = d.DistanceInKm,
                RegistrationFee = d.RegistrationFee,
                MaxParticipants = d.MaxParticipants,
                StartTime = d.StartTime
            })
            .ToListAsync();

        return Ok(distances);
    }

    // POST: api/organizer/races/{raceId}/distances
    [HttpPost("{raceId:int}/distances")]
    public async Task<ActionResult<RaceDistanceDto>> AddDistanceToRace(int raceId, RaceDistanceCreateDto createDto)
    {
        var race = await _context.Races.FindAsync(raceId);
        if (race == null) return NotFound("Không tìm thấy giải chạy.");

        var organizerId = GetCurrentUserId();
        if (race.OrganizerId != organizerId) return Forbid("Bạn không có quyền thêm cự ly cho giải chạy này.");
        if (race.Status != "Pending" && race.Status != "Approved")
            return BadRequest("Không thể thêm cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

        var newDistance = new RaceDistance
        {
            RaceId = raceId,
            Name = createDto.Name,
            DistanceInKm = createDto.DistanceInKm,
            RegistrationFee = createDto.RegistrationFee,
            MaxParticipants = createDto.MaxParticipants,
            StartTime = createDto.StartTime
        };
        _context.RaceDistances.Add(newDistance);
        await _context.SaveChangesAsync();

        var dto = new RaceDistanceDto
        {
            Id = newDistance.Id,
            Name = newDistance.Name,
            DistanceInKm = newDistance.DistanceInKm,
            RegistrationFee = newDistance.RegistrationFee,
            MaxParticipants = newDistance.MaxParticipants,
            StartTime = newDistance.StartTime
        };
        return CreatedAtAction(nameof(GetDistancesForRace), new { raceId }, dto);
    }

    // PUT: api/organizer/races/{raceId}/distances/{distanceId}
    [HttpPut("{raceId:int}/distances/{distanceId:int}")]
    public async Task<IActionResult> UpdateDistance(int raceId, int distanceId, RaceDistanceUpdateDto updateDto)
    {
        var distance = await _context.RaceDistances.Include(d => d.Race)
            .FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
        if (distance == null) return NotFound("Không tìm thấy cự ly hoặc cự ly không thuộc giải chạy này.");

        var organizerId = GetCurrentUserId();
        if (distance.Race.OrganizerId != organizerId) return Forbid("Bạn không có quyền sửa cự ly này.");
        if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved")
            return BadRequest("Không thể sửa cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

        distance.Name = updateDto.Name;
        distance.DistanceInKm = updateDto.DistanceInKm;
        distance.RegistrationFee = updateDto.RegistrationFee;
        distance.MaxParticipants = updateDto.MaxParticipants;
        distance.StartTime = updateDto.StartTime;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // DELETE: api/organizer/races/{raceId}/distances/{distanceId}
    [HttpDelete("{raceId:int}/distances/{distanceId:int}")]
    public async Task<IActionResult> DeleteDistance(int raceId, int distanceId)
    {
        var distance = await _context.RaceDistances.Include(d => d.Race)
            .FirstOrDefaultAsync(d => d.Id == distanceId && d.RaceId == raceId);
        if (distance == null) return NotFound("Không tìm thấy cự ly hoặc cự ly không thuộc giải chạy này.");

        var organizerId = GetCurrentUserId();
        if (distance.Race.OrganizerId != organizerId) return Forbid("Bạn không có quyền xóa cự ly này.");
        if (distance.Race.Status != "Pending" && distance.Race.Status != "Approved")
            return BadRequest("Không thể xóa cự ly khi giải chạy đã hoàn thành hoặc bị hủy.");

        var hasRegs = await _context.Registrations.AnyAsync(r => r.RaceDistanceId == distanceId);
        if (hasRegs) return BadRequest("Không thể xóa cự ly đã có vận động viên đăng ký.");

        _context.RaceDistances.Remove(distance);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private int GetCurrentUserId()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return id;
        throw new InvalidOperationException("Không thể xác định ID người dùng từ token.");
    }
}

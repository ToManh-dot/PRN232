using MarathonManager.API.DTOs.Race;
using MarathonManager.API.DTOs.RaceDistances;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers;

[Route("api/admin/races")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminRacesController : ControllerBase
{
    private readonly MarathonManagerContext _context;
    private readonly IWebHostEnvironment _env;

    public AdminRacesController(MarathonManagerContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: api/admin/races/pending
    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetPendingRaces()
    {
        var races = await _context.Races
            .Where(r => r.Status == "Pending")
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
        return Ok(races);
    }

    // GET: api/admin/races/all
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetAllRacesForAdmin()
    {
        var races = await _context.Races
            .Include(r => r.Organizer)
            .OrderByDescending(r => r.RaceDate)
            .Select(r => new RaceSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Location = r.Location,
                RaceDate = r.RaceDate,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                OrganizerName = r.Organizer.FullName
            })
            .ToListAsync();

        return Ok(races);
    }

    // GET: api/admin/races/detail/{id}
    [HttpGet("detail/{id:int}")]
    public async Task<ActionResult<RaceDetailDto>> GetRaceDetailsForAdmin(int id)
    {
        var race = await _context.Races
            .Include(r => r.Organizer)
            .Include(r => r.RaceDistances)
            .Where(r => r.Id == id)
            .Select(r => new RaceDetailDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                Location = r.Location,
                RaceDate = r.RaceDate,
                ImageUrl = r.ImageUrl,
                Status = r.Status,
                OrganizerName = r.Organizer.FullName,
                Distances = r.RaceDistances.Select(d => new RaceDistanceDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    DistanceInKm = d.DistanceInKm,
                    RegistrationFee = d.RegistrationFee,
                    MaxParticipants = d.MaxParticipants,
                    StartTime = d.StartTime
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (race == null) return NotFound(new { message = "Không tìm thấy giải chạy." });
        return Ok(race);
    }

    // PATCH: api/admin/races/{id}/approve
    [HttpPatch("{id:int}/approve")]
    public async Task<IActionResult> ApproveRace(int id)
    {
        var race = await _context.Races.FindAsync(id);
        if (race == null) return NotFound();
        race.Status = "Approved";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Duyệt giải thành công." });
    }

    // PATCH: api/admin/races/{id}/cancel
    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> CancelRace(int id)
    {
        var race = await _context.Races.FindAsync(id);
        if (race == null) return NotFound();
        race.Status = "Cancelled";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Hủy giải thành công." });
    }

    // DELETE: api/admin/races/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteRace(int id)
    {
        var race = await _context.Races.FindAsync(id);
        if (race == null) return NotFound();

        // Xóa ảnh vật lý (nếu có)
        if (!string.IsNullOrEmpty(race.ImageUrl))
        {
            try
            {
                var path = Path.Combine(_env.WebRootPath ?? "wwwroot", race.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* log nếu cần */ }
        }

        _context.Races.Remove(race);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

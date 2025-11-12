using MarathonManager.API.DTOs.Race;
using MarathonManager.API.DTOs.RaceDistances;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers;

[Route("api/public/races")]
[ApiController]
[AllowAnonymous]
public class PublicRacesController : ControllerBase
{
    private readonly MarathonManagerContext _context;

    public PublicRacesController(MarathonManagerContext context)
    {
        _context = context;
    }

    // GET: api/public/races
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RaceSummaryDto>>> GetRaces()
    {
        var races = await _context.Races
            .Where(r => r.Status == "Approved")
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

        return Ok(races);
    }

    // GET: api/public/races/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RaceDetailDto>> GetRace(int id)
    {
        var race = await _context.Races
            .Include(r => r.Organizer)
            .Include(r => r.RaceDistances)
            .Where(r => r.Id == id && r.Status == "Approved")
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

        if (race == null)
            return NotFound(new { message = "Không tìm thấy giải chạy hoặc giải chưa được duyệt." });

        return Ok(race);
    }
}

using MarathonManager.API.DTOs.Race;
using MarathonManager.API.DTOs.RaceDistances;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RacesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public RacesController(MarathonManagerContext context)
        {
            _context = context;
        }

        
        [HttpGet]
        [AllowAnonymous]
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
                    Status = r.Status,
                    OrganizerName = r.Organizer.FullName
                })
                .ToListAsync();

            return Ok(races);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
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

        // API/Controllers/RacesController.cs
        [HttpGet("manage/{id:int}")]
        [Authorize(Roles = "Organizer,Admin")]
        public async Task<ActionResult<RaceUpdateDto>> GetRaceForEdit(int id)
        {
            var race = await _context.Races
                .Where(r => r.Id == id)
                .Select(r => new RaceUpdateDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    Location = r.Location,
                    RaceDate = r.RaceDate,
                    ImageUrl = r.ImageUrl
                })
                .FirstOrDefaultAsync();

            if (race == null)
                return NotFound(new { message = "Không tìm thấy giải chạy." });

            return Ok(race);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Organizer")]
        public async Task<IActionResult> UpdateRace(int id, [FromBody] RaceUpdateDto dto)
        {
            if (id != dto.Id) return BadRequest("Id không khớp");

            var race = await _context.Races.FindAsync(id);
            if (race == null) return NotFound("Không tìm thấy giải chạy.");

            race.Name = dto.Name;
            race.Description = dto.Description;
            race.Location = dto.Location;
            race.RaceDate = dto.RaceDate;
            race.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật giải chạy thành công" });
        }
        // GET: api/races/{raceId}/runners
        [HttpGet("{raceId:int}/runners")]
        [Authorize(Roles = "Organizer")]
        public async Task<ActionResult<IEnumerable<RunnersInRaceDto>>> GetRunnersInRace(int raceId)
        {
            var runners = await _context.Registrations
                .Where(r => r.RaceDistance.RaceId == raceId)
                .Include(r => r.Runner)
                .Select(r => new RunnersInRaceDto
                {
                    Id = r.Runner.Id,
                    FullName = r.Runner.FullName,
                    Email = r.Runner.Email
                })
                .Distinct()
                .ToListAsync();

            return Ok(runners);
        }

    }
}

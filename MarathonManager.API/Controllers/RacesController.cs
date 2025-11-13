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

        // ==========================================================
        // PUBLIC ENDPOINTS (AllowAnonymous)
        // ==========================================================

        // GET: api/races  — danh sách giải đã Approved
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

        // GET: api/races/{id} — chi tiết 1 giải đã Approved
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<RaceDetailDto>> GetRace(int id)
        {
            var race = await _context.Races
               .Include(r => r.Organizer)      // để lấy OrganizerName
               .Include(r => r.RaceDistances)  // để map Distances
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
}

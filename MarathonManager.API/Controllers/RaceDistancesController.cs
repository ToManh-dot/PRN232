using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RaceDistancesController : ControllerBase
    {
        private readonly MarathonManagerContext _context;

        public RaceDistancesController(MarathonManagerContext context)
        {
            _context = context; 
        }

        // GET: /api/RaceDistances/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetRaceDistance(int id)
        {
            var distance = await _context.RaceDistances
                .Include(rd => rd.Race) // Lấy thông tin giải chạy
                .FirstOrDefaultAsync(rd => rd.Id == id);

            if (distance == null)
                return NotFound(new { message = "Không tìm thấy cự ly." });

            var result = new
            {
                distance.Id,
                distance.Name,
                distance.DistanceInKm,
                distance.RegistrationFee,
                distance.StartTime,
                RaceName = distance.Race.Name,
                RaceLocation = distance.Race.Location,
                RaceDate = distance.Race.RaceDate
            };

            return Ok(result);
        }
    }
}

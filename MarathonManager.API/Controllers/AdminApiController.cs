using MarathonManager.API.DTOs; 
using MarathonManager.API.DTOs.Admin; 
using MarathonManager.API.DTOs.Race;

using MarathonManager.API.Models; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;   

namespace MarathonManager.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class AdminApiController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly MarathonManagerContext _context;

        public AdminApiController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            MarathonManagerContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

       
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var usersDto = await _userManager.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd.Value <= DateTimeOffset.UtcNow,
                    CreatedAt = u.CreatedAt,
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    DateOfBirth = u.DateOfBirth, 
                    Gender = u.Gender,           
                    Roles = (from ur in _context.UserRoles
                             join r in _context.Roles on ur.RoleId equals r.Id
                             where ur.UserId == u.Id
                             select r.Name).ToList() ?? new List<string>()
                })
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return Ok(usersDto);
        }

        // PATCH: api/admin/users/{id}/toggle-lock
        [HttpPatch("users/{id}/toggle-lock")]
        public async Task<IActionResult> ToggleUserLock(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            bool currentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            IdentityResult result;
            if (currentlyLocked)
                result = await _userManager.SetLockoutEndDateAsync(user, null); 
            else
                result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); 

            if (result.Succeeded)
            {
                bool isActiveNow = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
                return Ok(new { message = $"Tài khoản đã {(isActiveNow ? "mở khóa" : "bị khóa")}.", isActive = isActiveNow });
            }
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest($"Cập nhật thất bại: {errors}");
        }

        // GET: api/admin/users/5
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("Không tìm thấy người dùng.");

            var roles = await _userManager.GetRolesAsync(user);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,

                IsActive = !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow,

                CreatedAt = user.CreatedAt,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Roles = roles.ToList()
            };
            return Ok(userDto);
        }

        // GET: api/admin/roles
       
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto { Id = r.Id.ToString(), Name = r.Name })
                .OrderBy(r => r.Name)
                .ToListAsync();
            return Ok(roles);
        }

        [HttpPut("users/{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(int id, [FromBody] UpdateUserRolesDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            dto.RoleNames ??= new List<string>(); 

            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(dto.RoleNames).ToList();
            var rolesToAdd = dto.RoleNames.Except(currentRoles).ToList();

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Lỗi khi xóa role cũ: {errors}" });
                }
            }

            if (rolesToAdd.Any())
            {
                foreach (var roleName in rolesToAdd)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        return BadRequest(new { message = $"Role '{roleName}' không tồn tại." });
                    }
                }
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                    return BadRequest(new { message = $"Lỗi khi thêm role mới: {errors}" });
                }
            }

            return Ok(new { message = "Cập nhật role thành công." });
        }

      
        [HttpGet("blogs")]
        public async Task<ActionResult<IEnumerable<BlogAdminDto>>> GetBlogPosts()
        {
            var blogs = await _context.BlogPosts
                .Include(b => b.Author) 
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new BlogAdminDto
                {
                    Id = b.Id,
                    Title = b.Title, 
                    Status = b.Status,
                    AuthorName = b.Author != null ? b.Author.FullName : "N/A", 
                    CreatedAt = b.CreatedAt,
                    UpdatedAt = b.UpdatedAt
                })
                .ToListAsync();
            return Ok(blogs);
        }

        // PATCH: api/admin/blogs/{id}/toggle-publish
        [HttpPatch("blogs/{id}/toggle-publish")]
        public async Task<IActionResult> ToggleBlogPostPublish(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết.");
            }

            blogPost.Status = (blogPost.Status == "Published") ? "Draft" : "Published";
            blogPost.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Bài viết đã chuyển sang trạng thái {blogPost.Status}.", newStatus = blogPost.Status });
        }

        [HttpDelete("blogs/{id}")]
        public async Task<IActionResult> DeleteBlogPost(int id)
        {
            var blogPost = await _context.BlogPosts.FindAsync(id);
            if (blogPost == null)
            {
                return NotFound("Không tìm thấy bài viết.");
            }

            _context.BlogPosts.Remove(blogPost);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bài viết thành công." });
        }

      
        [HttpGet("all-races")]
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

        // GET: api/admin/races/detail/5
        [HttpGet("all-races/detail/{id}")]
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

            if (race == null)
            {
                return NotFound(new { message = "Không tìm thấy giải chạy." });
            }
            return Ok(race);
        }

  
    }
}
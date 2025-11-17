using MarathonManager.API.DTOs.Account;
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly MarathonManagerContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountsController(MarathonManagerContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: api/account/profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Không thể xác định người dùng." });

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserProfileDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    DateOfBirth = u.DateOfBirth,
                    Gender = u.Gender,
                    IsActive = u.IsActive,
                    UserName = u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    CreatedAt = u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null) return NotFound(new { message = "Không tìm thấy người dùng." });

            return Ok(user);
        }

        // PUT: api/accounts/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] EditProfileDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

         
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.Gender = model.Gender;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { errors });
            }

            return Ok(new { message = "Cập nhật thông tin thành công." });
        }

        // POST: api/Account/ChangePassword
        [HttpPost("change-password")]
        [Authorize] 
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.GetUserAsync(User);
            var isValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!isValid)
                return BadRequest(new { message = "Current password không đúng." });

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                return BadRequest(new { errors });
            }

            await _signInManager.RefreshSignInAsync(user);

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
    }
}

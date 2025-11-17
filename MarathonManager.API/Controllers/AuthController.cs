using MarathonManager.API.DTOs.Auth; 
using MarathonManager.API.Models;
using Microsoft.AspNetCore.Identity;
using MarathonManager.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MarathonManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public AuthController(
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            IConfiguration configuration, IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailSender = emailSender;
        }

      
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }
            var passwordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!passwordValid)
            {
                return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin." });
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var tokenString = GenerateJwtToken(user, userRoles);

            return Ok(new
            {
                token = tokenString,
                user = new
                {
                    email = user.Email,
                    fullName = user.FullName,
                    roles = userRoles
                }
            });
        }

  
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
            {
                return BadRequest(new { message = "Email đã tồn tại." });
            }

            User newUser = new User()
            {
                Email = registerDto.Email,
                UserName = registerDto.Email, 
                FullName = registerDto.FullName,
                IsActive = true, 
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser, registerDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Tạo tài khoản thất bại.", errors = result.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(newUser, "Runner");

            return Ok(new { message = "Đăng ký tài khoản thành công." });
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                return Ok(new { message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var frontendBaseUrl = _configuration["FrontendBaseUrl"];

            var resetUrl =
                $"{frontendBaseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(encodedToken)}";

            var subject = "Đặt lại mật khẩu Marathon Manager";
            var body = $@"
Xin chào {user.FullName},

Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản Marathon Manager.
Vui lòng click vào link sau để đặt lại mật khẩu (link chỉ sử dụng được 1 lần):

{resetUrl}

Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.
";

            await _emailSender.SendEmailAsync(user.Email!, subject, body);

            return Ok(new { message = "Nếu email tồn tại, hệ thống đã gửi hướng dẫn đặt lại mật khẩu." });
        }

        
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Yêu cầu đặt lại mật khẩu không hợp lệ." });
            }

            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(dto.Token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
            }
            catch
            {
                return BadRequest(new { message = "Token không hợp lệ." });
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Không thể đặt lại mật khẩu.",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            return Ok(new { message = "Đặt lại mật khẩu thành công." });
        }


    
        private string GenerateJwtToken(User user, IList<string> roles)
        {
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) 
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.Now.AddHours(24),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


      
        [HttpPost]
        [Route("seed-roles")]
        public async Task<IActionResult> SeedRoles()
        {
            bool adminRoleExists = await _roleManager.RoleExistsAsync("Admin");
            bool organizerRoleExists = await _roleManager.RoleExistsAsync("Organizer");
            bool runnerRoleExists = await _roleManager.RoleExistsAsync("Runner");

            if (!adminRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Admin"));
            }
            if (!organizerRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Organizer"));
            }
            if (!runnerRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Runner"));
            }

            var adminUser = await _userManager.FindByEmailAsync("admin@marathon.com");
            if (adminUser == null)
            {
                User admin = new User
                {
                    Email = "admin@marathon.com",
                    UserName = "admin@marathon.com",
                    FullName = "Quản Trị Viên",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                var result = await _userManager.CreateAsync(admin, "Admin@123"); 
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            return Ok(new { message = "Tạo Roles (và tài khoản Admin) thành công." });
        }
//#if DEBUG   // Đảm bảo chỉ build trong Debug, không dùng cho Production
//        // POST: api/auth/debug-reset-password
//        [HttpPost("debug-reset-password")]
//        public async Task<IActionResult> DebugResetPassword([FromBody] DebugResetPasswordDto dto)
//        {
//            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.NewPassword))
//            {
//                return BadRequest(new { message = "Email và mật khẩu mới không được để trống." });
//            }

//            var user = await _userManager.FindByEmailAsync(dto.Email);
//            if (user == null)
//            {
//                return NotFound(new { message = "Không tìm thấy user với email này." });
//            }

//            // Xóa mật khẩu cũ (nếu có)
//            var removeResult = await _userManager.RemovePasswordAsync(user);
//            if (!removeResult.Succeeded)
//            {
//                return BadRequest(new
//                {
//                    message = "Không thể xóa mật khẩu cũ.",
//                    errors = removeResult.Errors.Select(e => e.Description)
//                });
//            }

//            // Thêm mật khẩu mới
//            var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
//            if (!addResult.Succeeded)
//            {
//                return BadRequest(new
//                {
//                    message = "Không thể đặt mật khẩu mới.",
//                    errors = addResult.Errors.Select(e => e.Description)
//                });
//            }

//            return Ok(new
//            {
//                message = "Đặt lại mật khẩu thành công.",
//                email = dto.Email,
//                newPassword = dto.NewPassword
//            });
//        }
//#endif

    }
}
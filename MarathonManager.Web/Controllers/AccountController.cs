using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MarathonManager.API.DTOs.Account;
using System.Net.Http;
using System.Text.Json;
namespace MarathonManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration; // Dùng để đọc địa chỉ API
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // ===================================
        // GET: /Account/Login
        // Hiển thị Form Đăng nhập
        // ===================================
        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ===================================
        // POST: /Account/Login
        // Xử lý việc Đăng nhập
        // ===================================
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            // 1. Gói dữ liệu và gọi API
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/login", jsonContent);

            // 2. Xử lý kết quả
            if (apiResponse.IsSuccessStatusCode)
            {
                // Đọc nội dung (chứa token)
                var jsonResponse = await apiResponse.Content.ReadAsStringAsync();

                // Cần 1 class tạm để hứng token
                var tokenDto = JsonConvert.DeserializeObject<TokenResponseDto>(jsonResponse);

                // 3. Xử lý Token (QUAN TRỌNG NHẤT)
                await SignInUserAsync(tokenDto.Token);

                return LocalRedirect(returnUrl);
            }
            else
            {
                // Lấy lỗi từ API (nếu có)
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }
        }

        // ===================================
        // GET: /Account/Register
        // Hiển thị Form Đăng ký
        // ===================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ===================================
        // POST: /Account/Register
        // Xử lý việc Đăng ký
        // ===================================
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            // DTO của API chỉ cần FullName, Email, Password
            var apiDto = new { model.FullName, model.Email, model.Password };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/register", jsonContent);

            if (apiResponse.IsSuccessStatusCode)
            {
                // Đăng ký thành công, chuyển đến trang Đăng nhập
                return RedirectToAction("Login", new { registrationSuccess = true });
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Đăng ký thất bại. Email có thể đã tồn tại.");
                return View(model);
            }
        }

        // ===================================
        // POST: /Account/Logout
        // Xử lý Đăng xuất
        // ===================================
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // 1. Xóa Cookie của Web (ASP.NET Core Cookie)
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // 2. Xóa Cookie chứa JWT (AuthToken)
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("AuthToken");

            return RedirectToAction("Index", "Home");
        }


        // ===================================
        // HÀM PHỤ TRỢ (Private)
        // ===================================

        // Hàm này thực hiện 2 việc:
        // 1. Đăng nhập vào ứng dụng Web (để User.Identity.IsAuthenticated = true)
        // 2. Lưu JWT Token vào HttpOnly Cookie (để gửi cho API ở các request sau)
        private async Task SignInUserAsync(string token)
        {
            // 1. Đọc Token
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            // 2. Tạo danh tính (ClaimsIdentity) từ Token
            // (ASP.NET Core sẽ dùng nó để tạo Cookie Xác thực)
            var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // 3. Lấy tên từ claim "name"
            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            if (nameClaim != null)
            {
                // Thêm claim "Name" (loại mặc định) để _Layout có thể hiển thị @User.Identity.Name
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // "Ghi nhớ" đăng nhập
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            // 4. Đăng nhập người dùng vào ứng dụng WEB
            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // 5. Lưu JWT Token gốc vào HttpOnly Cookie
            // Cookie này sẽ được IHttpClientFactory tự động đọc và gửi đến API
            _httpContextAccessor.HttpContext.Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,  // Chỉ server được đọc, JavaScript không thể
                Secure = true,    // Chỉ gửi qua HTTPS
                SameSite = SameSiteMode.Strict, // Chống tấn công CSRF
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });
        }

        // Class tạm thời để hứng token
        private class TokenResponseDto
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }
        // ===================================
        // GET: /Account/Profile
        // Hiển thị thông tin người dùng hiện tại
        // ===================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để xem thông tin cá nhân.";
                return RedirectToAction("Login");
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/accounts/profile");

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin người dùng.";
                return View();
            }

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonConvert.DeserializeObject<UserProfileViewModel>(json);

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để chỉnh sửa thông tin cá nhân.";
                return RedirectToAction("Login");
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/api/accounts/profile");
            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = "Không thể tải thông tin người dùng.";
                return RedirectToAction("Profile");
            }

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonConvert.DeserializeObject<UserProfileViewModel>(json);

            var model = new EditProfileViewModel
            {
                FullName = profile.FullName,
                PhoneNumber = profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth,
                Gender = profile.Gender
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = _httpContextAccessor.HttpContext.Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login");
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Gửi thông tin update lên API
            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, "application/json");

            var response = await client.PutAsync("/api/accounts/profile", jsonContent); // PUT hoặc PATCH tùy API

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);
            return View(model);
        }


        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới và nhập lại không khớp");
                return View(model);
            }

            var token = Request.Cookies["AuthToken"];
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Login");
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Map ViewModel -> API DTO
            var apiDto = new ChangePasswordDto
            {
                CurrentPassword = model.CurrentPassword,
                NewPassword = model.NewPassword,
                ConfirmPassword = model.ConfirmPassword
            };

            var response = await client.PostAsync(
                "/api/accounts/change-password",
                new StringContent(JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Đổi mật khẩu thành công!";
                return View(model);
            }

            var error = await response.Content.ReadAsStringAsync();
            ModelState.AddModelError("", error);

            return View(model);
        }
    }
}
using MarathonManager.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;
using MarathonManager.API.DTOs.Account;
using System.Net.Http;
using System.Text.Json;

namespace MarathonManager.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
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

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/login", jsonContent);

            if (apiResponse.IsSuccessStatusCode)
            {
                var jsonResponse = await apiResponse.Content.ReadAsStringAsync();
                var tokenDto = JsonConvert.DeserializeObject<TokenResponseDto>(jsonResponse);
                await SignInUserAsync(tokenDto.Token);
                return LocalRedirect(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var response = await client.PostAsJsonAsync("api/auth/forgot-password", new
            {
                email = model.Email
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Không thể gửi yêu cầu đặt lại mật khẩu. Vui lòng thử lại sau.";
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            {
                TempData["ErrorMessage"] = "Link đặt lại mật khẩu không hợp lệ.";
                return RedirectToAction("Login");
            }

            var vm = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var response = await client.PostAsJsonAsync("api/auth/reset-password", new
            {
                email = model.Email,
                token = model.Token,
                newPassword = model.NewPassword
            });

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Không thể đặt lại mật khẩu. Link có thể đã hết hạn hoặc không hợp lệ.";
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient("MarathonApi");

            var apiDto = new { model.FullName, model.Email, model.Password };

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(apiDto),
                Encoding.UTF8, "application/json");

            var apiResponse = await client.PostAsync("/api/auth/register", jsonContent);

            if (apiResponse.IsSuccessStatusCode)
            {
                return RedirectToAction("Login", new { registrationSuccess = true });
            }
            else
            {
                var responseBody = await apiResponse.Content.ReadAsStringAsync();
                try
                {
                    dynamic errorObj = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    if (errorObj?.message != null)
                    {
                        ModelState.AddModelError(string.Empty, (string)errorObj.message);
                    }

                    if (errorObj?.errors != null)
                    {
                        foreach (var err in errorObj.errors)
                        {
                            ModelState.AddModelError(string.Empty, (string)err);
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Đăng ký thất bại. Vui lòng thử lại.");
                }

                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _httpContextAccessor.HttpContext.Response.Cookies.Delete("AuthToken");
            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUserAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            var claimsIdentity = new ClaimsIdentity(jwtToken.Claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name");
            if (nameClaim != null)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, nameClaim.Value));
            }

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await _httpContextAccessor.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _httpContextAccessor.HttpContext.Response.Cookies.Append("AuthToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });
        }

        private class TokenResponseDto
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }

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

            var jsonContent = new StringContent(
                JsonConvert.SerializeObject(model),
                Encoding.UTF8, "application/json");

            var response = await client.PutAsync("/api/accounts/profile", jsonContent);

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

using MarathonManager.API.DTOs;
using MarathonManager.API.Models;
using MarathonManager.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarathonManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly MarathonManagerContext _context;

        public PaymentsController(IConfiguration config, MarathonManagerContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost("vnpay/create")]
        public async Task<IActionResult> CreateVnPayPayment([FromBody] CreateVnPayPaymentRequest request)
        {
            // Lấy registration từ DB
            var registration = await _context.Registrations
                .Include(r => r.RaceDistance)
                .FirstOrDefaultAsync(r => r.Id == request.RegistrationId);

            if (registration == null)
                return NotFound(new { Message = "Registration not found" });

            if (registration.PaymentStatus == "Paid")
                return BadRequest(new { Message = "Registration already paid" });

            if (registration.PaymentStatus == "Cancelled")
                return BadRequest(new { Message = "Registration has been cancelled" });

            string vnp_Url = _config["VnPay:Url"] ?? string.Empty;
            string vnp_TmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            string vnp_HashSecret = _config["VnPay:HashSecret"] ?? string.Empty;
            string vnp_ReturnUrl = _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrEmpty(vnp_Url) ||
                string.IsNullOrEmpty(vnp_TmnCode) ||
                string.IsNullOrEmpty(vnp_HashSecret) ||
                string.IsNullOrEmpty(vnp_ReturnUrl))
            {
                return StatusCode(500, new { Message = "VNPAY configuration is missing" });
            }

            var vnpay = new VnPayLibrary();

            long amount = (long)(registration.RaceDistance.RegistrationFee * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toán đăng ký giải chạy #{registration.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", registration.Id.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            if (registration.PaymentStatus != "Pending")
            {
                registration.PaymentStatus = "Pending";
                await _context.SaveChangesAsync();
            }

            return Ok(new CreateVnPayPaymentResponse { PaymentUrl = paymentUrl });
        }

    }
}

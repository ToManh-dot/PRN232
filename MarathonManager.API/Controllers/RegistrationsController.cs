using MarathonManager.API;
using MarathonManager.API.DTOs.Registration;
using MarathonManager.API.Models;
using MarathonManager.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RegistrationsController : ControllerBase
{
    private readonly MarathonManagerContext _context;
    private readonly IConfiguration _config; 

    public RegistrationsController(MarathonManagerContext context, IConfiguration config) 
    {
        _context = context;
        _config = config;
    }

    // POST: api/Registrations
    [HttpPost]
    [Authorize(Roles = "Runner")]
    public async Task<IActionResult> RegisterForRace([FromBody] RunnerRegistrationRequestDto registrationDto)
    {
        var runnerId = GetCurrentUserId();

        var raceDistance = await _context.RaceDistances
            .Include(rd => rd.Race)
            .FirstOrDefaultAsync(rd => rd.Id == registrationDto.RaceDistanceId);

        if (raceDistance == null || raceDistance.Race.Status != "Approved")
            return NotFound(new { message = "Cự ly không tồn tại hoặc giải chưa được duyệt." });

        if (await _context.Registrations.AnyAsync(r => r.RunnerId == runnerId && r.RaceDistanceId == registrationDto.RaceDistanceId))
            return BadRequest(new { message = "Bạn đã đăng ký cự ly này rồi." });

        int currentCount = await _context.Registrations.CountAsync(r => r.RaceDistanceId == registrationDto.RaceDistanceId);
        if (currentCount >= raceDistance.MaxParticipants)
            return BadRequest(new { message = "Cự ly đã hết chỗ." });

        var newRegistration = new Registration
        {
            RunnerId = runnerId,
            RaceDistanceId = registrationDto.RaceDistanceId,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending"
        };

        _context.Registrations.Add(newRegistration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Đăng ký thành công! Vui lòng thanh toán.", registrationId = newRegistration.Id });
    }

    // POST: api/Registrations/{registrationId}/pay
    [HttpPost("{registrationId}/pay")]
    [Authorize(Roles = "Runner")]
    public async Task<IActionResult> PayRegistration(int registrationId)
    {
        var runnerId = GetCurrentUserId();

        var registration = await _context.Registrations
            .Include(r => r.RaceDistance)
            .ThenInclude(rd => rd.Race)
            .FirstOrDefaultAsync(r => r.Id == registrationId && r.RunnerId == runnerId);

        if (registration == null)
            return NotFound(new { message = "Không tìm thấy đăng ký này." });

        if (registration.PaymentStatus == "Paid")
            return BadRequest(new { message = "Đã thanh toán trước đó." });

        registration.PaymentStatus = "Paid";
        _context.Registrations.Update(registration);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Thanh toán thành công!", registrationId = registration.Id });
    }

    [HttpPost("{registrationId:int}/payment-url")]
    [Authorize(Roles = "Runner")]
    public IActionResult CreatePaymentUrl(int registrationId, [FromBody] PaymentRequestDto dto)
    {
        var reg = _context.Registrations
            .Include(r => r.RaceDistance)
            .FirstOrDefault(r => r.Id == registrationId && r.RunnerId == GetCurrentUserId());

        if (reg == null || reg.PaymentStatus != "Pending")
            return BadRequest(new { message = "Không thể thanh toán." });

        if (string.IsNullOrWhiteSpace(dto.ReturnUrl) ||
            !Uri.TryCreate(dto.ReturnUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return BadRequest(new { message = "ReturnUrl không hợp lệ. Phải là URL HTTPS công khai (ngrok hoặc frontend domain)." });
        }

        var vnpay = new VnPayLibrary();

        var amount = (int)(reg.RaceDistance.RegistrationFee * 100); 

        vnpay.AddRequestData("vnp_Version", "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", _config["VNPAY:TmnCode"]);
        vnpay.AddRequestData("vnp_Amount", amount.ToString());
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        if (ipAddress == "::1") ipAddress = "127.0.0.1";
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);

        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan dang ky ID {registrationId}");
        vnpay.AddRequestData("vnp_OrderType", "other");

        vnpay.AddRequestData("vnp_ReturnUrl", dto.ReturnUrl.Trim());

        vnpay.AddRequestData("vnp_TxnRef", registrationId.ToString());

        string paymentUrl = vnpay.CreateRequestUrl(
            _config["VNPAY:Url"],
            _config["VNPAY:HashSecret"]
        );

        Console.WriteLine("🔗 VNPAY Payment URL: " + paymentUrl);

        return Ok(new { paymentUrl });
    }

    [HttpGet("return")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentReturn()
    {
        var vnpay = new VnPayLibrary();

        Console.WriteLine("===== VNPAY RETURN START =====");
        Console.WriteLine("Query string received:");

        foreach (var key in Request.Query.Keys)
        {
            var value = Request.Query[key];
            Console.WriteLine($"{key} = {value}");
            if (key.StartsWith("vnp_"))
                vnpay.AddResponseData(key, value);
        }

        var txnRefStr = vnpay.GetResponseData("vnp_TxnRef");
        var responseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnpay.GetResponseData("vnp_TransactionStatus");

        Console.WriteLine($"txnRef = {txnRefStr}");
        Console.WriteLine($"responseCode = {responseCode}");
        Console.WriteLine($"transactionStatus = {transactionStatus}");

        bool validSignature = vnpay.ValidateSignature(_config["VNPAY:HashSecret"]);
        Console.WriteLine("Signature valid: " + validSignature);

        if (!validSignature)
        {
            Console.WriteLine("❌ Signature không hợp lệ");
            return Redirect("/payment-failed?error=invalid_signature");
        }

        if (responseCode == "00" && transactionStatus == "00")
        {
            Console.WriteLine("✅ Thanh toán thành công, cập nhật DB");
            var reg = await _context.Registrations.FirstOrDefaultAsync(r => r.Id == int.Parse(txnRefStr));
            if (reg != null)
            {
                reg.PaymentStatus = "Paid";
                await _context.SaveChangesAsync();
                Console.WriteLine($"PaymentStatus của registrationId={txnRefStr} đã cập nhật thành Paid");
            }
            return Redirect("/payment-success?regId=" + txnRefStr);
        }

        Console.WriteLine("❌ Thanh toán thất bại hoặc bị hủy");
        return Redirect($"/payment-failed?regId={txnRefStr}&code={responseCode}");
    }



    // POST: api/Registrations/{raceId}/assign-bib
    [HttpPost("{raceId:int}/assign-bib")]
    [Authorize(Roles = "Organizer")]
    public async Task<IActionResult> AssignBibAutomatically(int raceId)
    {
        var race = await _context.Races
            .Include(r => r.RaceDistances).ThenInclude(d => d.Registrations)
            .FirstOrDefaultAsync(r => r.Id == raceId && r.OrganizerId == GetCurrentUserId());

        if (race == null) return Forbid();

        var paidNoBib = race.RaceDistances
            .SelectMany(d => d.Registrations)
            .Where(r => r.PaymentStatus == "Paid" && string.IsNullOrEmpty(r.BibNumber))
            .OrderBy(r => r.RegistrationDate)
            .ToList();

        if (!paidNoBib.Any())
            return Ok(new { message = "Không có VĐV nào cần gán BIB." });

        int nextBib = await _context.Registrations
            .Where(r => r.RaceDistance.RaceId == raceId && !string.IsNullOrEmpty(r.BibNumber))
            .Select(r => Convert.ToInt32(r.BibNumber))
            .DefaultIfEmpty(1000)
            .MaxAsync() + 1;

        foreach (var reg in paidNoBib)
            reg.BibNumber = nextBib++.ToString("D4"); 

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = $"Đã gán {paidNoBib.Count} BIB từ {nextBib - paidNoBib.Count:D4} đến {(nextBib - 1):D4}"
        });
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            throw new UnauthorizedAccessException("Không thể xác định người dùng.");
        return userId;
    }

    [HttpGet("test-return-url")]
    [AllowAnonymous]
    public IActionResult GenerateTestReturnUrl()
    {
        string hashSecret = _config["VNPAY:HashSecret"];
        string txnRef = "1";

        var queryParams = new SortedDictionary<string, string>
    {
        {"vnp_Amount", "100000"},
        {"vnp_BankCode", "NCB"},
        {"vnp_TxnRef", txnRef},
        {"vnp_ResponseCode", "00"}
    };

        var hashData = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(hashSecret));
        var hashBytes = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashData));
        string secureHash = BitConverter.ToString(hashBytes).Replace("-", "");

        var baseUrl = "http://localhost:5000/payment/return";
        var fullUrl = $"{baseUrl}?{hashData}&vnp_SecureHash={secureHash}";
        return Ok(fullUrl);
    }
}
using MarathonManager.API.Models;

namespace MarathonManager.API.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(Registration registration, string ipAddress)
        {
            string vnp_Url = _config["VnPay:Url"] ?? string.Empty;
            string vnp_TmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
            string vnp_HashSecret = _config["VnPay:HashSecret"] ?? string.Empty;
            string vnp_ReturnUrl = _config["VnPay:ReturnUrl"] ?? string.Empty;

            if (string.IsNullOrEmpty(vnp_Url) ||
                string.IsNullOrEmpty(vnp_TmnCode) ||
                string.IsNullOrEmpty(vnp_HashSecret) ||
                string.IsNullOrEmpty(vnp_ReturnUrl))
            {
                throw new InvalidOperationException("VNPAY configuration is missing.");
            }

            var vnpay = new VnPayLibrary();

            long amount = (long)(registration.RaceDistance.RegistrationFee * 100);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toán đăng ký giải chạy #{registration.Id}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", registration.Id.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }
    }
}

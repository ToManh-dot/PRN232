namespace MarathonManager.Web.DTOs
{
    public class CreateVnPayPaymentResponseDto
    {
        public string PaymentUrl { get; set; } = string.Empty;
    }

    public class ConfirmPaymentRequestDto
    {
        public string PaymentMethod { get; set; } = "VNPAY";
        public string? TransactionNo { get; set; }
    }
}

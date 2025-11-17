namespace MarathonManager.API.DTOs
{
    public class CreateVnPayPaymentRequest
    {
        public int RegistrationId { get; set; }
    }

    public class CreateVnPayPaymentResponse
    {
        public string PaymentUrl { get; set; } = string.Empty;
    }

    public class ConfirmPaymentRequest
    {
        public string PaymentMethod { get; set; } = string.Empty; 
        public string? TransactionNo { get; set; }          
    }
}

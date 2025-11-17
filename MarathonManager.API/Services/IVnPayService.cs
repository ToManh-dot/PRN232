using MarathonManager.API.Models;

namespace MarathonManager.API.Services
{
    public interface IVnPayService
    {
       
        string CreatePaymentUrl(Registration registration, string ipAddress);
    }
}

using PaymentGateway.Application.DTOs;


namespace PaymentGateway.Application.Interfaces
{
    public interface IBankClient
    {
        Task<BankAuthorizationResult> Process(PaymentRequestDto request);

        public class BankAuthorizationResult
        {
            public bool Authorized { get; set; }
            public string? AuthorizationCode { get; set; }
        }
    }
}

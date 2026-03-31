using Microsoft.Extensions.Logging;

using PaymentGateway.Api.Models;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Validator;
using PaymentGateway.Domain.Entities;


namespace PaymentGateway.Application.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IBankClient _bankClient;
        private readonly IPaymentRepository _paymentRepository;
        private readonly PaymentRequestValidator _validator;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IBankClient bankClient, IPaymentRepository paymentRepository, PaymentRequestValidator validator, ILogger<PaymentService> logger)
        {
            _bankClient = bankClient;
            _paymentRepository = paymentRepository;
            _validator = validator;
            _logger = logger;
        }

        public async Task<PaymentResponseDto> ProcessAsync(PaymentRequestDto request)
        {
            _logger.LogInformation(
            "Payment processing started. Amount={Amount}, Currency={Currency}",
            request.Amount,
            request.Currency
            );

            _validator.Validate(request);

            var bankResponse = await _bankClient.Process(request);

            var status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined;

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Amount = request.Amount,
                Currency = request.Currency.ToUpper(),
                Status = status
            };

            await _paymentRepository.SaveAsync(payment);

            _logger.LogInformation(
            "Payment processing completed. PaymentId={PaymentId}, Status={Status}, Amount={Amount}",
            payment.Id, status, request.Amount);

            return Map(payment);
        }



        public async Task<PaymentResponseDto> GetAsync(Guid id)
        {
            _logger.LogInformation("Retrieving payment. PaymentId={PaymentId}", id);
            var payment = await _paymentRepository.GetAsync(id);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found. PaymentId={PaymentId}", id);
            }

            return payment == null ? null : Map(payment);
        }

        private PaymentResponseDto Map(Payment payment)
        {
            return new PaymentResponseDto
            {
                Id = payment.Id,
                Status = payment.Status.ToString(),
                CardNumberLastFour = payment.CardNumberLastFour,
                ExpiryMonth = payment.ExpiryMonth,
                ExpiryYear = payment.ExpiryYear,
                Amount = payment.Amount,
                Currency = payment.Currency
            };
        }
    }
}

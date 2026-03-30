using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentGateway.Application.Interfaces
{
    public interface IPaymentService
    {
        public Task<DTOs.PaymentResponseDto> ProcessAsync(DTOs.PaymentRequestDto request);

        public Task<DTOs.PaymentResponseDto> GetAsync(Guid id);
    }
}

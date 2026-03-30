using System;
using System.Collections.Generic;
using System.Text;

using PaymentGateway.Api.Models;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Infrastructure.Client;

namespace PaymentGateway.Application.Interfaces
{
    public interface IBankClient
    {
        Task<BankPaymentResponse> Process(PaymentRequestDto request);
    }
}

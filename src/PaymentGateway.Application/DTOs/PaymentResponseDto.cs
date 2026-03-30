using System;
using System.Collections.Generic;
using System.Text;

using PaymentGateway.Api.Models;

namespace PaymentGateway.Application.DTOs
{
    public class PaymentResponseDto
    {
        public Guid Id { get; set; }
        public string CardNumberLastFour { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public string Status { get; set; }
        public string Currency { get; set; }
        public int Amount { get; set; }

    }
}

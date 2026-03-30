using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentGateway.Application.DTOs
{
    public class PaymentRequestDto
    {
        public string CardNumber{ get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Currency { get; set; }
        public int Amount { get; set; }
        public string Cvv { get; set; }
    }
}

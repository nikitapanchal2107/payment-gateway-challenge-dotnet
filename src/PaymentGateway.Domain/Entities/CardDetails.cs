using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentGateway.Domain.Entities
{
    public class CardDetails
    {
        public string CardNumberLastFour { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public int Cvv { get; set;  }
    }
}

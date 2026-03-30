using System;
using System.Collections.Generic;
using System.Text;

namespace PaymentGateway.Application.Options
{
    public class PaymentGatewayOptions
    {
        public string BankSimulatorBaseUrl { get; set; }
        public List<string> AllowedCurrencies { get; set; } = new List<string>();
    }
}

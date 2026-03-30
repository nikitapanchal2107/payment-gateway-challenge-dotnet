using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace PaymentGateway.Infrastructure.Client
{

    public class BankPaymentResponse
    {
        
        [JsonPropertyName("authorized")]
        public bool Authorized { get; set; }
        [JsonPropertyName("authorization_code")]
        public string? AuthorizationCode { get; set; }
    }
}
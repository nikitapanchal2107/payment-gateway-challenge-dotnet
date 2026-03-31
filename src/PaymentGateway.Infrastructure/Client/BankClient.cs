using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;

using static PaymentGateway.Application.Interfaces.IBankClient;

namespace PaymentGateway.Infrastructure.Client
{
    public class BankClient : IBankClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BankClient> _logger;


        public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

        }

        public async Task<BankAuthorizationResult> Process(PaymentRequestDto request)
        {
            var cardLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4);

            _logger.LogInformation(
                "Initiating bank payment request. Amount={Amount}, Currency={Currency}, CardLastFour={CardLastFour}",
                request.Amount, request.Currency, cardLastFour);

            var bankRequest = new BankPaymentRequest
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };

            var response = await _httpClient.PostAsJsonAsync("payments", bankRequest);

            _logger.LogInformation(
                "Bank API responded. StatusCode={StatusCode}, CardLastFour={CardLastFour}",
                (int)response.StatusCode, cardLastFour);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Bank API returned error. StatusCode={StatusCode}, Amount={Amount}, Currency={Currency}, CardLastFour={CardLastFour}, Response={Response}",
                    (int)response.StatusCode, request.Amount, request.Currency, cardLastFour, errorContent);

                response.EnsureSuccessStatusCode(); // Throws HttpRequestException
            }

            var bankResponse = await response.Content.ReadFromJsonAsync<BankPaymentResponse>();

            if (bankResponse == null)
            {
                throw new InvalidOperationException("Bank response was null");
            }

            return new BankAuthorizationResult
            {
                Authorized = bankResponse.Authorized,
                AuthorizationCode = bankResponse.AuthorizationCode
            };
        }
        }

    }
    

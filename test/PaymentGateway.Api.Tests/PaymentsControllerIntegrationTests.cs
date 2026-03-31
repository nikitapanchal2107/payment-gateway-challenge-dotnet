using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Infrastructure.Client;

using Xunit;

using static PaymentGateway.Application.Interfaces.IBankClient;

namespace PaymentGateway.Api.Tests
{
    public class PaymentsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public PaymentsControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }
        #region Happy Path Tests

        [Fact]
        public async Task ProcessPayment_WithValidRequest_Authorized_ReturnsSuccessAndCanRetrieve()
        {
            // Arrange
            var client = CreateClientWithMockBank(authorized: true);
            var request = new PaymentRequestDto
            {
                CardNumber = "2222405343248113", // Odd = Authorized
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 100,
                Cvv = "123"
            };

            // Act - Create payment
            var createResponse = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert - Payment authorized
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
            var payment = await createResponse.Content.ReadFromJsonAsync<PaymentResponseDto>();
            Assert.NotNull(payment);
            Assert.Equal("Authorized", payment.Status);
            Assert.Equal("8113", payment.CardNumberLastFour);
            Assert.Equal(100, payment.Amount);
            Assert.Equal("USD", payment.Currency);

            // Act - Retrieve payment
            var getResponse = await client.GetAsync($"/api/Payments/{payment.Id}");

            // Assert - Can retrieve same payment
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var retrieved = await getResponse.Content.ReadFromJsonAsync<PaymentResponseDto>();
            Assert.Equal(payment.Id, retrieved!.Id);
            Assert.Equal("Authorized", retrieved.Status);
        }

        [Fact]
        public async Task ProcessPayment_WithValidRequest_Declined_ReturnsSuccessWithDeclinedStatus()
        {
            // Arrange
            var client = CreateClientWithMockBank(authorized: false);
            var request = new PaymentRequestDto
            {
                CardNumber = "2222405343248112", // Even = Declined
                ExpiryMonth = 6,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "GBP",
                Amount = 250,
                Cvv = "456"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var payment = await response.Content.ReadFromJsonAsync<PaymentResponseDto>();
            Assert.NotNull(payment);
            Assert.Equal("Declined", payment.Status);
            Assert.Equal("8112", payment.CardNumberLastFour);
            Assert.Equal(250, payment.Amount);
            Assert.Equal("GBP", payment.Currency);
        }

        #endregion

        #region Retrieval Tests

        [Fact]
        public async Task GetPayment_WithNonExistentId_Returns404()
        {
            // Arrange
            var client = _factory.CreateClient();
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await client.GetAsync($"/api/Payments/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Not Found", errorContent);
        }

        #endregion

        

        #region Helper Methods

        /// <summary>
        /// Creates an HTTP client with a mock bank that returns authorized/declined responses.
        /// </summary>
        private HttpClient CreateClientWithMockBank(bool authorized)
        {
            return _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace real BankClient with test version
                    var bankClientDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBankClient));
                    if (bankClientDescriptor != null)
                        services.Remove(bankClientDescriptor);

                    services.AddSingleton<IBankClient>(new MockBankClient(authorized));
                });
            }).CreateClient();
        }

        /// <summary>
        /// Creates a valid payment request with specified values.
        /// </summary>
        private PaymentRequestDto CreatePaymentRequest(string cardNumber, int amount, string currency)
        {
            return new PaymentRequestDto
            {
                CardNumber = cardNumber,
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = currency,
                Amount = amount,
                Cvv = "123"
            };
        }

        #endregion

        #region Mock Bank Client

        /// <summary>
        /// Mock bank client for testing - simulates authorized or declined responses.
        /// </summary>
        private class MockBankClient : IBankClient
        {
            private readonly bool _authorized;

            public MockBankClient(bool authorized)
            {
                _authorized = authorized;
            }

            public Task<BankAuthorizationResult> Process(PaymentRequestDto request)
            {
                return Task.FromResult(new BankAuthorizationResult
                {
                    Authorized = _authorized,
                    AuthorizationCode = _authorized ? $"AUTH{Guid.NewGuid():N}"[..10] : null
                });
            }
        }

        #endregion
    }
}

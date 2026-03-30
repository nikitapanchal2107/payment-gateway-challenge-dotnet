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

        #region Validation/Rejection Tests

        [Fact]
        public async Task ProcessPayment_WithInvalidCardNumber_ReturnsRejected400()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new PaymentRequestDto
            {
                CardNumber = "123", // Too short - invalid
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 100,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("14-19", errorContent);
            Assert.Contains("Rejected", errorContent);
        }

        [Fact]
        public async Task ProcessPayment_WithExpiredCard_ReturnsRejected400()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new PaymentRequestDto
            {
                CardNumber = "2222405343248113",
                ExpiryMonth = 1,
                ExpiryYear = 2020, // Expired
                Currency = "USD",
                Amount = 100,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("expired", errorContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ProcessPayment_WithUnsupportedCurrency_ReturnsRejected400()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new PaymentRequestDto
            {
                CardNumber = "2222405343248113",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "JPY", // Not allowed
                Amount = 100,
                Cvv = "123"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/Payments", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("Unsupported currency", errorContent);
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

        #region Multiple Payments Test

        [Fact]
        public async Task ProcessPayment_MultiplePayments_AllStoredIndependently()
        {
            // Arrange
            var client = CreateClientWithMockBank(authorized: true);
            var payment1 = CreatePaymentRequest("2222405343248113", 100, "USD");
            var payment2 = CreatePaymentRequest("2222405343248115", 200, "GBP");
            var payment3 = CreatePaymentRequest("2222405343248117", 300, "EUR");

            // Act - Create 3 payments
            var response1 = await client.PostAsJsonAsync("/api/Payments", payment1);
            var response2 = await client.PostAsJsonAsync("/api/Payments", payment2);
            var response3 = await client.PostAsJsonAsync("/api/Payments", payment3);

            // Assert - All succeed
            Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

            var result1 = await response1.Content.ReadFromJsonAsync<PaymentResponseDto>();
            var result2 = await response2.Content.ReadFromJsonAsync<PaymentResponseDto>();
            var result3 = await response3.Content.ReadFromJsonAsync<PaymentResponseDto>();

            // All have unique IDs
            Assert.NotEqual(result1!.Id, result2!.Id);
            Assert.NotEqual(result2.Id, result3!.Id);
            Assert.NotEqual(result1.Id, result3.Id);

            // All can be retrieved independently
            var get1 = await client.GetAsync($"/api/Payments/{result1.Id}");
            var get2 = await client.GetAsync($"/api/Payments/{result2.Id}");
            var get3 = await client.GetAsync($"/api/Payments/{result3.Id}");

            Assert.Equal(HttpStatusCode.OK, get1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get2.StatusCode);
            Assert.Equal(HttpStatusCode.OK, get3.StatusCode);
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

            public Task<BankPaymentResponse> Process(PaymentRequestDto request)
            {
                return Task.FromResult(new BankPaymentResponse
                {
                    Authorized = _authorized,
                    AuthorizationCode = _authorized ? $"AUTH{Guid.NewGuid():N}"[..10] : null
                });
            }
        }

        #endregion
    }
}

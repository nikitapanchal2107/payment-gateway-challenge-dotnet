using System.Net;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;

using Moq;
using Moq.Protected;

using PaymentGateway.Application.DTOs;
using PaymentGateway.Infrastructure.Client;

namespace PaymentGateway.Infrastructure.Tests
{

    public class BankClientTests
    {
        private readonly Mock<ILogger<BankClient>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly BankClient _bankClient;

        public BankClientTests()
        {
            _loggerMock = new Mock<ILogger<BankClient>>();
            _httpHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:5000")
            };
            _bankClient = new BankClient(_httpClient, _loggerMock.Object);
        }

        [Fact]
        public async Task Process_WhenBankAuthorizes_ReturnsAuthorizedResponse()
        {
            // Arrange
            var request = CreateValidRequest("2222405343248113"); // Odd number
            var bankResponse = new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "AUTH123"
            };

            SetupHttpResponse(HttpStatusCode.OK, bankResponse);

            // Act
            var result = await _bankClient.Process(request);

            // Assert
            Assert.True(result.Authorized);
            Assert.Equal("AUTH123", result.AuthorizationCode);
        }

        [Fact]
        public async Task Process_WhenBankDeclines_ReturnsDeclinedResponse()
        {
            // Arrange
            var request = CreateValidRequest("2222405343248112"); // Even number
            var bankResponse = new BankPaymentResponse
            {
                Authorized = false,
                AuthorizationCode = null
            };

            SetupHttpResponse(HttpStatusCode.OK, bankResponse);

            // Act
            var result = await _bankClient.Process(request);

            // Assert
            Assert.False(result.Authorized);
            Assert.Null(result.AuthorizationCode);
        }

        [Fact]
        public async Task Process_WhenBankReturns503_ThrowsHttpRequestException()
        {
            // Arrange - Card ending in 0 causes 503
            var request = CreateValidRequest("2222405343248110");
            SetupHttpResponse(HttpStatusCode.ServiceUnavailable, null);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _bankClient.Process(request));
        }

        [Fact]
        public async Task Process_WhenBankReturns400_ThrowsHttpRequestException()
        {
            // Arrange
            var request = CreateValidRequest();
            SetupHttpResponse(HttpStatusCode.BadRequest, null);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _bankClient.Process(request));
        }

        #region Helper Methods

        /// <summary>
        /// Sets up the mock HTTP handler to return a specific status code and response.
        /// </summary>
        private void SetupHttpResponse(HttpStatusCode statusCode, BankPaymentResponse? response)
        {
            var httpResponse = new HttpResponseMessage(statusCode);

            if (response != null)
            {
                httpResponse.Content = JsonContent.Create(response);
            }

            _httpHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);
        }

        /// <summary>
        /// Creates a valid payment request for testing.
        /// </summary>
        private PaymentRequestDto CreateValidRequest(string cardNumber = "1234567890123456")
        {
            return new PaymentRequestDto
            {
                CardNumber = cardNumber,
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 100,
                Cvv = "123"
            };
        }

        #endregion
    }
}

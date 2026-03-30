using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // <-- Add this using directive

using Moq;

using PaymentGateway.Api.Models;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Options;
using PaymentGateway.Application.Services;
using PaymentGateway.Application.Validator;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Infrastructure.Client;

namespace PaymentGateway.Application.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<IBankClient> _bankClientMock;
        private readonly Mock<IPaymentRepository> _repositoryMock;
        private readonly PaymentRequestValidator _validator;
        private readonly Mock<ILogger<PaymentService>> _loggerMock;
        private readonly PaymentService _service;

        public PaymentServiceTests()
        {
            _bankClientMock = new Mock<IBankClient>();
            _repositoryMock = new Mock<IPaymentRepository>();

            var options = Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
            {
                AllowedCurrencies = new List<string> { "USD", "GBP", "EUR" }
            });
            _validator = new PaymentRequestValidator(options);
            _loggerMock = new Mock<ILogger<PaymentService>>();

            _service = new PaymentService(
                _bankClientMock.Object,
                _repositoryMock.Object,
                _validator,
                _loggerMock.Object);
        }

        

        [Fact]
        public async Task ProcessAsync_BankAuthorizes_SavesPaymentWithAuthorizedStatus()
        {
            // Arrange
            var request = CreateValidRequest();
            var bankResponse = new BankPaymentResponse
            {
                Authorized = true,
                AuthorizationCode = "AUTH123"
            };

            _validator.Validate(request);
            _bankClientMock.Setup(b => b.Process(request)).ReturnsAsync(bankResponse);
            _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ProcessAsync(request);

            // Assert
            Assert.Equal("Authorized", result.Status);
            Assert.Equal("3456", result.CardNumberLastFour);
            _repositoryMock.Verify(r => r.SaveAsync(It.Is<Payment>(p =>
                p.Status == PaymentStatus.Authorized &&
                p.CardNumberLastFour == "3456" &&
                p.Amount == 100)), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_BankDeclines_SavesPaymentWithDeclinedStatus()
        {
            // Arrange
            var request = CreateValidRequest();
            var bankResponse = new BankPaymentResponse { Authorized = false };

            _validator.Validate(request);
            _bankClientMock.Setup(b => b.Process(request)).ReturnsAsync(bankResponse);
            _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ProcessAsync(request);

            // Assert
            Assert.Equal("Declined", result.Status);
            _repositoryMock.Verify(r => r.SaveAsync(It.Is<Payment>(p =>
                p.Status == PaymentStatus.Declined)), Times.Once);
        }

        [Fact]
        public async Task ProcessAsync_SavesCardLastFourDigitsOnly()
        {
            // Arrange
            var request = CreateValidRequest();
            request.CardNumber = "1234567890123456";
            var bankResponse = new BankPaymentResponse { Authorized = true };

            _validator.Validate(request);
            _bankClientMock.Setup(b => b.Process(request)).ReturnsAsync(bankResponse);
            _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);

            // Act
            await _service.ProcessAsync(request);

            // Assert
            _repositoryMock.Verify(r => r.SaveAsync(It.Is<Payment>(p =>
                p.CardNumberLastFour == "3456")), Times.Once);
        }

        [Fact]
        public async Task GetAsync_ExistingPayment_ReturnsPaymentDetails()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = paymentId,
                CardNumberLastFour = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 100,
                Status = PaymentStatus.Authorized
            };

            _repositoryMock.Setup(r => r.GetAsync(paymentId)).ReturnsAsync(payment);

            // Act
            var result = await _service.GetAsync(paymentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentId, result.Id);
            Assert.Equal("Authorized", result.Status);
            Assert.Equal("1234", result.CardNumberLastFour);
        }

        [Fact]
        public async Task GetAsync_NonExistentPayment_ReturnsNull()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetAsync(paymentId)).ReturnsAsync((Payment?)null);

            // Act
            var result = await _service.GetAsync(paymentId);

            // Assert
            Assert.Null(result);
        }

        private PaymentRequestDto CreateValidRequest()
        {
            return new PaymentRequestDto
            {
                CardNumber = "1234567890123456",
                ExpiryMonth = 12,
                ExpiryYear = DateTime.UtcNow.Year + 1,
                Currency = "USD",
                Amount = 100,
                Cvv = "123"
            };
        }
    }
}

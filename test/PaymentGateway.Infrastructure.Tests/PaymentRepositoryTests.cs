using PaymentGateway.Api.Models;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Infrastructure.Repository;

namespace PaymentGateway.Infrastructure.Tests
{
    public class PaymentRepositoryTests
    {
        private readonly PaymentRepository _repository;

        public PaymentRepositoryTests()
        {
            _repository = new PaymentRepository();
        }

        [Fact]
        public async Task SaveAsync_ValidPayment_SavesSuccessfully()
        {
            // Arrange
            var payment = CreatePayment();

            // Act
            await _repository.SaveAsync(payment);
            var retrieved = await _repository.GetAsync(payment.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(payment.Id, retrieved.Id);
            Assert.Equal(payment.CardNumberLastFour, retrieved.CardNumberLastFour);
            Assert.Equal(payment.Status, retrieved.Status);
        }

        [Fact]
        public async Task GetAsync_NonExistentPayment_ReturnsNull()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _repository.GetAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        
        [Fact]
        public async Task GetAsync_RetrievesCompletePaymentData()
        {
            // Arrange
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                CardNumberLastFour = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 500,
                Status = PaymentStatus.Authorized
            };

            await _repository.SaveAsync(payment);

            // Act
            var retrieved = await _repository.GetAsync(payment.Id);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("1234", retrieved.CardNumberLastFour);
            Assert.Equal(12, retrieved.ExpiryMonth);
            Assert.Equal(2025, retrieved.ExpiryYear);
            Assert.Equal("USD", retrieved.Currency);
            Assert.Equal(500, retrieved.Amount);
            Assert.Equal(PaymentStatus.Authorized, retrieved.Status);
        }

        private Payment CreatePayment()
        {
            return new Payment
            {
                Id = Guid.NewGuid(),
                CardNumberLastFour = "3456",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = 100,
                Status = PaymentStatus.Authorized
            };
        }
    }
}

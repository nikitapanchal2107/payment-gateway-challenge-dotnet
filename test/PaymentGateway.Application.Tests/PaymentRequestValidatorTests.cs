
using System.ComponentModel.DataAnnotations;
using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Options;

using PaymentGateway.Application.Validator;

namespace PaymentGateway.Application.Tests
{
    public class PaymentRequestValidatorTests
    {
        private readonly PaymentRequestValidator _validator;
        public PaymentRequestValidatorTests()
        {
            var options = Microsoft.Extensions.Options.Options.Create(new PaymentGatewayOptions
            {
                AllowedCurrencies = new List<string> { "USD", "GBP", "EUR" }
            });
            _validator = new PaymentRequestValidator(options);
        }

        #region Card Number Tests

        [Theory]
        [InlineData("12345678901234")]      // 14 digits
        [InlineData("1234567890123456")]    // 16 digits
        [InlineData("1234567890123456789")] // 19 digits
        public void Validate_ValidCardNumber_DoesNotThrow(string cardNumber)
        {
            var request = CreateValidRequest();
            request.CardNumber = cardNumber;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("123456789012")]         // Too short
        [InlineData("12345678901234567890")] // Too long
        public void Validate_InvalidCardNumberLength_ThrowsValidationException(string cardNumber)
        {
            var request = CreateValidRequest();
            request.CardNumber = cardNumber;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("14-19", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Validate_CardNumberNullOrEmpty_ThrowsValidationException(string cardNumber)
        {
            var request = CreateValidRequest();
            request.CardNumber = cardNumber!;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("123456789012345a")]     // Contains letter
        [InlineData("1234 5678 9012 3456")]  // Contains spaces
        public void Validate_CardNumberNonNumeric_ThrowsValidationException(string cardNumber)
        {
            var request = CreateValidRequest();
            request.CardNumber = cardNumber;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("numeric", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Expiry Tests

        [Theory]
        [InlineData(1)]
        [InlineData(6)]
        [InlineData(12)]
        public void Validate_ValidExpiryMonth_DoesNotThrow(int month)
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = month;
            request.ExpiryYear = DateTime.UtcNow.Year + 1;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-1)]
        public void Validate_InvalidExpiryMonth_ThrowsValidationException(int month)
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = month;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("1 and 12", exception.Message);
        }

        [Fact]
        public void Validate_FutureExpiryDate_DoesNotThrow()
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = 12;
            request.ExpiryYear = DateTime.UtcNow.Year + 1;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_CurrentMonthExpiry_DoesNotThrow()
        {
            var request = CreateValidRequest();
            var now = DateTime.UtcNow;
            request.ExpiryMonth = now.Month;
            request.ExpiryYear = now.Year;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_ExpiredCard_ThrowsValidationException()
        {
            var request = CreateValidRequest();
            request.ExpiryMonth = 1;
            request.ExpiryYear = DateTime.UtcNow.Year - 1;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Equal("Card expired", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_InvalidExpiryYear_ThrowsValidationException(int year)
        {
            var request = CreateValidRequest();
            request.ExpiryYear = year;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("year", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Currency Tests

        [Theory]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("EUR")]
        public void Validate_AllowedCurrency_DoesNotThrow(string currency)
        {
            var request = CreateValidRequest();
            request.Currency = currency;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("usd")]
        [InlineData("Usd")]
        public void Validate_CurrencyCaseInsensitive_DoesNotThrow(string currency)
        {
            var request = CreateValidRequest();
            request.Currency = currency;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Fact]
        public void Validate_UnsupportedCurrency_ThrowsValidationException()
        {
            var request = CreateValidRequest();
            request.Currency = "JPY";

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("Unsupported currency", exception.Message);
        }

        [Theory]
        [InlineData("US")]    // Too short
        [InlineData("USDD")]  // Too long
        public void Validate_InvalidCurrencyLength_ThrowsValidationException(string currency)
        {
            var request = CreateValidRequest();
            request.Currency = currency;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("3", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Validate_CurrencyNullOrEmpty_ThrowsValidationException(string currency)
        {
            var request = CreateValidRequest();
            request.Currency = currency!;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Amount Tests

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(999999)]
        public void Validate_PositiveAmount_DoesNotThrow(int amount)
        {
            var request = CreateValidRequest();
            request.Amount = amount;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public void Validate_ZeroOrNegativeAmount_ThrowsValidationException(int amount)
        {
            var request = CreateValidRequest();
            request.Amount = amount;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region CVV Tests

        [Theory]
        [InlineData("123")]
        [InlineData("1234")]
        public void Validate_ValidCvv_DoesNotThrow(string cvv)
        {
            var request = CreateValidRequest();
            request.Cvv = cvv;

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        [Theory]
        [InlineData("12")]     // Too short
        [InlineData("12345")]  // Too long
        public void Validate_InvalidCvvLength_ThrowsValidationException(string cvv)
        {
            var request = CreateValidRequest();
            request.Cvv = cvv;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("3–4", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Validate_CvvNullOrEmpty_ThrowsValidationException(string cvv)
        {
            var request = CreateValidRequest();
            request.Cvv = cvv!;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("12a")]   // Contains letter
        [InlineData("1 3")]   // Contains space
        public void Validate_CvvNonNumeric_ThrowsValidationException(string cvv)
        {
            var request = CreateValidRequest();
            request.Cvv = cvv;

            var exception = Assert.Throws<ValidationException>(() => _validator.Validate(request));
            Assert.Contains("numeric", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Full Request Tests

        [Fact]
        public void Validate_CompletelyValidRequest_DoesNotThrow()
        {
            var request = CreateValidRequest();

            var exception = Record.Exception(() => _validator.Validate(request));

            Assert.Null(exception);
        }

        #endregion

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

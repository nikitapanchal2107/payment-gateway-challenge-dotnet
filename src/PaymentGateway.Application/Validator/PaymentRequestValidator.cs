using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

using Microsoft.Extensions.Options;

using PaymentGateway.Application.DTOs;
using PaymentGateway.Application.Options;

namespace PaymentGateway.Application.Validator
{
    public class PaymentRequestValidator
    {
        private readonly PaymentGatewayOptions _options;
        public PaymentRequestValidator(IOptions<PaymentGatewayOptions> options)
        {
            _options = options.Value;
        }
        public void Validate(PaymentRequestDto r)
        {
            ValidateCardNumber(r.CardNumber);
            ValidateExpiry(r.ExpiryMonth, r.ExpiryYear);
            ValidateCurrency(r.Currency);
            ValidateAmount(r.Amount);
            ValidateCvv(r.Cvv);

        }

        private void ValidateCardNumber(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ValidationException("Card number is required.");

            if (cardNumber.Length is < 14 or > 19)
                throw new ValidationException("Card number must be 14-19 digits long.");

            if (!cardNumber.All(char.IsDigit))
                throw new ValidationException("Card number must contain only numeric characters.");

        }

        private void ValidateExpiry(int month, int year)
        {

            if (month < 1 || month > 12)
                throw new ValidationException("Expiry month must be between 1 and 12.");

            if (year < 1 || year > 9999)
                throw new ValidationException("Expiry year must be a valid year.");

            var now = DateTime.UtcNow;
            var expiry = new DateTime(year, month, 1).AddMonths(1);
            if (expiry <= now)
                throw new ValidationException("Card expired");
        }

        private void ValidateCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ValidationException("Currency is required.");

            if (currency.Length != 3)
                throw new ValidationException("Currency must be a 3‑letter ISO code.");

            if (!_options.AllowedCurrencies.Contains(currency.ToUpperInvariant()))
                throw new ValidationException("Unsupported currency.");
        }

        private void ValidateAmount(int amount)
        {
            if (amount <= 0)
                throw new ValidationException("Amount must be a positive integer.");
        }

        private void ValidateCvv(string cvv)
        {
            if (string.IsNullOrWhiteSpace(cvv))
                throw new ValidationException("CVV is required.");

            if (cvv.Length is < 3 or > 4)
                throw new ValidationException("CVV must be 3–4 digits long.");

            if (!cvv.All(char.IsDigit))
                throw new ValidationException("CVV must contain only numeric characters.");
        }

    }
}

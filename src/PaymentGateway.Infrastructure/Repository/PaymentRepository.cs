using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Infrastructure.Repository
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly Dictionary<Guid, Payment> _payments = new();

        public Task SaveAsync(Payment payment)
        {
            _payments[payment.Id] = payment;
            return Task.CompletedTask;
        }

        public Task<Payment> GetAsync(Guid id)
        {
            _payments.TryGetValue(id, out var payment);
            return Task.FromResult(payment);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Interfaces
{
    public interface IPaymentRepository
    {
        public Task SaveAsync(Payment payment);
        public Task<Payment> GetAsync(Guid id);
    }
}

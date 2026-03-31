# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

##  API Endpoints

### **POST /api/Payments**
Create a payment.

**Responses:**
- `200 OK` — Authorized or Declined  
- `400 Bad Request` — Validation failure  
- `500 Internal Server Error` — Unexpected error  

---

### **GET /api/Payments/{id}**
Retrieve an existing payment.

**Responses:**
- `200 OK` — Payment found  
- `404 Not Found` — Payment does not exist  

---

## 🧩 Design Considerations and Assumptions

This section outlines the architectural decisions, assumptions, and rationale behind the implementation.

---

### Clean Architecture

**Assumption:** Clean separation of concerns is essential for maintainability.  
**Decision:** Implemented a 4‑layer architecture:
- API Layer - Controllers, middleware
- Application Layer - Business logic, services, DTOs, validators 
- Domain Layer - Core entities, enums, Core business rules
- Infrastructure Layer  - External integrations (bank client, repository) 

**Rationale:** Payment Gateway Domain: 
- Complex validation rules (expiry, currency, CVV) 
- External dependencies (bank API) 
- Security critical 
- High testability requirement (financial accuracy) 

- Future-Proofing: 
- Easy to add features (refunds, fraud detection) 
- Easy to swap implementations (different banks, databases) 
- Easy to test each layer independently 
- Clear boundaries for team collaboration 

- Production Readiness: 
- Follows industry standards (SOLID principles) 
- Easy to onboard new team members 
- Clear separation of concerns 

---

### Dependency Injection

**Assumption:** Loose coupling and testability are required.  
**Decision:** All dependencies injected via constructor injection.  
**Rationale:** Supports mocking and enforces explicit dependencies.

---

### Repository Pattern

**Assumption:** Data access should be abstracted.  
**Decision:** `IPaymentRepository` with in‑memory implementation.  
**Rationale:** Allows future database swap without changing business logic.

---

### Centralized Validation

**Assumption:** Payment validation is complex.  
**Decision:** Implemented `PaymentRequestValidator`.  
**Rationale:**
- Handles expiry logic  
- Config‑driven currency validation  
- Clear error messages  
- Reusable logic  

---

### Card Number Validation

- Accepts **14–19 digit numeric** strings  
- Required field  

---

### Expiry Date Validation

- Cards expire at **end of month**  
- Current month is considered valid  

---

### Currency Validation

- Configurable whitelist (USD, GBP, EUR)  
- Case‑insensitive  

---

### CVV Validation

- Accepts **3–4 digits**  

---

### Security — Card Masking

- Only last **4 digits** stored and returned  
- Prevents sensitive data exposure  

---

### Error Messages

- Generic messages  
- No sensitive information leaked  

---

### Bank Integration

- Last digit **odd** → Authorized  
- Last digit **even** → Declined  

Matches simulator specification.

---

### Payment Status

- **Rejected** → Validation failure (400)  
- **Declined** → Bank declined (200)  
- **Authorized** → Bank approved (200)  

---

### In‑Memory Storage

- `Dictionary`  
- No persistent DB required for exercise  

---

### Exception Handling Middleware

- Converts `ValidationException` → 400  
- Unexpected errors → 500  
- Standardized **ProblemDetails** responses  

---

### Retry Logic (Polly)

- Retries on timeouts, connection failures, HTTP 5xx  
- Exponential backoff: **2s → 4s → 8s**  
- Prevents cascading failures  

---

## Testing Strategy

###  Unit Tests

- `PaymentRequestValidatorTests`  
- `PaymentServiceTests`  
- `PaymentRepositoryTests`  
- `BankClientTests`  

### Integration Tests

- `PaymentsControllerIntegrationTests` using `WebApplicationFactory`

###  Coverage Includes

- Valid/invalid card numbers  
- Expired cards  
- Unsupported currencies  
- Concurrent payments  
- Retrieval of existing/non‑existent payments  

---

## ▶️ How to Run

```bash
git clone https://github.com/nikitapanchal2107/payment-gateway-challenge-dotnet
cd payment-gateway-challenge-dotnet
dotnet build
dotnet test
dotnet run --project src/PaymentGateway.Api

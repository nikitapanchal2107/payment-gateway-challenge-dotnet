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

Feel free to change the structure of the solution, use a different test library etc.

Design Considerations and Assumptions

Architecture & Design Patterns

Layered Architecture

•	Assumption: Clean separation of concerns is essential for maintainability using Solid principles
•	Decision: Implemented a 4-layer architecture:
•	API Layer: Controllers, middleware, API-specific concerns
•	Application Layer: Business logic, services, DTOs, validators
•	Domain Layer: Core entities, enums, business rules
•	Infrastructure Layer: External integrations (bank client, repository)
•	Rationale: Enables independent testing of each layer and easier maintenance

Dependency Injection

•	Assumption: The application needs testability and loose coupling
•	Decision: All dependencies injected via constructor injection
•	Rationale: Facilitates unit testing with mocks and follows SOLID principles

Repository Pattern

•	Assumption: Data access should be abstracted from business logic
•	Decision: IPaymentRepository interface with in-memory implementation(List)
•	Rationale: If in the future will have to swap with real database without changing business logic

Validation Strategy - Centralized Validation

•	Assumption: Payment validation rules are complex and require dedicated logic
•	Decision: Created PaymentRequestValidator class rather than relying solely on data annotations
•	Rationale: Complex validation logic (e.g., expiry date validation)
•	Configuration-driven validation (allowed currencies from settings using IOption)
•	Clear error messages
•	Reusable validation logic

Card Number Validation

•	Assumption: Standard card numbers are 14-19 digits
•	Decision: Accept only numeric strings in this range and it is required field


Expiry Date Validation

•	Assumption: Cards expire at the end of the expiry month
•	Decision: Accept cards that expire in the current month as it indicates future


Currency Validation

•	Assumption: Gateway supports limited currencies
•	Decision: Configurable whitelist via appsettings.json (USD, GBP, EUR)
•	Rationale: Business requirement to limit supported currencies and case-insensitive

CVV Validation

•	Assumption: Standard CVV is 3-4 digits
•	Decision: Accept both lengths
•	Rationale: Business requirement to consider 3-4 digits

Security Considerations
Card Number Masking
•	Assumption: Full card numbers must not be stored or returned
•	Decision: Store and return only last 4 digits
•	Rationale: It is serious compliance risk to show full card number


Error Messages

•	Assumption: Error messages should not leak sensitive information
•	Decision: Generic validation messages without exposing internal details
•	Rationale: Prevents information disclosure attacks

Payment Processing Logic - Bank Integration

•	Assumption: Bank simulator determines authorization based on card number
•	Decision: Last digit odd = authorized, even = declined
•	Rationale: Matches provided bank simulator specification

Payment Status

•	Assumption: Three possible outcomes: Authorized, Declined, Rejected
•	Decision:
•	Rejected: Validation failure (400 Bad Request)
•	Declined: Bank declined (200 OK with status)
•	Authorized: Bank approved (200 OK with status)
•	Rationale: Distinguishes between request problems and legitimate declines

Idempotency
•	Assumption: Each payment request creates a new payment
•	Decision: No idempotency keys implemented
•	Rationale: Out of scope for take-home; production would need this

Data Storage - In-Memory Storage

•	Assumption: Persistent database not required for exercise
•	Decision: Concurrent dictionary for thread-safe in-memory storage
•	Rationale: Simplifies setup; demonstrates understanding of thread safety

Error Handling - Exception Handling Middleware

•	Assumption: Consistent error responses needed across all endpoints
•	Decision: Global exception handling middleware
•	Rationale:
	Converts ValidationException → 400 Bad Request
	Handles unexpected errors → 500 Internal Server Error
	Standardized error format (Problem Details)

Retry

•	Assumption: Timeout error,Connection failure,Http 5xx server error
•	Decision: Added simple retry mechanism with 3 times try with 2 second delay between each call using Polly without circuit breaker mechanism
•	Rationale: It would prevent cascading failure.


Testing Strategy
Unit Tests

•	Assumption: Each component should be tested in isolation
•	Decision: Separate test projects for each layer
•	PaymentRequestValidatorTests: Validation logic
•	PaymentServiceTests: Business logic with mocked dependencies
•	PaymentRepositoryTests: Data access
•	BankClientTests: External integration

Integration Tests

•	Assumption: End-to-end behavior needs verification
•	Decision: PaymentsControllerIntegrationTests with WebApplicationFactory
•	Rationale: Tests full request pipeline with mock bank client

Test Coverage

•	Decision: Test happy paths, edge cases, and error scenarios
•	Examples:
•	Valid/invalid card numbers
•	Expired cards
•	Unsupported currencies
•	Multiple concurrent payments
•	Retrieval of existing/non-existent payments

API Design-RESTful Conventions

•	Assumption: Standard HTTP semantics expected
•	Decision:
•	POST /api/Payments - Create payment
•	GET /api/Payments/{id} - Retrieve payment
•	HTTP Status Codes:
•	200 OK: Successful processing (including declined)
•	400 Bad Request: Validation failure
•	404 Not Found: Payment not found
•	500 Internal Server Error: Unexpected errors

DTOs

•	Assumption: API contracts should be stable and decoupled from domain
•	Decision: Separate PaymentRequestDto and PaymentResponseDto
•	Rationale: Prevents exposing internal domain structure

Configuration - Options Pattern

•	Assumption: Configuration should be external and type-safe
•	Decision: PaymentGatewayOptions class with strongly-typed settings.
•	Rationale: Compile-time checking, easier testing, follows .NET best practices

Environment - Specific Settings

•	Assumption: Different environments may have different configurations
•	Decision: appsettings.json with configurable bank URL and currencies owever I haven't added all configuration in appsettings.
•	Rationale: Enables dev/staging/production variations



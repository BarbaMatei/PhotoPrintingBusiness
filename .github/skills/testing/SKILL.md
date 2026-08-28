---
name: testing
description: Testing conventions and patterns for FotoTipar. Use this skill when writing unit tests, integration tests, or E2E tests for both the Angular frontend (Jasmine/Jest) and ASP.NET Core backend (xUnit).
---

## Testing Strategy

### Backend (ASP.NET Core — xUnit)

#### Unit Tests

- **Location**: `src/PhotoPrint.Tests/Unit/`
- **Framework**: xUnit + Moq + FluentAssertions
- **What to test**:
  - Service methods (business logic)
  - Validators (FluentValidation rules)
  - State machine transitions (OrderStatusMachine)
  - Token generation and validation
  - MIME magic byte validation
  - Price calculations
- **Patterns**:
  - Arrange-Act-Assert
  - One assertion per test (or closely related assertions)
  - Mock all dependencies via interfaces
  - Use `InMemoryDatabase` for simple EF queries; real DB for complex queries
  - Name convention: `MethodName_StateUnderTest_ExpectedBehavior`

#### Integration Tests

- **Location**: `src/PhotoPrint.Tests/Integration/`
- **Framework**: xUnit + `WebApplicationFactory<Program>` + Testcontainers (PostgreSQL)
- **What to test**:
  - Full API endpoint flows (HTTP request → response)
  - Database operations (migrations, queries, constraints)
  - Middleware behavior (error handling, CORS, rate limiting)
  - Authentication/authorization enforcement
  - Webhook handling (Stripe)
- **Patterns**:
  - Use `WebApplicationFactory` to create test server
  - Use Testcontainers for real PostgreSQL instance
  - Seed test data in test setup, clean up in teardown
  - Test both happy path and error cases
  - Verify HTTP status codes AND response body content

#### Backend Test Example

```csharp
[Fact]
public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.EmailExistsAsync("test@email.com")).ReturnsAsync(true);
    var service = new AuthService(mockRepo.Object, ...);

    // Act & Assert
    await Assert.ThrowsAsync<ConflictException>(
        () => service.RegisterAsync(new RegisterRequest { Email = "test@email.com" })
    );
}
```

### Frontend (Angular — Jasmine + Karma / Jest)

#### Unit Tests

- **Location**: co-located `.spec.ts` files next to components/services
- **Framework**: Jasmine + Karma (Angular default) or Jest
- **What to test**:
  - Component rendering and interaction
  - Service method logic (mock HTTP calls)
  - Form validation rules
  - Guards (route access decisions)
  - Interceptors (header attachment, error handling)
  - Pipes (currency formatting, status labels)
- **Patterns**:
  - Use `TestBed` for component tests
  - Mock services with `jasmine.createSpyObj()` or `jest.fn()`
  - Use `HttpClientTestingModule` for HTTP service tests
  - Test DOM interactions via `fixture.debugElement`

#### E2E Tests

- **Location**: `e2e/`
- **Framework**: Cypress or Playwright
- **What to test**:
  - Full user flows (register → upload → checkout → confirm)
  - Guest checkout flow
  - Admin order workflow
  - Cart persistence across page reloads
- **Patterns**:
  - Page Object Model for reusable selectors
  - Test against running backend (or mock API with interceptors)
  - Use `data-testid` attributes for stable selectors
  - Reset test state between tests

#### Frontend Test Example

```typescript
describe('RegisterComponent', () => {
  it('should disable submit when GDPR not checked', () => {
    const button = fixture.debugElement.query(By.css('button[type="submit"]'));
    expect(button.nativeElement.disabled).toBeTruthy();

    component.form.get('gdprConsent')?.setValue(true);
    fixture.detectChanges();
    expect(button.nativeElement.disabled).toBeFalsy();
  });
});
```

## Test Coverage Targets

- **Backend services**: 80%+ line coverage
- **Backend validators**: 100% rule coverage
- **Frontend components**: 70%+ branch coverage
- **Frontend services**: 80%+ coverage
- **E2E**: cover all critical user paths

## Test Data

- Use factories or builders for test data creation
- Never use production data in tests
- Seed realistic Romanian test data (names, addresses, phone numbers)

## CI Integration

- Run all unit tests on every PR
- Run integration tests on merge to main
- Run E2E tests nightly or before release
- Fail PR if coverage drops below thresholds

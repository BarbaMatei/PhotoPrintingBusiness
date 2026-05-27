---
bolt: 001-error-handling-logging
created: 2026-05-05T15:51:00Z
status: accepted
superseded_by: null
---

# ADR-002: Custom ValidationFilter Overrides [ApiController] 400 Behavior

## Context

ASP.NET Core's `[ApiController]` attribute automatically short-circuits requests with invalid ModelState and returns `HTTP 400 Bad Request` in the `ValidationProblemDetails` format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Email": ["The Email field is required."]
  }
}
```

FotoTipar's API conventions (api-conventions.md) specify `422 Unprocessable Entity` for validation errors in the format:

```json
{
  "errors": [
    { "field": "email", "message": "Câmpul email este obligatoriu." }
  ]
}
```

These two behaviors are incompatible. The `[ApiController]` default uses 400, PascalCase field names, and English messages from data annotation attributes. The project requires 422, camelCase field names, and Romanian messages from FluentValidation validators.

## Decision

Suppress the `[ApiController]` automatic model state response by configuring `ApiBehaviorOptions.SuppressModelStateInvalidFilter = true` and register a custom `ValidationFilter` (`IActionFilter`) globally. The filter intercepts FluentValidation-populated ModelState errors and returns `422` with the `{ errors: [{field, message}] }` shape defined in api-conventions.md.

## Rationale

FluentValidation with `AddFluentValidationAutoValidation()` populates `ModelState` exactly like data annotations, which means the `[ApiController]` filter fires before any custom filter. Suppressing the built-in behavior gives full control over:

1. The HTTP status code (422 vs 400)
2. The response body shape (array of `{field, message}` vs nested ProblemDetails)
3. The language of error messages (Romanian via FluentValidation validators vs English from framework defaults)
4. Field name casing (camelCase via `JsonNamingPolicy.CamelCase.ConvertName` vs PascalCase from ModelState keys)

### Alternatives Considered

| Alternative | Pros | Cons | Why Rejected |
|-------------|------|------|--------------|
| Use `[ApiController]` default (400) | Zero configuration | Returns 400 instead of 422; PascalCase keys; English messages | Rejected — violates api-conventions.md and Romanian UI requirement |
| Customize `InvalidModelStateResponseFactory` in `ApiBehaviorOptions` | Keeps 400 handling centralized | Still returns 400; no clean way to transform to 422 with our shape | Rejected — cannot change status code from 400 to 422 via this factory |
| Disable `[ApiController]` entirely | Full control | Loses other `[ApiController]` conveniences (automatic binding, route inference) | Rejected — too disruptive; we only need to suppress the validation filter |
| Custom `ValidationProblemDetails` subclass | Stays within ASP.NET Core conventions | Cannot change 400 to 422; response shape still differs | Rejected — cannot satisfy api-conventions.md shape requirement |

## Consequences

### Positive

- All validation errors return 422 with `{ errors: [{field, message}] }` — consistent across entire API
- Romanian error messages from FluentValidation validators are surfaced correctly
- Field names are camelCase in responses, matching JSON property naming convention
- Single place to modify validation error format for the entire API

### Negative

- Deviates from `[ApiController]` default — developers must know about `SuppressModelStateInvalidFilter = true`
- If a future developer adds data annotation validators (`[Required]`, `[MaxLength]`) alongside FluentValidation, their messages may appear in English (if not also added to FluentValidation validators)

### Risks

- **Risk**: Developers add `[Required]` data annotations instead of FluentValidation rules, bypassing Romanian messages. **Mitigation**: Add a coding standards note prohibiting data annotation validators; all validation must go through FluentValidation.
- **Risk**: FluentValidation's `AddFluentValidationAutoValidation()` behavior changes in a future version. **Mitigation**: Pin FluentValidation.AspNetCore version in NuGet.

## Related

- **Stories**: US-1-005 (fluentvalidation-integration)
- **Standards**: api-conventions.md (422 validation error format), coding-standards.md
- **Previous ADRs**: none

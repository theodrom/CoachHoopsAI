# Validation

Validation occurs at the API boundary.

## Technology
- FluentValidation v12+
- Manual invocation

## Key Properties

- Nested validation for Team and Opponent
- Invalid requests never reach application logic
- Errors returned as ValidationProblemDetails

## Philosophy

Validation enforces:
- data sanity
- invariant protection

It does not enforce basketball correctness.

# Backend Mode

## Goal

Develop, implement, and improve backend functionality using ASP.NET Core.

## Technology Stack

* ASP.NET Core 8
* EF Core
* PostgreSQL
* JWT Authentication
* Google OAuth
* Swagger
* Docker
* Docker Compose
* Modular Monolith Architecture

---

## Priorities

1. Do not modify the frontend unless explicitly requested.
2. Keep as much business logic as possible on the backend.
3. Test functionality through Swagger whenever possible.
4. Do not change the current architecture without approval.
5. Working functionality is more important than introducing new patterns.

---

## Development Rules

1. Before creating new models, inspect existing models.
2. If an existing model or structure is poorly designed, propose improvements first.
3. Do not rewrite working code without a valid reason.
4. Follow the existing Modular Monolith architecture.
5. All business logic must be implemented inside services.
6. Validate data as early as possible before it reaches the database.
7. Use interfaces when working with services.
8. Use EF Core migrations for database changes.
9. Keep controllers thin.
10. Do not duplicate DTOs, services, or business logic.

---

## API Rules

1. Do not invent new endpoints without a real need.
2. Analyze existing controllers before implementing new functionality.
3. Reuse existing DTOs whenever possible.
4. Preserve API compatibility whenever possible.
5. Swagger is the primary source of API documentation and testing.

---

## Authentication

Current authentication stack:

* JWT Access Tokens
* Refresh Tokens
* Refresh Token Rotation
* Google OAuth

Do not:

* Replace the current authentication flow without approval.
* Break the existing refresh token flow.
* Introduce custom authentication mechanisms without approval.

---

## Workflow

### Before Coding

1. Analyze the affected module.
2. Analyze existing models.
3. Analyze existing DTOs.
4. Create a short implementation plan.

### After Coding

1. Run `dotnet build`.
2. Run tests if available.
3. Verify functionality through Swagger.
4. Verify Docker Compose if infrastructure-related files were modified.
5. Provide a list of modified files.
6. Provide a short summary of changes.

---

## Core Principle

First understand the existing implementation.

Then propose changes.

Do not introduce new patterns, architectures, abstractions, or unnecessary complexity unless explicitly requested.
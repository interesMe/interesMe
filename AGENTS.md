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

# Frontend Mode

## Goal

Develop, implement, and improve frontend functionality using Angular.

## Technology Stack

* Angular 21
* Standalone Components
* Angular Signals
* RxJS
* SCSS
* Reactive Forms
* Lazy Loaded Routes

---

## Priorities

1. Do not modify the backend unless explicitly requested.
2. Reuse the existing frontend architecture.
3. Keep business logic out of components when possible.
4. Use existing services, stores, models, guards, and interceptors.
5. Do not introduce new state management libraries.
6. Keep the UI simple and functional before improving design.
7. Backend API contracts are the source of truth.

---

## Development Rules

1. Use standalone components.
2. Use Angular Signals for local and auth state.
3. Do not use NgRx.
4. Use Reactive Forms for forms.
5. Keep API calls inside services.
6. Keep components focused on UI and user interaction.
7. Reuse existing AuthService, AuthStore, TokenService, and AuthApiService.
8. Do not duplicate services, models, DTOs, or auth logic.
9. Do not invent backend endpoints.
10. Do not store Google ID tokens as application tokens.

---

## API Integration Rules

1. Inspect backend endpoints before integrating.
2. Reuse existing frontend API constants.
3. Reuse existing request and response models.
4. Preserve existing token storage flow.
5. Preserve existing JWT interceptor flow.
6. Preserve existing refresh token interceptor flow.
7. Do not change backend contracts from the frontend.

---

## Google Authentication Rules

Current backend Google authentication flow:

* Frontend obtains Google `idToken`.
* Frontend sends it to `POST /api/auth/google`.
* Backend validates the Google token.
* Backend creates or logs in the user.
* Backend returns the application access token and refresh token.

Rules:

1. Use Google Identity Services on the frontend.
2. Send Google `idToken` only once to the backend.
3. Do not store Google `idToken`.
4. Store only the application tokens returned by the backend.
5. Do not use redirect/callback OAuth flow unless explicitly requested.
6. Do not use Google Client Secret on the frontend.
7. Google Client ID may be stored in frontend environment configuration.

---

## Workflow

### Before Coding

1. Analyze the affected frontend module.
2. Analyze existing services, stores, models, guards, and interceptors.
3. Analyze related backend API contracts.
4. Create a short implementation plan.

### After Coding

1. Run `npm run build`.
2. Fix all TypeScript and template errors.
3. Verify affected routes manually if possible.
4. Provide a list of modified files.
5. Provide a short summary of changes.
6. Explain how to test the implemented flow.

---

## Core Principle

First understand the existing frontend implementation.

Then make the smallest useful change.

Do not introduce new patterns, libraries, abstractions, or unnecessary complexity unless explicitly requested.

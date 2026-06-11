# InteresMe API

ASP.NET Core 8 backend for InteresMe.

InteresMe is currently moving toward a Student MVP: a product for discovering people through shared interests, goals, lightweight projects, and real open intent. The product center is the **Initiative**.

An Initiative is not only a startup or project. It is any open intent a user wants to create, share, and let others apply to join.

Examples:

- I want to walk with someone in Kyiv
- Looking for people to play Minecraft tonight
- Learning German together
- Creating an indie game
- Looking for hackathon teammates
- Looking for a co-founder

The backend is a modular monolith. Keep module boundaries clear, keep controllers thin, and keep business logic in services.

## Current Capabilities

- Email/password registration and login
- JWT access tokens
- Refresh tokens with rotation
- Google ID token login
- GitHub OAuth login
- Logout and account deletion
- Email verification
- Phone verification
- Discovery goal selection
- User profile with avatar upload
- Interests catalog and user interests
- Initiatives
- Initiative roles needed
- Initiative tags/interests
- Join requests
- Join request application form
- Initiative delete/archive lifecycle
- Public initiative sharing by slug

## Product Flow

1. User registers or logs in.
2. User completes onboarding data:
   - discovery goal
   - interests
   - profile
3. User creates an Initiative.
4. Backend generates a stable unique slug from the Initiative title.
5. Public Initiatives can be shared by URL.
6. Anonymous users can view public, non-archived Initiatives by slug.
7. To apply/join, a user must register or log in.
8. Applicant submits a join request/application form.
9. Initiative owner views applications.
10. Initiative owner accepts or rejects applications.

## Tech Stack

- ASP.NET Core 8
- EF Core 8
- PostgreSQL
- JWT Bearer authentication
- Refresh token rotation
- Google OAuth / Google ID token validation
- GitHub OAuth
- Swagger / OpenAPI
- Docker
- Docker Compose

## Project Structure

```text
InteresMe.API/
├── BuildingBlocks/
│   ├── Email/
│   ├── Results/
│   └── Security/
├── Configuration/
├── Data/
│   ├── AppDbContext.cs
│   ├── DatabaseExtensions.cs
│   └── Migrations/
├── Modules/
│   ├── Auth/
│   ├── Discovery/
│   ├── Initiatives/
│   ├── Interests/
│   ├── Profile/
│   └── Verification/
├── Security/
├── smoke_tests/
├── Program.cs
└── Dockerfile
```

`Program.cs` wires controllers, Swagger, CORS, EF Core, JWT authentication, current-user access, email services, OAuth services, and module services. It also applies EF Core migrations on startup through `DatabaseExtensions.ApplyMigrationsAsync()`.

## Module Architecture

Most modules follow a straightforward controller/service/model/DTO shape. The Initiatives module is more structured because it is the current product center and has several responsibilities.

```text
Modules/Initiatives/
├── Controllers/
├── Contracts/
│   ├── Requests/
│   └── Responses/
├── Domain/
│   ├── Entities/
│   └── Enums/
├── Mapping/
├── Services/
│   ├── InitiativeManagement/
│   ├── InitiativeQueries/
│   ├── JoinRequests/
│   └── Slugs/
└── Validators/
```

### Initiatives Responsibilities

- `Controllers`: HTTP endpoints only. Keep them thin.
- `Domain/Entities`: EF Core entities.
- `Domain/Enums`: enum values stored in the database and exposed through contracts.
- `Contracts/Requests`: request DTOs used by controllers.
- `Contracts/Responses`: response DTOs returned by the API.
- `Services/InitiativeManagement`: create, update, delete/archive.
- `Services/InitiativeQueries`: authenticated list/detail queries and public slug query.
- `Services/JoinRequests`: apply, list applications, accept, reject.
- `Services/Slugs`: Initiative slug generation.
- `Mapping`: entity-to-response mapping and public response projection.
- `Validators`: request normalization and validation.

Do not collapse Initiative logic back into one large service. Future areas such as members, search, comments, and notifications should get their own focused services.

## Initiatives Domain

### Entities

- `Initiative`
  - Owner-created open intent.
  - Has title, slug, short description, goal type, status, visibility, optional university, optional team size, interests/tags, roles needed, timestamps.
- `InitiativeInterest`
  - Join entity between an Initiative and an Interest.
- `InitiativeRole`
  - Role needed by an Initiative, for example developer, designer, teammate, participant.
- `InitiativeJoinRequest`
  - Application from a user to join an Initiative.

### Enums

`InitiativeVisibility`

- `Public = 1`
- `Private = 2`

`InitiativeStatus`

- `Idea = 1`
- `Active = 2`
- `Completed = 3`
- `Archived = 4`

`InitiativeGoalType`

- `Connect = 1`
- `Learn = 2`
- `Build = 3`
- `Play = 4`
- `Explore = 5`

`InitiativeJoinRequestStatus`

- `Pending = 1`
- `Accepted = 2`
- `Rejected = 3`

### Join Request Application Fields

Join requests support a lightweight application form:

- `roleId`
- `message`
- `motivation`
- `experience`
- `contribution`
- `availability`

`roleId` is optional. If provided, it must belong to the target Initiative. `message` is limited to 500 characters.

### Slugs

Slugs are generated from Initiative titles on create. They are unique. If the base slug already exists, the backend appends a short unique suffix. Slugs are stable after creation and are not regenerated during update.

### Visibility and Public Sharing

New Initiatives default to `Public` when visibility is not provided.

The public endpoint returns landing-page-safe data only. It does not return join requests, applicant data, owner email, owner phone, tokens, auth data, or private profile data.

Private Initiatives, missing Initiatives, and archived Initiatives return `404` from the public endpoint.

### Delete / Archive Lifecycle

Deleting an Initiative:

- Removes the Initiative if there are no accepted join requests.
- Archives the Initiative if accepted members exist.

Archived Initiatives are not visible through the public slug endpoint.

## API Overview

Swagger is available when the API is running:

- Local profile: `http://localhost:5097/swagger`
- Docker Compose: `http://localhost:8080/swagger`

This section is a map of endpoint groups, not a replacement for Swagger.

### Auth

Base route: `/api/auth`

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/google`
- `GET /api/auth/github`
- `GET /api/auth/github/callback`
- `POST /api/auth/github`
- `POST /api/auth/github/session`
- `DELETE /api/auth/me`

### Verification

Base route: `/api/verification`

- `GET /api/verification/me`
- `POST /api/verification/email/send`
- `POST /api/verification/email/confirm`
- `POST /api/verification/phone/send`
- `POST /api/verification/phone/confirm`

### Discovery / Goal

Base route: `/api/discovery`

- `GET /api/discovery/goals`
- `GET /api/discovery/me`
- `PUT /api/discovery/me/goal`

### Profile

Base route: `/api/profile`

- `GET /api/profile/me`
- `POST /api/profile/me`
- `PUT /api/profile/me`
- `POST /api/profile/me/avatar`
- `PUT /api/profile/me/interests`

Profile create/update supports JSON and multipart form data for avatar upload.

### Interests

Base route: `/api/interests`

- `GET /api/interests`
- `POST /api/interests`

### Initiatives

Base route: `/api/initiatives`

- `GET /api/initiatives`
- `POST /api/initiatives`
- `GET /api/initiatives/{id}`
- `PUT /api/initiatives/{id}`
- `DELETE /api/initiatives/{id}`
- `POST /api/initiatives/{id}/join-requests`
- `GET /api/initiatives/{id}/join-requests`
- `POST /api/initiatives/{id}/join-requests/{requestId}/accept`
- `POST /api/initiatives/{id}/join-requests/{requestId}/reject`

Public route:

- `GET /api/public/initiatives/{slug}`

## Authorization Rules

- Anonymous users can:
  - register
  - log in
  - refresh/logout with refresh token
  - use Google/GitHub auth entry points
  - view discovery goals
  - confirm email verification with a token
  - view public, non-archived Initiatives by slug

- Anonymous users cannot:
  - create Initiatives
  - apply to Initiatives
  - view private Initiatives
  - view archived Initiatives through the public endpoint
  - view join requests or applicant data
  - update profile, goals, interests, or verification data

- Authenticated users can:
  - create Initiatives
  - update their own profile/interests/goal
  - apply to another user's Initiative

- Initiative owners can:
  - update their own Initiatives
  - delete/archive their own Initiatives
  - view join requests for their own Initiatives
  - accept or reject join requests for their own Initiatives

- Initiative owners cannot:
  - submit a join request to their own Initiative

- Non-owners cannot:
  - update/delete another user's Initiative
  - view another Initiative owner's applications
  - accept or reject applications for another user's Initiative

## Local Development

### Prerequisites

- .NET SDK 8
- PostgreSQL 16 or compatible PostgreSQL server
- Docker and Docker Compose, if using the containerized setup
- EF Core CLI tools for migration commands:

```bash
dotnet tool install --global dotnet-ef
```

### Environment Variables

Configuration is loaded from environment variables. `EnvLoader` searches for `.env` in:

- current working directory
- parent directory
- grandparent directory

Start from the root `.env.example`:

```bash
cp .env.example .env
```

Required variables:

```env
POSTGRES_USER=interesme
POSTGRES_PASSWORD=generate-a-strong-password
POSTGRES_DB=interesme
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
AUTH_TOKEN_SECRET=generate-at-least-32-characters
```

Optional variables:

```env
JWT_ISSUER=InteresMe
JWT_AUDIENCE=InteresMe.Client
GOOGLE_CLIENT_ID=
GITHUB_CLIENT_ID=
GITHUB_CLIENT_SECRET=
FRONTEND_OAUTH_CALLBACK_URL=http://localhost:4201/auth/callback
FRONTEND_ORIGINS=http://localhost:4200,http://127.0.0.1:4200,http://localhost:4201,http://127.0.0.1:4201
```

Generate local secrets:

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -base64 48   # AUTH_TOKEN_SECRET
```

Do not commit real secrets. Keep production secrets outside the repository.

### Appsettings

`appsettings.json` contains general ASP.NET logging and host settings. Most operational configuration is environment-based.

`appsettings.Development.json` contains development email settings. Treat any real SMTP/OAuth values as secrets and do not commit production credentials.

### Run PostgreSQL Locally

If PostgreSQL is installed locally, make sure it matches your `.env` values.

Alternatively, use Docker Compose from the repository root:

```bash
docker compose up -d postgres
```

### Apply Migrations

The API applies migrations automatically on startup.

Manual update:

```bash
dotnet ef database update --project InteresMe.API --startup-project InteresMe.API
```

### Run the API Locally

From the repository root:

```bash
dotnet run --project InteresMe.API
```

Common local URLs:

- API: `http://localhost:5097`
- Swagger: `http://localhost:5097/swagger`

### Build

```bash
dotnet build
```

## Docker

The backend has a multi-stage Dockerfile:

- runtime image: `mcr.microsoft.com/dotnet/aspnet:8.0`
- build image: `mcr.microsoft.com/dotnet/sdk:8.0`
- restore is cached by copying `InteresMe.API.csproj` first
- published output is copied into the final runtime image

Build backend image from repository root:

```bash
docker compose build backend
```

Run backend and PostgreSQL:

```bash
docker compose up -d postgres backend
```

Run full stack:

```bash
docker compose up -d
```

Docker API URLs:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

Docker Compose reads `.env` from the repository root. Required production-like variables are the same as local development. Never commit real secrets.

## Database and EF Core

`AppDbContext` is located at:

```text
InteresMe.API/Data/AppDbContext.cs
```

It maps schemas for:

- `auth`
- `profile`
- `interests`
- `discovery`
- `initiatives`
- `verification`

Migrations are located at:

```text
InteresMe.API/Data/Migrations/
```

Create a migration:

```bash
dotnet ef migrations add MigrationName --project InteresMe.API --startup-project InteresMe.API
```

Apply migrations:

```bash
dotnet ef database update --project InteresMe.API --startup-project InteresMe.API
```

Check pending model changes:

```bash
dotnet ef migrations has-pending-model-changes --project InteresMe.API --startup-project InteresMe.API
```

Rules:

- Use EF migrations for schema changes.
- Do not hand-edit schema through SQL unless the migration requires explicit data backfill.
- Keep entity mappings in `AppDbContext`.
- Confirm no unintended pending model changes after refactors.

## Smoke Tests

Smoke tests are in:

```text
InteresMe.API/smoke_tests/
```

Current smoke test:

```text
public_initiatives_smoke.py
```

It uses only the Python standard library and verifies:

- anonymous user can view a public Initiative by slug
- anonymous user cannot view private Initiative
- anonymous user cannot view archived Initiative
- public response does not contain `joinRequests`
- public response does not contain sensitive owner/auth data
- created Initiative receives a slug
- duplicate titles produce unique slugs

Run it against the Docker Compose API:

```bash
python3 InteresMe.API/smoke_tests/public_initiatives_smoke.py
```

Override API base URL:

```bash
API_BASE_URL=http://localhost:8080 python3 InteresMe.API/smoke_tests/public_initiatives_smoke.py
```

The API must already be running.

## Development Conventions

Follow the repository-level `AGENTS.md` backend rules:

- Do not modify the frontend unless explicitly requested.
- Keep business logic in services.
- Keep controllers thin.
- Validate data before it reaches the database.
- Use interfaces for services.
- Reuse existing DTOs and services.
- Do not invent endpoints without a real need.
- Preserve API compatibility where possible.
- Swagger is the primary API documentation and manual testing surface.

Initiatives-specific conventions:

- Initiatives are currently the product center.
- Do not put all Initiative logic back into one service.
- Keep Initiative management, queries, join requests, slugs, mapping, and validation separated.
- Do not add matching, feed, AI recommendations, recruiter mode, or chat until the product explicitly needs them.
- Public Initiative responses must stay landing-page-safe.
- Keep slug behavior stable unless a deliberate slug policy is approved.

## Roadmap / Next Backend Steps

Short-term likely backend work:

- `InitiativeMember` domain model after accepted applications
- "My Initiatives" endpoints
- Initiative search/filtering
- Initiative comments or lightweight discussion
- Notifications for applications and decisions

Later:

- Talent signals
- Recruiter mode
- Recommendations/matching

## What This Project Is Not Yet

- Not a Tinder clone
- Not a LinkedIn clone
- Not a Facebook-style feed
- Not a recommendation engine yet
- Not a recruiter marketplace yet

The current direction is simpler: help students and early users express open intent, share it, and let the right people apply to join.

# InteresMe

InteresMe is a social discovery platform focused on meaningful connections through interests, goals, projects, and lifestyle.

This repository is a **monorepo** with a single ASP.NET Core backend and an Angular frontend.

---

## Tech Stack

### Backend (`InteresMe.API`)
- ASP.NET Core 8
- Modular folder structure (one Web API project)
- PostgreSQL + Entity Framework Core
- JWT authentication (Swagger **Authorize** button)
- BCrypt password hashing
- Swagger / OpenAPI
- Docker

### Frontend (`InteresMe.Client`)
- Angular
- TypeScript
- Tailwind CSS
- Nginx (Docker)

---

## Project Structure

```txt
interesme/
├── InteresMe.API/              # Backend (single project)
│   ├── Modules/
│   │   ├── Auth/               # Register, Login, JWT
│   │   ├── Users/
│   │   ├── Feed/
│   │   └── Chat/
│   ├── Data/                   # DbContext, migrations
│   ├── Security/               # JWT, BCrypt
│   ├── Configuration/          # Env, Swagger
│   └── Dockerfile
├── InteresMe.Client/
├── docker-compose.yml
├── .env.example                # Copy to .env (not committed)
└── InteresMe.sln
```

Each module contains: `Controllers/`, `Services/`, `DTOs/`, `Models/`.

---

## Getting Started

### 1. Environment variables

```bash
cp .env.example .env
```

Generate secrets and paste them into `.env`:

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -base64 48   # AUTH_TOKEN_SECRET
```

| Variable | Description |
|----------|-------------|
| `POSTGRES_USER` | Database user |
| `POSTGRES_PASSWORD` | Database password (min 16 chars) |
| `POSTGRES_DB` | Database name |
| `POSTGRES_HOST` | `localhost` for local run, `postgres` in Docker |
| `POSTGRES_PORT` | Default `5432` |
| `AUTH_TOKEN_SECRET` | JWT signing key (min 32 chars) |

Never commit `.env` — it is listed in `.gitignore`.

### 2. Run with Docker (recommended)

```bash
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:5050 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

Migrations are applied automatically when the API starts.

### 3. Run locally (without Docker)

Start PostgreSQL, then:

```bash
dotnet run --project InteresMe.API
```

| | URL |
|---|-----|
| API | http://localhost:5097 |
| Swagger | http://localhost:5097/swagger |

---

## API Overview

### Auth (PostgreSQL, public)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Create account |
| POST | `/api/auth/login` | Login, returns JWT |

### Protected modules (require JWT)

| Module | Base route |
|--------|------------|
| Users | `/api/users` |
| Feed | `/api/feed` |
| Chat | `/api/chat` |

### Swagger + JWT

1. Call `POST /api/auth/login`
2. Copy the `token` from the response
3. Click **Authorize** in Swagger
4. Paste the token (without `Bearer`)
5. Call protected endpoints

---

## Development

```bash
# Restore & build
dotnet restore InteresMe.sln
dotnet build InteresMe.sln

# EF migrations (from repo root)
dotnet ef migrations add MigrationName \
  --project InteresMe.API/InteresMe.API.csproj \
  --output-dir Data/Migrations
```

---

## Current Status

- Auth with PostgreSQL, BCrypt, JWT
- Users, Feed, Chat modules (in-memory data for Feed/Chat/Users)
- Docker Compose with Postgres
- CI: build backend + frontend on `main`

Planned: full persistence for all modules, interest-based discovery, communities, matching.

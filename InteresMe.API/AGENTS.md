# InteresMe Backend

## Project

InteresMe is a platform around:

- Interests
- Initiatives
- Communities
- User Journeys

People connect through activity.

Not through appearance.

Do not turn InteresMe into:

- Tinder
- LinkedIn
- Instagram
- TikTok

## Core Principles

- Initiatives are primary.
- Profiles are secondary.
- Interests are a structured map.
- History is a user journey.
- Follow means following activity, not appearance.

## Stack

- ASP.NET Core 8
- PostgreSQL
- EF Core
- Modular Monolith

## Development Rules

Before implementing:

1. Inspect existing code.
2. Follow existing module structure.
3. Reuse existing patterns.
4. Extend existing functionality before creating new abstractions.

## EF Core

Use:

- AsNoTracking()
- projections
- bounded queries

Avoid:

- giant Include chains
- N+1 queries
- loading unnecessary collections

## API

- Return DTOs only.
- Never expose EF entities.
- Use existing ApiErrorResponse.
- Use existing domain error patterns.

## DTO File Rule

Use one public DTO class per file.

Group related DTOs by folder, not by putting many public classes into one file.

## Migrations

When schema changes:

1. Create migration.
2. Inspect migration.
3. Ensure migration contains only intended changes.
4. Apply migration.

## Profile Pages

Prefer aggregate endpoints.

Good:

GET /api/users/{id}/profile-view

Bad:

GET profile
GET interests
GET history
GET initiatives
GET followers

## Avoid

Do not introduce unless explicitly requested:

- Microservices
- CQRS
- Event Sourcing
- Kafka
- RabbitMQ
- Generic repositories

Prefer simple solutions.

## Required Final Report

Always respond with:

## План
## Що зроблено
## Змінені файли
## Міграція
## Endpoint-и
## Перевірка
## Наступні кроки

Final report must be in Ukrainian.
# InteresMe

InteresMe is a modern social discovery platform focused on meaningful connections through interests, goals, projects, and lifestyle.

Unlike traditional social media platforms that prioritize endless scrolling and superficial interaction, InteresMe is designed to help people discover like-minded individuals through shared passions, personal growth, creativity, and real-world activities.

---

## Tech Stack

### Backend
- ASP.NET Core 8
- REST API
- Swagger
- Docker

### Frontend
- Angular
- TypeScript
- Tailwind CSS
- Nginx
- Docker

---

## Project Structure

```txt
interesme/
│
├── InteresMe.API/
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   └── Dockerfile
│
├── InteresMe.Client/
│   ├── src/
│   └── Dockerfile
│
├── docker-compose.yml
└── InteresMe.sln
Running with Docker

Build and start the entire project:

docker compose up --build
Local URLs

Frontend:

http://localhost:5050

Backend:

http://localhost:8080

Swagger:

http://localhost:8080/swagger
Planned Features
Authentication & Authorization
User profiles
Interest-based discovery
Activity feed
Messaging system
Communities
Smart matching
Responsive UI
Current Status

Project is currently in active development.

This repository contains both frontend and backend applications in a monorepo architecture.
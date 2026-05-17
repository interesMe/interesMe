# InteresMe API

Backend API for the InteresMe platform.

InteresMe is a social discovery platform focused on meaningful connections through interests, goals, projects, and lifestyle.

---

## Tech Stack

- ASP.NET Core 8
- REST API
- Docker
- Swagger

---

## Features

- REST API
- Swagger documentation
- Docker support
- Authentication system (planned)
- PostgreSQL integration (planned)

---

## Development

Run locally:

```bash
dotnet run

API URL:

http://localhost:5097

Swagger:

http://localhost:5097/swagger
Docker

Build image:

docker build -t interesme-api .

Run container:

docker run -p 8080:8080 interesme-api

Docker API URL:

http://localhost:8080

Docker Swagger:

http://localhost:8080/swagger
Project Structure
InteresMe.API/
│
├── Controllers/
├── Models/
├── Services/
├── DTOs/
├── Data/
└── Program.cs
Status

Project is currently in active development.
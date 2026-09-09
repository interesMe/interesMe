# InteresMe

InteresMe — вебплатформа соціального пошуку (social discovery), яка допомагає людям знаходити релевантних людей, спільноти та ініціативи на основі спільних інтересів, цілей, навичок і активностей.

## Навчальний проєкт

Цей репозиторій використовується як репозиторій проєкту для лабораторної роботи №1 "Ініціалізація командного проєкту". InteresMe — це наявний реальний програмний проєкт, який продовжує розвиватись; для лабораторної роботи він доповнений навчальною документацією, а не створений виключно для навчальних цілей.

## Склад команди

| Учасник | Роль | GitHub username |
| ------- | ---- | ---------------- |
| Максим | Project Manager (Team Lead), Product Owner, Developer, Designer, Analyst | `kqwakrss` |

Проєкт виконується одноосібно — Максим виконує всі ролі, передбачені завданням.

## Артефакти навчального проєкту

- [Лабораторна робота №1 — Проєктний документ](./project-brief.md)
- [Лабораторна робота №2 — Класифікація проєкту та аналіз оточення](./project-classification.md)

---

## Технологічний стек

### Бекенд (`InteresMe.API`)
- ASP.NET Core 8
- Модульна структура (єдиний Web API проєкт, модулі за доменами)
- PostgreSQL + Entity Framework Core
- JWT автентифікація (кнопка **Authorize** у Swagger), OAuth через Google та GitHub
- Хешування паролів через BCrypt
- Swagger / OpenAPI
- Docker

### Фронтенд (`interesme-web`)
- Angular
- TypeScript
- Tailwind CSS
- Nginx (у Docker-збірці)

---

## Структура репозиторію

```txt
interesme/
├── InteresMe.API/              # Бекенд (єдиний проєкт)
│   ├── Modules/
│   │   ├── Auth/                # Реєстрація, вхід, JWT, OAuth
│   │   ├── Profile/              # Профіль користувача
│   │   ├── Posts/                 # Стрічка, коментарі, лайки, поширення
│   │   ├── Chat/                  # Особисті повідомлення, групи, канали
│   │   ├── Discovery/             # Пошук релевантних людей і контенту
│   │   ├── Interests/             # Ієрархічний каталог інтересів
│   │   ├── Initiatives/           # Ініціативи/проєкти для пошуку учасників
│   │   ├── History/                # Історія активності користувача
│   │   └── Verification/           # Верифікація
│   ├── Data/                     # DbContext, міграції
│   ├── Security/                 # JWT, BCrypt
│   ├── Configuration/             # Env, Swagger
│   └── Dockerfile
├── interesme-web/                # Фронтенд (Angular)
├── docker-compose.yml
├── .env.example                  # Копіювати в .env (не комітиться)
├── project-brief.md              # Проєктний документ (лабораторна №1)
├── project-classification.md     # Класифікація та аналіз оточення (лабораторна №2)
└── InteresMe.sln
```

Кожен бекенд-модуль містить: `Controllers/`, `Services/`, `DTOs/`, `Models/`.

---

## Змінні середовища

```bash
cp .env.example .env
```

Згенеруйте секрети та вставте їх у `.env`:

```bash
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -base64 48   # AUTH_TOKEN_SECRET
```

| Змінна | Опис |
|--------|------|
| `POSTGRES_USER` | Користувач бази даних |
| `POSTGRES_PASSWORD` | Пароль бази даних (мін. 16 символів) |
| `POSTGRES_DB` | Назва бази даних |
| `POSTGRES_HOST` | `localhost` для локального запуску, `postgres` у Docker |
| `POSTGRES_PORT` | За замовчуванням `5432` |
| `AUTH_TOKEN_SECRET` | Ключ підпису JWT (мін. 32 символи) |
| `GOOGLE_CLIENT_ID` | Google OAuth Client ID для фронтенду |
| `GITHUB_CLIENT_ID` | GitHub OAuth App Client ID |
| `GITHUB_CLIENT_SECRET` | GitHub OAuth App Client Secret |
| `FRONTEND_OAUTH_CALLBACK_URL` | Маршрут фронтенду після OAuth-колбеку бекенду |
| `FRONTEND_PORT` | Порт фронтенд dev-сервера в Docker Compose |

Ніколи не комітьте `.env` — він внесений у `.gitignore`.

---

## Запуск через Docker (рекомендовано)

```bash
docker compose up --build
```

| Сервіс | URL |
|--------|-----|
| Frontend | http://localhost:4201 |
| API | http://localhost:8080 |
| Swagger | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |

Міграції застосовуються автоматично під час старту API.

## Локальний запуск (без Docker)

Запустіть PostgreSQL, потім:

```bash
dotnet run --project InteresMe.API
```

| | URL |
|---|-----|
| API | http://localhost:5097 |
| Swagger | http://localhost:5097/swagger |

Для фронтенду — у каталозі `interesme-web`:

```bash
npm install
npm start
```

---

## Огляд API

### Auth (публічні маршрути)

| Метод | Маршрут | Опис |
|-------|---------|------|
| POST | `/api/auth/register` | Створення акаунта |
| POST | `/api/auth/login` | Вхід, повертає JWT |

### Захищені модулі (потребують JWT)

| Модуль | Базовий маршрут |
|--------|-----------------|
| Profile | `/api/profile`, `/api/users` |
| Posts | `/api/posts` |
| Chat | `/api/chat`, особисті повідомлення / групи / канали |
| Discovery | `/api/discovery` |
| Interests | `/api/interests` |
| Initiatives | `/api/initiatives` |
| History | `/api/history` |

### Swagger + JWT

1. Викличте `POST /api/auth/login`
2. Скопіюйте `token` з відповіді
3. Натисніть **Authorize** у Swagger
4. Вставте токен (без `Bearer`)
5. Викликайте захищені ендпоінти

---

## Команди розробки

```bash
# Відновлення та збірка бекенду
dotnet restore InteresMe.sln
dotnet build InteresMe.sln

# EF-міграції (з кореня репозиторію)
dotnet ef migrations add MigrationName \
  --project InteresMe.API/InteresMe.API.csproj \
  --output-dir Data/Migrations

# Фронтенд
cd interesme-web
npm install
npm start
```

---

## Поточний стан

- Автентифікація через PostgreSQL, BCrypt, JWT, OAuth (Google, GitHub)
- Реалізовані модулі: Auth, Profile, Posts (стрічка, коментарі, лайки, поширення, зображення), Chat (особисті повідомлення, групи, канали, вкладення), Discovery, Interests (каталог), Initiatives, History, Verification
- Docker Compose з PostgreSQL
- CI: збірка бекенду та фронтенду на `main`

## Запланована функціональність

- Розширений алгоритм рекомендацій та підбору за інтересами
- Розвиток спільнот і групових просторів за інтересами
- Подальше покриття тестами та розширення персистентності даних

# 🌍 Vivu — AI Travel Planning Platform

<div align="center">

### Intelligent Travel Planning & Community Platform

AI-powered itinerary generation, trip collaboration, blogging, realtime notifications and subscription system.

<br/>

![.NET](https://img.shields.io/badge/.NET-8-512BD4?style=for-the-badge\&logo=dotnet)
![React](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge\&logo=react\&logoColor=black)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-4169E1?style=for-the-badge\&logo=postgresql)
![Redis](https://img.shields.io/badge/Redis-Cache-DC382D?style=for-the-badge\&logo=redis)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?style=for-the-badge\&logo=docker)

</div>

---

# 📖 Overview

**Vivu** là nền tảng du lịch thông minh hỗ trợ người dùng từ giai đoạn lên ý tưởng, tạo lịch trình, cộng tác cùng bạn đồng hành, đến chia sẻ trải nghiệm qua blog cộng đồng.

Hệ thống kết hợp:

* ⚡ ASP.NET Core Web API
* 🎨 React + Vite
* 🤖 AI itinerary assistant
* 🔔 Realtime notifications
* 💳 Subscription & payment
* 📊 CMS & analytics

README này giúp bạn:

* Hiểu kiến trúc monorepo
* Chạy hệ thống local
* Cấu hình môi trường
* Hiểu workflow phát triển
* Mở rộng hệ thống dễ dàng

---

# 🏗 Architecture

Repository được tổ chức theo hướng **Clean Architecture** + **CQRS**.

```text
Client
   ↓
Vivu.WebApi
   ↓
Vivu.Application
   ↓
Vivu.Domain
   ↓
Vivu.Infrastructure
   ↓
Database / Redis / External Services
```

---

# 📂 Monorepo Structure

```text
Vivu/
├── src/
│   ├── Vivu.WebApi/
│   ├── Vivu.Application/
│   ├── Vivu.Domain/
│   └── Vivu.Infrastructure/
│
├── resources/
│   └── VivuAppFE/
│
├── tests/
│   ├── Vivu.Application.UnitTests/
│   └── Vivu.IntegrationTests/
│
├── Dockerfile
├── docker-compose.yml
└── Vivu.sln
```

---

# ⚙️ Backend Architecture

## 🔹 Vivu.WebApi

Entry point của hệ thống:

* Controllers
* Middleware
* Swagger
* SignalR Hubs
* Authentication
* Background jobs wiring

## 🔹 Vivu.Application

Business logic layer:

* CQRS + MediatR
* Commands / Queries
* Validators
* Use cases
* DTOs

## 🔹 Vivu.Domain

Core domain layer:

* Entities
* Enums
* Value Objects
* Domain contracts

## 🔹 Vivu.Infrastructure

Infrastructure layer:

* EF Core
* Repositories
* Redis
* Email services
* Cloud integrations
* Payment integrations

---

# 🎨 Frontend Architecture

Frontend nằm tại:

```text
resources/VivuAppFE
```

## Frontend stack

* React 19
* Vite
* React Router
* Tailwind CSS
* Radix UI
* Axios
* SWR
* SignalR Client
* Vitest

## Frontend structure

```text
src/
├── api/
├── components/
├── routes/
├── hooks/
├── pages/
├── layouts/
└── test/
```

---

# ✨ Main Features

# 🔐 Authentication

* Register / Login
* Refresh token
* Forgot password
* Email verification
* Google login

# 🧳 Trip Management

* Create/Edit/Delete trip
* Manage trip days
* Add locations into itinerary
* Reorder locations
* Invite members
* Role management
* Join by invitation code

# 🤖 AI Travel Assistant

* Generate itinerary from prompt
* Modify itinerary using AI
* AI chat assistant
* Intent detection

# 📝 Community Blog

* Create & publish blogs
* Comments
* Likes
* Bookmark
* Report system

# 📊 CMS & Moderation

* User management
* Location management
* Categories & cities
* Report moderation
* Statistics dashboard

# 🔔 Realtime Notifications

Powered by SignalR:

* Trip updates
* Notifications
* Collaboration events

# 💳 Subscription & Payments

* Subscription packages
* Payment processing
* Transaction tracking

---

# 🌐 Main API Domains

```text
AuthController
UsersController
TripsController
TripDayController
TripLocationController
TripMembersController
LocationsController
BlogsController
AIController
NotificationsController
PaymentController
StatisticsController
```

---

# 🛠 Tech Stack

## Backend

* .NET 8
* ASP.NET Core Web API
* Entity Framework Core
* MediatR
* JWT Authentication
* SignalR
* Serilog
* Hangfire
* Redis
* PostgreSQL

## Frontend

* React 19
* Vite
* Tailwind CSS
* Radix UI
* Axios
* SWR
* Vitest

---

# 🚀 Getting Started

# 1️⃣ Requirements

* .NET SDK 8+
* Node.js 20+
* npm 10+
* PostgreSQL
* Redis 7+
* Docker (optional)

---

# 2️⃣ Environment Variables

Ứng dụng sử dụng:

* `appsettings.json`
* `.env`
* Environment variables

Ví dụ:

```env
JWT_SECRET=
JWT_ISSUER=
JWT_AUDIENCE=

ConnectionStrings__DefaultConnection=

Redis__ConnectionString=

CORS_ALLOWED_ORIGINS=
```

⚠️ Không commit secrets thật lên repository.

---

# 🐳 Run With Docker

```bash
docker compose up --build
```

Default ports:

| Service         | Port |
| --------------- | ---- |
| API             | 5000 |
| Redis           | 6379 |
| Redis Commander | 8081 |

---

# 💻 Run Backend Locally

```bash
dotnet restore
dotnet build Vivu.sln
dotnet run --project src/Vivu.WebApi
```

Default development URLs:

```text
http://localhost:5012
https://localhost:7294
```

Swagger:

```text
/swagger
```

---

# 🎨 Run Frontend Locally

```bash
cd resources/VivuAppFE

npm install
npm run dev
```

Useful scripts:

```bash
npm run build
npm run lint
npm run test:run
```

---

# 🧪 Testing

## Backend Unit Tests

```bash
dotnet test tests/Vivu.Application.UnitTests/Vivu.Application.UnitTests.csproj
```

## Backend Integration Tests

```bash
dotnet test tests/Vivu.IntegrationTests/Vivu.IntegrationTests.csproj
```

## Frontend Tests

```bash
cd resources/VivuAppFE
npm run test:run
```

---

# 🔄 Development Workflow

```text
1. Pull latest source
2. Create feature branch
3. Run Redis & Database
4. Run backend
5. Run frontend
6. Develop feature
7. Write tests
8. Create Pull Request
```

Example:

```bash
git checkout -b feature/trip-management
```

---

# 🤝 Contributing

Before creating PR:

* Ensure tests pass
* Ensure lint passes
* Avoid committing secrets
* Follow architecture boundaries
* Add tests for business logic changes

```bash
dotnet test
npm run test:run
```

---

# 📌 Future Documentation

Có thể mở rộng thêm:

* `docs/backend.md`
* `docs/frontend.md`
* `docs/deployment.md`
* `.env.example`
* `docs/api.md`

---

<div align="center">

## 👨‍💻 Vivu Team - Capstone Project SP26 - FPT University Da Nang

Build smarter trips.
Travel better with AI.

</div>

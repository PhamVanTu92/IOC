# IOC - Intelligent Operation Center

## Tổng quan
Hệ thống **Trung tâm Điều hành Thông minh (IOC)** — nền tảng plugin-based cho phép tích hợp
các module nghiệp vụ (Finance, HR, Marketing, ...) vào một dashboard điều hành thống nhất.

## Kiến trúc hệ thống

```
┌─────────────────────────────────────────────────────────┐
│                   Frontend (React + Vite)                │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │ Finance  │  │   HR     │  │  Marketing Plugin    │  │
│  │  Plugin  │  │  Plugin  │  └──────────────────────┘  │
│  └──────────┘  └──────────┘                             │
│  ┌─────────────────────────────────────────────────┐    │
│  │   DragDrop Builder  │  ECharts Widgets          │    │
│  └─────────────────────────────────────────────────┘    │
└───────────────────────┬─────────────────────────────────┘
                        │ GraphQL (WebSocket/HTTP)
┌───────────────────────▼─────────────────────────────────┐
│              Backend (.NET 8 / HotChocolate)             │
│  ┌──────────────────┐   ┌────────────────────────────┐  │
│  │   IOC.Core       │   │   IOC.SemanticLayer        │  │
│  │  Plugin Host     │   │   Metric Definitions       │  │
│  └──────────────────┘   └────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐   │
│  │  Plugins: Finance | HR | Marketing | ...         │   │
│  └──────────────────────────────────────────────────┘   │
└───────────────────────┬─────────────────────────────────┘
                        │ Kafka Events / SignalR Push
┌───────────────────────▼─────────────────────────────────┐
│              Realtime Layer                              │
│         Kafka (Events)  +  SignalR (Push to Browser)    │
└─────────────────────────────────────────────────────────┘
```

## Stack công nghệ

| Layer     | Technology                                        |
|-----------|---------------------------------------------------|
| Frontend  | React 18, Vite, TypeScript, ECharts, dnd-kit      |
| Backend   | .NET 8, HotChocolate (GraphQL), EF Core           |
| Semantic  | Custom Semantic Layer (Metric / Dimension)        |
| Realtime  | Apache Kafka, ASP.NET Core SignalR                |
| Database  | SQL Server / PostgreSQL                           |
| Cache     | Redis                                             |
| Container | Docker, Docker Compose                            |

## Cấu trúc thư mục

```
IOC/
├── src/
│   ├── frontend/          # React app (Vite + TypeScript)
│   ├── backend/           # .NET 8 solution
│   └── realtime/          # Kafka producers/consumers + SignalR hubs
├── tests/
│   ├── frontend/          # Jest + React Testing Library + Playwright
│   └── backend/           # xUnit + WebApplicationFactory + Testcontainers
├── docs/architecture/
└── docker/
```

## Plugin System

### Backend — thêm plugin mới
1. Tạo project: `src/backend/Plugins/IOC.{TenPlugin}/`
2. Implement `IPlugin` interface từ `IOC.Core`
3. Đăng ký vào `IOC.sln` và `IOC.Api/Program.cs`
4. Viết unit test tại `tests/backend/unit/`

### Frontend — thêm plugin mới
1. Tạo thư mục: `src/frontend/src/plugins/{ten-plugin}/`
2. Export object implement `IOCPlugin` interface
3. Đăng ký vào `src/frontend/src/core/PluginRegistry.ts`
4. Viết unit test tại `tests/frontend/unit/`

## Custom Commands

| Command                | Mục đích                              |
|------------------------|---------------------------------------|
| `/project:review`      | Review code trước khi merge           |
| `/project:fix-issue`   | Phân tích và fix issue cụ thể         |
| `/project:deploy`      | Deploy lên môi trường chỉ định        |
| `/project:add-plugin`  | Scaffold một plugin module mới        |
| `/project:gen-module`  | Tạo CRUD module từ schema định nghĩa  |

## Conventions

- **Backend**: C# — PascalCase cho class/method, `_camelCase` cho private fields
- **Frontend**: TypeScript strict mode, functional components, named exports
- **GraphQL**: Query = read-only, Mutation = write, Subscription = realtime stream
- **Kafka topics**: `ioc.{domain}.{event}` (vd: `ioc.finance.budget-updated`)
- **Tests**: Mỗi feature PHẢI có unit test; API endpoint PHẢI có integration test

## Getting Started

```bash
# Khởi động infrastructure (Kafka, SQL Server, Redis)
docker compose -f docker/docker-compose.dev.yml up -d

# Backend
cd src/backend && dotnet build IOC.sln && dotnet run --project IOC.Api

# Frontend
cd src/frontend && npm install && npm run dev
```

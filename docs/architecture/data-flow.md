# Data Flow Architecture

## Request Flow (Read)

```
Browser
  │  GraphQL Query (HTTP POST /graphql)
  ▼
IOC.Api (HotChocolate)
  │  Resolver gọi Service
  ▼
Plugin Service (vd: BudgetService)
  │  Repository pattern
  ▼
SQL Server / PostgreSQL
  │  Kết quả
  ▼
SemanticLayer (aggregate nếu cần)
  │  Formatted data
  ▼
GraphQL Response (JSON)
  │
  ▼
Browser (Apollo Client cache)
  │
  ▼
React Component re-render
```

## Mutation Flow (Write)

```
Browser
  │  GraphQL Mutation
  ▼
IOC.Api Mutation Resolver
  │  Validate input
  ▼
Plugin Service
  │  Business logic
  ▼
Database (EF Core)
  │  Transaction commit
  ▼
Kafka Producer (publish event)
  │  Topic: ioc.{domain}.{event}
  ▼
[Async] Kafka Consumer
  │  Process side effects
  ▼
SignalR DashboardHub
  │  Broadcast to subscribers
  ▼
Browser (connected via WebSocket)
  │  Apollo Subscription update
  ▼
React Component realtime update
```

## Realtime Flow

```
Kafka Topic: ioc.finance.budget-updated
  │
  ▼
KafkaConsumerService<BudgetUpdatedEvent>
  │  Handler processes event
  ▼
DashboardNotifier.NotifyMetricUpdatedAsync()
  │
  ▼
DashboardHub → Group("domain-finance")
  │  SendAsync("ReceiveMetricUpdate", payload)
  ▼
Browser SignalR Connection
  │  connection.on("ReceiveMetricUpdate", ...)
  ▼
React State update → ECharts re-render
```

## Event Schema (CloudEvents 1.0)

```json
{
  "specversion": "1.0",
  "type": "ioc.finance.budget-updated",
  "source": "/ioc/finance",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "time": "2026-04-22T10:30:00Z",
  "datacontenttype": "application/json",
  "data": {
    "budgetId": 123,
    "departmentId": 5,
    "previousAmount": 500000000,
    "newAmount": 600000000,
    "updatedBy": "user@company.com"
  }
}
```

## Kafka Topics Map

| Topic                          | Producer      | Consumer(s)          |
|-------------------------------|---------------|----------------------|
| ioc.finance.budget-updated    | Finance API   | Dashboard Notifier   |
| ioc.finance.invoice-created   | Finance API   | Accounting, Reports  |
| ioc.hr.employee-joined        | HR API        | IT Provisioning      |
| ioc.hr.payroll-processed      | HR API        | Finance, Accounting  |
| ioc.marketing.campaign-launched | Marketing API | Analytics, Reports |
| ioc.marketing.lead-converted  | Marketing API | CRM, Finance         |
| ioc.system.errors             | Any service   | Alert System         |

# API Conventions — IOC Project

## GraphQL Schema Conventions

### Naming
- **Query fields**: camelCase (vd: `budgetList`, `employeeById`)
- **Mutation fields**: `verb` + `Noun` (vd: `createBudget`, `updateEmployee`)
- **Subscription fields**: `on` + `Event` (vd: `onBudgetUpdated`)
- **Types**: PascalCase (vd: `BudgetType`, `EmployeeType`)
- **Input types**: suffix `Input` (vd: `CreateBudgetInput`)
- **Payload types**: suffix `Payload` (vd: `CreateBudgetPayload`)

### Pattern chuẩn

```graphql
# Query
type Query {
  budgets(filter: BudgetFilterInput, page: PageInput): BudgetConnection!
  budgetById(id: ID!): Budget
}

# Mutation với payload
type Mutation {
  createBudget(input: CreateBudgetInput!): CreateBudgetPayload!
}

type CreateBudgetPayload {
  budget: Budget
  errors: [UserError!]!
}

# Subscription
type Subscription {
  onBudgetUpdated(domain: String!): BudgetUpdatedEvent!
}
```

### Pagination
- Dùng **Relay-style cursor pagination**: `Connection` / `Edge` / `Node`
- Không dùng offset pagination cho danh sách lớn

### Error Handling
- Business errors: trả về trong `errors: [UserError!]!` trong payload
- System errors: throw exception → middleware xử lý

---

## Kafka Topic Conventions

### Naming pattern
```
ioc.{domain}.{event-verb}
```

| Domain     | Topic                          | Mô tả                    |
|------------|-------------------------------|--------------------------|
| finance    | `ioc.finance.budget-updated`  | Budget thay đổi          |
| finance    | `ioc.finance.invoice-created` | Hoá đơn mới              |
| hr         | `ioc.hr.employee-joined`      | Nhân viên mới            |
| hr         | `ioc.hr.leave-approved`       | Nghỉ phép được duyệt     |
| marketing  | `ioc.marketing.campaign-launched` | Campaign ra mắt      |
| system     | `ioc.system.errors`           | Lỗi hệ thống             |

### Message format (CloudEvents)
```json
{
  "specversion": "1.0",
  "type": "ioc.finance.budget-updated",
  "source": "/ioc/finance",
  "id": "uuid-v4",
  "time": "2026-04-22T10:00:00Z",
  "datacontenttype": "application/json",
  "data": { }
}
```

---

## SignalR Hub Conventions

- Hub URL: `/hubs/{domain}` (vd: `/hubs/dashboard`, `/hubs/alerts`)
- Group naming: `{userId}` hoặc `{domain}-{tenantId}`
- Method names: PascalCase (vd: `ReceiveMetricUpdate`, `ReceiveAlert`)

```csharp
// Client subscribe
await connection.InvokeAsync("SubscribeToDomain", "finance");

// Server push
await Clients.Group("finance").SendAsync("ReceiveMetricUpdate", payload);
```

---

## REST Health Endpoints (bắt buộc)

| Endpoint          | Mô tả                        |
|-------------------|------------------------------|
| `GET /health`     | Liveness check               |
| `GET /health/ready` | Readiness check (DB, Kafka) |
| `GET /metrics`    | Prometheus metrics           |

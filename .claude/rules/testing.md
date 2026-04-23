# Testing Rules — IOC Project

## Nguyên tắc chung
- **Test pyramid**: Unit (70%) > Integration (20%) > E2E (10%)
- Mỗi bug fix PHẢI có regression test
- Test phải chạy được độc lập (không phụ thuộc vào thứ tự)
- Coverage minimum: **80% cho business logic**

---

## Backend Tests (xUnit)

### Unit Tests (`tests/backend/unit/`)
- Đặt trong project `IOC.{Module}.Tests`
- Naming: `{MethodName}_When{Condition}_Should{Result}`
- Dùng **AAA pattern**: Arrange / Act / Assert
- Mock dependencies với `Moq` hoặc `NSubstitute`

```csharp
[Fact]
public async Task GetBudget_WhenIdExists_ShouldReturnBudget()
{
    // Arrange
    var mockRepo = new Mock<IBudgetRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(new Budget { Id = 1 });
    var service = new BudgetService(mockRepo.Object);

    // Act
    var result = await service.GetBudgetAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
}
```

### Integration Tests (`tests/backend/integration/`)
- Dùng `WebApplicationFactory<Program>`
- Dùng `Testcontainers` cho SQL Server, Redis, Kafka
- Test GraphQL queries/mutations qua HTTP

```csharp
[Fact]
public async Task GraphQL_QueryBudgets_ReturnsData()
{
    var client = _factory.CreateClient();
    var query = new { query = "{ budgets { id amount } }" };
    var response = await client.PostAsJsonAsync("/graphql", query);
    response.EnsureSuccessStatusCode();
}
```

---

## Frontend Tests

### Unit Tests (`tests/frontend/unit/`) — Jest + RTL
- Naming: `{ComponentName}.test.tsx` hoặc `{hookName}.test.ts`
- Dùng `@testing-library/react` cho component tests
- Dùng `renderHook` cho custom hook tests
- Mock GraphQL với `MockedProvider` (Apollo)

```tsx
it('renders KPI card with correct value', () => {
  render(<KPICard title="Revenue" value={1000000} />);
  expect(screen.getByText('1,000,000')).toBeInTheDocument();
});
```

### E2E Tests (`tests/frontend/e2e/`) — Playwright
- Chạy với app thật (dev server)
- Chỉ test critical user flows
- Naming: `{feature}.spec.ts`

```typescript
test('user can drag widget to dashboard', async ({ page }) => {
  await page.goto('/dashboard');
  await page.dragAndDrop('[data-widget="kpi-card"]', '[data-drop-zone="main"]');
  await expect(page.locator('[data-drop-zone="main"] [data-widget]')).toBeVisible();
});
```

---

## Chạy tests

```bash
# Backend unit
cd tests/backend/unit/IOC.Core.Tests && dotnet test

# Backend integration
cd tests/backend/integration/IOC.Api.Tests && dotnet test

# Frontend unit
cd tests/frontend/unit && npm test -- --watchAll=false --coverage

# E2E (cần dev server đang chạy)
cd tests/frontend/e2e && npx playwright test
```

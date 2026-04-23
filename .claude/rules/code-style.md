# Code Style Rules — IOC Project

## Backend (C# / .NET 8)

### Naming
- **Classes, Interfaces, Methods, Properties**: PascalCase
- **Private fields**: `_camelCase` với underscore prefix
- **Local variables, parameters**: camelCase
- **Constants**: `UPPER_SNAKE_CASE`
- **Interfaces**: Prefix `I` (vd: `IPluginService`)

### Structure
- Mỗi file chỉ chứa 1 public class/interface
- Namespace phải match với thư mục
- Using statements được sắp xếp: System → Microsoft → Third-party → Internal
- Không dùng `var` khi type không rõ ràng từ right-hand side

### Async/Await
- Mọi I/O operation phải async
- Method name async phải suffix `Async` (vd: `GetBudgetAsync`)
- Luôn pass `CancellationToken` qua các async chain

### Dependency Injection
- Constructor injection, không dùng service locator
- Interface trước implementation trong constructor
- Không inject `IServiceProvider` trừ factory pattern

---

## Frontend (TypeScript / React)

### Naming
- **Components**: PascalCase, file name match component name
- **Hooks**: `use` prefix + camelCase (vd: `useFinanceData`)
- **Types/Interfaces**: PascalCase
- **Constants**: `UPPER_SNAKE_CASE`
- **Event handlers**: `handle` prefix (vd: `handleSubmit`)

### Components
- Functional components ONLY, không dùng class components
- Named exports (không dùng default export cho components)
- Props interface được định nghĩa ngay trên component
- Không vượt quá 200 dòng — tách component nếu cần

### TypeScript
- `strict: true` trong tsconfig — không bypass với `any`
- Dùng `type` cho union/intersection, `interface` cho object shapes
- Generic types rõ ràng, không viết tắt (vd: `TEntity` không phải `T`)

### ECharts
- Options object phải được `useMemo` để tránh re-render
- Dùng wrapper `<EChart />` component từ `src/shared/components/EChart.tsx`
- Theme được đặt tập trung, không hardcode color

### State Management
- Local state: `useState` / `useReducer`
- Server state: GraphQL queries (Apollo Client hoặc urql)
- Global UI state: React Context (nhẹ) hoặc Zustand (phức tạp)

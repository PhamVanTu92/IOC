# Skill: deploy

## Trigger
Kích hoạt khi người dùng chạy `/project:deploy <environment>`

## Mô tả
Tự động hoá quy trình deploy cho IOC system:
- Build Docker images
- Chạy database migrations
- Deploy services theo đúng thứ tự (infra → backend → frontend)
- Health check sau deploy
- Rollback tự động nếu health check fail

## Quy trình

### Step 1 — Validate
```bash
# Kiểm tra tests pass
cd tests/backend/unit && dotnet test --no-restore
cd tests/frontend/unit && npm test -- --watchAll=false

# Kiểm tra build
cd src/backend && dotnet build IOC.sln --configuration Release
cd src/frontend && npm run build
```

### Step 2 — Build Images
```bash
docker compose -f docker/docker-compose.yml build --no-cache
```

### Step 3 — Deploy theo thứ tự
```bash
# 1. Infrastructure (Kafka, SQL Server, Redis)
docker compose up -d zookeeper kafka sqlserver redis

# 2. Run migrations
docker compose run --rm api dotnet ef database update

# 3. Backend API
docker compose up -d api

# 4. Frontend
docker compose up -d frontend
```

### Step 4 — Health Check
```bash
# Chờ services ready
sleep 10

# Check backend
curl -f http://localhost:5000/health/ready || ROLLBACK=true

# Check frontend
curl -f http://localhost:5173 || ROLLBACK=true

# Rollback nếu cần
if [ "$ROLLBACK" = "true" ]; then
  docker compose down
  git checkout HEAD~1
  docker compose up -d
fi
```

## Environment Variables

| Env        | Config File                    |
|------------|-------------------------------|
| dev        | docker/docker-compose.dev.yml  |
| staging    | docker/docker-compose.yml      |
| production | Yêu cầu approval + backup DB  |

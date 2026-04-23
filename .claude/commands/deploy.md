# /project:deploy — Deploy

**Cú pháp:** `/project:deploy <environment>`
- Environments: `dev` | `staging` | `production`

## Pre-deploy checklist

- [ ] Tests pass: `dotnet test` + `npm test`
- [ ] Build thành công: `dotnet build` + `npm run build`
- [ ] Migration đã chạy: `dotnet ef database update`
- [ ] `.env` đúng cho môi trường target
- [ ] Docker images đã build

## Quy trình deploy

```bash
# 1. Build images
docker compose -f docker/docker-compose.yml build

# 2. Run migrations
docker compose run --rm backend dotnet ef database update

# 3. Deploy services
docker compose -f docker/docker-compose.yml up -d

# 4. Health check
curl http://localhost:5000/health
curl http://localhost:5173
```

## Rollback

```bash
# Quay về version trước
docker compose down
git checkout <previous-tag>
docker compose up -d
```

## Lưu ý
- Production deploy PHẢI có approval từ Tech Lead
- Luôn backup DB trước khi migrate production

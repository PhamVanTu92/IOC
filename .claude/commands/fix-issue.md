# /project:fix-issue — Fix Issue

**Cú pháp:** `/project:fix-issue #<issue-number>` hoặc `/project:fix-issue <mô tả lỗi>`

## Quy trình

1. **Tái hiện lỗi** — Chạy test hiện tại để xác nhận lỗi tồn tại
2. **Xác định root cause** — Trace từ symptom → origin
3. **Viết failing test** — Test phải fail trước khi fix
4. **Implement fix** — Fix nhỏ nhất có thể giải quyết vấn đề
5. **Verify** — Chạy lại toàn bộ test suite

## Điều tra

```bash
# Backend logs
cd src/backend && dotnet run --project IOC.Api 2>&1 | tail -50

# Frontend console errors
# Kiểm tra browser DevTools → Console

# Kafka events
docker exec -it ioc-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic ioc.errors --from-beginning
```

## Lưu ý
- Không fix nhiều issues trong 1 commit
- Branch name: `fix/<ten-issue>`
- Commit message: `fix: <mô tả ngắn gọn>`

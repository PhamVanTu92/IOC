# Skill: security-review

## Trigger
Tự động kích hoạt khi:
- Người dùng yêu cầu review bảo mật
- Trước khi tạo PR lên `main`/`production`
- Phát hiện thay đổi trong authentication/authorization code

## Mô tả
Thực hiện security audit toàn diện cho IOC codebase, tập trung vào:
- Injection vulnerabilities (SQL, GraphQL, NoSQL)
- Authentication & Authorization gaps
- Sensitive data exposure
- Kafka message validation
- SignalR connection security

## Checklist thực thi

### 1. Secrets & Credentials
```bash
# Tìm hardcoded secrets
grep -rn "password\|secret\|api_key\|connectionstring" src/ \
  --include="*.cs" --include="*.ts" --include="*.json" \
  -i | grep -v "*.example" | grep -v "settings.local"
```

### 2. GraphQL Security
- [ ] Introspection tắt trên production
- [ ] Query depth limit được cấu hình
- [ ] Rate limiting trên mutations
- [ ] Authorization attributes trên tất cả resolvers

### 3. Kafka Security
- [ ] Message schema validation trước khi consume
- [ ] Dead-letter queue cho invalid messages
- [ ] Không expose internal errors trong Kafka events

### 4. SignalR Security
- [ ] JWT validation trên hub connections
- [ ] Group membership được verify server-side
- [ ] Input sanitization trước khi broadcast

### 5. API Security
- [ ] CORS policy restrictive
- [ ] HTTPS enforced
- [ ] Request size limits
- [ ] SQL injection prevention (parameterized queries / EF Core)

## Output
Báo cáo dạng markdown với:
- Severity: Critical / High / Medium / Low
- File path + line number
- Mô tả lỗi
- Cách fix đề xuất

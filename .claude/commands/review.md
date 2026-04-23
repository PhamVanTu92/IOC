# /project:review — Code Review

Review toàn bộ staged changes hoặc diff so với branch `main`.

## Checklist

### Code Quality
- [ ] Không có magic numbers/strings chưa được đặt tên
- [ ] Không có code trùng lặp (DRY principle)
- [ ] Naming rõ ràng, phản ánh đúng intent
- [ ] Không có dead code hoặc commented-out code

### Backend (.NET)
- [ ] Các method dài hơn 30 dòng cần được refactor
- [ ] Repository/Service pattern được tuân thủ
- [ ] Không truy cập DB trực tiếp từ Controller
- [ ] IPlugin contract được implement đầy đủ

### Frontend (React)
- [ ] Component không vượt quá 200 dòng
- [ ] Custom hooks cho logic tái sử dụng
- [ ] ECharts options được memo hóa (`useMemo`)
- [ ] Plugin được đăng ký qua PluginRegistry

### Tests
- [ ] Unit test cho business logic mới
- [ ] Integration test nếu có endpoint mới
- [ ] Test coverage ≥ 80%

### Security
- [ ] Không hardcode credentials
- [ ] Input validation ở API layer
- [ ] Authorization check trên mutation/query

## Thực thi

```bash
# Xem diff
git diff main...HEAD

# Chạy test
cd tests/backend/unit && dotnet test
cd tests/frontend/unit && npm test -- --watchAll=false
```

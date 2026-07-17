# Hướng dẫn dùng subagent hằng ngày

| Agent | Dùng khi |
|---|---|
| `kpi_explorer` | Tìm code, call path và phạm vi ảnh hưởng; không sửa code |
| `kpi_planner` | Lập kế hoạch cho tính năng lớn; không sửa code |
| `kpi_frontend` | Làm Razor, Bootstrap, CSS và JavaScript |
| `kpi_backend` | Làm controller, EF Core, validation và business rules |
| `kpi_verifier` | Review độc lập, build, test và tìm regression; không sửa code |

## Prompt dùng nhanh

### Task nhỏ — chỉ main agent

```text
[Mô tả task]. Đây là task nhỏ: main agent tự khảo sát, triển khai và kiểm tra. Không dùng subagent.
```

### Task vừa — main agent và Verifier

```text
[Mô tả task]. Main agent tự lập kế hoạch và triển khai. Sau khi xong, gọi kpi_verifier review độc lập; main agent xử lý findings hợp lệ và kiểm tra cuối.
```

### Task lớn — đầy đủ workflow

```text
[Mô tả tính năng]. Đây là task lớn. Dùng kpi_explorer khảo sát, kpi_planner lập kế hoạch, rồi main agent phân chia frontend/backend theo file ownership không chồng lấn. Chỉ chạy kpi_frontend và kpi_backend song song khi không sửa chung file. Cuối cùng gọi kpi_verifier và để main agent tích hợp, sửa findings, kiểm tra cuối.
```

### Chỉ khảo sát code

```text
Gọi kpi_explorer khảo sát [module/vấn đề], trả về symbol, call path, file bị ảnh hưởng và rủi ro. Không sửa code.
```

### Chỉ lập kế hoạch

```text
Gọi kpi_planner lập kế hoạch decision-complete cho [tính năng]. Ghi dependency, thứ tự, file ownership, acceptance criteria và test cases. Không sửa code.
```

### Chỉ làm frontend

```text
Gọi kpi_frontend thực hiện [phần giao diện]. Chỉ sửa các file frontend được giao, giữ nguyên backend contract và chạy kiểm tra phù hợp.
```

### Chỉ làm backend

```text
Gọi kpi_backend thực hiện [business flow/API/controller]. Không sửa file frontend; bổ sung test cho logic không tầm thường và chạy kiểm tra phù hợp.
```

### Chỉ review và test

```text
Gọi kpi_verifier review thay đổi hiện tại, chạy build/test phù hợp và báo findings theo mức độ nghiêm trọng. Không sửa code.
```

### Nhiều agent với file ownership rõ ràng

```text
Dùng các subagent phù hợp cho [task]. Trước khi triển khai, main agent phải chia file ownership không chồng lấn. Không cho hai agent sửa cùng file; chờ tất cả hoàn thành rồi tích hợp và kiểm tra cuối.
```

### Ép chỉ dùng main agent

```text
[Mô tả task]. Chỉ dùng main agent làm toàn bộ từ khảo sát đến kiểm tra. Không spawn bất kỳ subagent nào.
```

### Để main agent tự chọn

```text
[Mô tả task]. Main agent hãy phân loại task nhỏ/vừa/lớn và dùng số subagent tối thiểu cần thiết theo AGENTS.md. Nói ngắn gọn cách phân chia trước khi làm và chịu trách nhiệm tích hợp, kiểm tra cuối.
```

> Mẹo: task nhỏ dùng main agent; task vừa thường chỉ cần thêm `kpi_verifier`; task lớn mới dùng Explorer, Planner, Frontend và Backend.

## Skill tự động

Bạn không cần ghi tên skill trong mọi prompt. Mỗi agent sẽ tự chọn skill phù hợp khi task thật sự cần và bỏ qua skill khi pattern sẵn có của dự án đã đủ.

Nếu muốn chỉ định rõ, thêm một câu như:

```text
Dùng skill phù hợp nếu nó thực sự cần cho task; không dùng nhiều skill trùng chức năng.
```

Hoặc ép lựa chọn cụ thể:

```text
Dùng skill impeccable cho phần audit và polish UI này.
```

```text
Không dùng skill cho task nhỏ này; chỉ làm theo pattern hiện có của repository.
```

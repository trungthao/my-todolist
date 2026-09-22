# My Todolist

Công cụ quản lý công việc cá nhân dạng Kanban (Cần làm / Đang làm / Đã xong) với theo dõi thời gian tự động cho công việc đang làm.

- **Backend**: ASP.NET Core Web API (.NET 9) + EF Core (Pomelo MySQL provider) + JWT auth
- **Frontend**: Vue 3 + Vite + PrimeVue + vuedraggable
- **Database**: MySQL

## 1. Cấu hình database (backend)

Mở `backend/TodoApi/appsettings.Development.json` (file này đã được gitignore, không commit) và điền connection string MySQL thật của bạn:

```json
{
  "ConnectionStrings": {
    "Default": "Server=YOUR_MYSQL_HOST;Port=3306;Database=todolist;User=YOUR_USER;Password=YOUR_PASSWORD;"
  },
  "Jwt": {
    "Key": "<khóa bí mật đã được tạo sẵn, có thể thay bằng chuỗi ngẫu nhiên khác>"
  },
  "Auth": {
    "DefaultUsername": "admin",
    "DefaultPassword": "changeme123"
  }
}
```

Database `todolist` cần được tạo trước (`CREATE DATABASE todolist;`) — EF Core sẽ tự tạo bảng khi chạy lần đầu.

> Tài khoản đăng nhập mặc định được tạo tự động ở lần chạy đầu tiên (nếu bảng `Users` rỗng) từ `Auth:DefaultUsername` / `Auth:DefaultPassword`. Đổi giá trị này trước khi chạy lần đầu nếu muốn dùng mật khẩu khác.

Nếu MySQL server của bạn không phải bản 8.0.x, sửa dòng `new MySqlServerVersion(new Version(8, 0, 36))` trong `Program.cs` cho khớp phiên bản thật.

## 2. Chạy backend

```bash
cd backend/TodoApi
dotnet run
```

API chạy tại `http://localhost:5108`. Migration sẽ tự áp dụng vào DB khi khởi động (`Database.MigrateAsync()`).

## 3. Chạy frontend

```bash
cd frontend
npm install
npm run dev
```

Mở `http://localhost:5173`, đăng nhập bằng tài khoản đã cấu hình ở bước 1. Vite dev server tự proxy `/api` sang backend (`localhost:5108`).

## Ghi chú về logic tính thời gian

- Khi kéo một công việc vào **Đang làm**, hệ thống ghi lại thời điểm bắt đầu.
- Khi kéo ra khỏi **Đang làm** (dù là về **Cần làm** hay sang **Đã xong**), thời gian đã trôi qua được cộng dồn vào tổng thời gian của công việc đó.
- Nếu kéo lại vào **Đang làm** lần nữa, đồng hồ tiếp tục chạy cộng thêm vào tổng đã có — không bị reset.

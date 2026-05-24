# BaiTapLon (CineManager) — Hướng dẫn chạy dự án

Repo này gồm 3 phần chính:

- `BaiTapLon/` — **WinForms** (ứng dụng desktop quản trị/bán vé)
- `BaiTapLon.Api/` — **ASP.NET Core Web API** (backend cho web)
- `cinemanager-web/` — **Vite + React** (frontend)

Ngoài ra:

- `BaiTapLon.Shared/` — chia sẻ `Models` + `AppDbContext` cho API
- `BaiTapLon.Tests/` — Unit tests

## 1) Yêu cầu môi trường

### Windows
- Windows (vì `BaiTapLon/` là WinForms)

### .NET
- **.NET SDK 10** (dự án target `net10.0` / `net10.0-windows`)
  - `BaiTapLon/` → `net10.0-windows`
  - `BaiTapLon.Api/` → `net10.0`

### Node.js
- Node.js **18+** (khuyến nghị 20+) + npm

### Database
- SQL Server LocalDB (khuyến nghị cài kèm Visual Studio) hoặc SQL Server/SQL Express

## 2) Cấu hình database (connection string)

Có **2** file `appsettings.json` riêng:

- `BaiTapLon/appsettings.json` (WinForms + EF migrations nằm ở project này)
- `BaiTapLon.Api/appsettings.json` (API đọc connection string ở đây)

Mặc định API dùng LocalDB:

- `Server=(localdb)\\MSSQLLocalDB;Database=CinemaManagement;Trusted_Connection=True;MultipleActiveResultSets=true`

Nếu bạn đổi DB server, hãy đổi **cả 2** file cho đồng bộ.

## 3) Chạy migrations tạo schema

Migrations nằm trong project `BaiTapLon/` (thư mục `BaiTapLon/Migrations/`).

### Cách A (khuyến nghị): dùng CLI `dotnet ef`

1. Cài EF tool (nếu máy chưa có):
   - `dotnet tool install --global dotnet-ef`

2. Apply migrations:

- Mở terminal tại repo root rồi chạy:
  - `cd BaiTapLon`
  - `dotnet ef database update`

> `AppDbContextFactory` đã được cấu hình để EF CLI chạy được.

### Cách B: chạy app để seed (nếu bạn đã có DB)

`AppDbContext` có seed data (Genres/Rooms/Seats/Snacks). Tuy nhiên việc **tạo schema** vẫn nên dùng migrations ở Cách A.

## 4) Chạy Backend API

API mặc định chạy tại:

- `http://localhost:5217`

Chạy API từ repo root:

- `dotnet run --project BaiTapLon.Api`

Ghi chú:
- CORS policy `CineManagerWeb` hiện cho phép origin: `http://localhost:5173` và `https://localhost:5173`

## 5) Chạy Frontend (Vite + React)

Frontend gọi API thông qua biến môi trường `VITE_API_URL` (xem `cinemanager-web/src/api/client.ts`).

1. Tạo file env:
- Copy `cinemanager-web/.env.example` → `cinemanager-web/.env`

2. Cài deps & chạy:

- `cd cinemanager-web`
- `npm install`
- `npm run dev`

Vite mặc định chạy:
- `http://localhost:5173`

## 6) Chạy ứng dụng WinForms

Chạy WinForms từ repo root:

- `dotnet run --project BaiTapLon`

Hoặc mở solution trong Visual Studio và chạy project `BaiTapLon`.

## 7) Chạy test

- `dotnet test`

## 8) Ghi chú về `.env` và bảo mật

- Repo đã cấu hình `.gitignore` để **không commit** các file `.env`.
- Chỉ commit các file mẫu như `.env.example`.

> ASP.NET Core **không tự động đọc** file `.env` (trừ khi bạn thêm thư viện loader). Nếu muốn override config cho API, bạn có thể dùng environment variables hoặc tạo `appsettings.Development.json` (local).

## 9) Troubleshooting nhanh

- **Frontend không gọi được API / lỗi CORS**: kiểm tra API đang chạy `http://localhost:5217` và web đang ở `http://localhost:5173`. Nếu đổi port, cập nhật:
  - `cinemanager-web/.env` (`VITE_API_URL`)
  - `BaiTapLon.Api/Program.cs` (CORS origins)

- **Lỗi DB / không có bảng**: chạy lại migrations `dotnet ef database update` trong thư mục `BaiTapLon/`.

<div align="center">

# 🚀 ERP Portal API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?style=for-the-badge&logo=JSON%20web%20tokens&logoColor=white)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)](https://swagger.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

**Modern ERP Portal Backend API built with .NET 8.0 & Clean Architecture**

[Features](#-features) • [Tech Stack](#-tech-stack) • [Getting Started](#-getting-started) • [API Documentation](#-api-documentation) • [Contact](#-contact)

</div>

---

## 📋 Overview

**ERP Portal API** là một hệ thống backend hiện đại cho quản lý doanh nghiệp (Enterprise Resource Planning), được xây dựng với kiến trúc sạch (Clean Architecture) và các công nghệ .NET mới nhất. Hệ thống cung cấp các API RESTful cho quản lý tài khoản, xác thực JWT, phân quyền người dùng và tích hợp với các module ERP.

## ✨ Features

- 🔐 **JWT Authentication** - Xác thực bảo mật với JSON Web Tokens
- 👥 **User Management** - Quản lý người dùng và phân quyền
- 📊 **Menu & Permissions** - Hệ thống menu động và quản lý quyền truy cập
- 🏗️ **Clean Architecture** - Tách biệt rõ ràng giữa Domain, Application, và Infrastructure layers
- 📚 **Swagger UI** - API documentation tự động và interactive
- 🔄 **AutoMapper** - Object-to-object mapping tự động
- 🐳 **Docker Ready** - Multi-stage build, non-root user, healthcheck, secret qua env

## 🛠️ Tech Stack

### Backend Framework
- **.NET 8.0** - Latest version of .NET framework
- **ASP.NET Core Web API** - High-performance API framework
- **C#** - Primary programming language

### Authentication & Security
- **JWT Bearer Authentication** - Stateless authentication
- **Microsoft.IdentityModel.Tokens** - Token validation and generation
- **ASP.NET Core Identity** - User management framework

### Libraries & Tools
- **AutoMapper 10.1.1** - Object mapping
- **Swashbuckle.AspNetCore 6.6.2** - Swagger/OpenAPI support
- **System.IdentityModel.Tokens.Jwt 8.0.0** - JWT handling

### Architecture Pattern
- **Clean Architecture** - Domain-driven design
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Loose coupling

## 🏗️ Project Structure

```
ERP_Portal_RC/
├── 📁 ERP_Portal_RC/              # API Layer (Presentation)
│   ├── Controllers/               # API Controllers
│   ├── Program.cs                 # Application entry point
│   └── appsettings.json          # Configuration
│
├── 📁 ERP_Portal_RC.Application/  # Application Layer
│   ├── Interfaces/                # Service interfaces
│   ├── Services/                  # Business logic services
│   ├── Mappings/                  # AutoMapper profiles
│   └── DTOs/                      # Data Transfer Objects
│
├── 📁 ERP_Portal_RC.Domain/       # Domain Layer
│   ├── Entities/                  # Domain entities
│   └── Interfaces/                # Repository interfaces
│
└── 📁 ERP_Portal_RC.Infrastructure/ # Infrastructure Layer
    └── Repositories/              # Data access implementations
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (hoặc database tương thích)
- IDE: Visual Studio 2022 hoặc VS Code

### Installation

1. **Clone repository**
   ```bash
   git clone https://github.com/minhnhatluongg/api-erp-reCode.git
   cd ERP_Portal_RC
   ```

2. **Configure appsettings.json**
   ```json
   {
        "ConnectionStrings": {
    "BosAccount": "Server=YOUR_SERVER;Initial Catalog=BosAccount;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosApproval": "Server=YOUR_SERVER;Initial Catalog=BosApproval;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosAsset": "Server=YOUR_SERVER;Initial Catalog=BosAsset;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosCataloge": "Server=YOUR_SERVER;Initial Catalog=BosCataloge;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosConfigure": "Server=YOUR_SERVER;Initial Catalog=BosConfigure;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosDocument": "Server=YOUR_SERVER;Initial Catalog=BosDocument;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosEVAT": "Server=YOUR_SERVER;Initial Catalog=BosEVAT;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosHumanResource": "Server=YOUR_SERVER;Initial Catalog=BosHumanResource;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosInfo": "Server=YOUR_SERVER;Initial Catalog=BosInfo;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosInventory": "Server=YOUR_SERVER;Initial Catalog=BosInventory;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosManufacture": "Server=YOUR_SERVER;Initial Catalog=BosManufacture;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosOnline": "Server=YOUR_SERVER;Initial Catalog=BosOnline;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosSales": "Server=YOUR_SERVER;Initial Catalog=BosSales;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosSupply": "Server=YOUR_SERVER;Initial Catalog=BosSupply;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
    "BosWarehouseData": "Server=YOUR_SERVER;Initial Catalog=BosWarehouseData;Persist Security Info=False;User ID=USERNAME;Password=YOUR_PASSWORD;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=360;",
   }
  
   ```

3. **Restore dependencies**
   ```bash
   dotnet restore
   ```

4. **Build project**
   ```bash
   dotnet build
   ```

5. **Run application**
   ```bash
   dotnet run --project ERP_Portal_RC/API.ERP_Portal_RC.csproj
   ```

6. **Access Swagger UI**
   - Development: `https://localhost:5001/swagger`
   - Production: `https://localhost:5001/api-docs`

## 📖 API Documentation

Sau khi chạy ứng dụng, truy cập Swagger UI để xem tài liệu API đầy đủ và test các endpoints:

**Development:** `http://localhost:5000/swagger`

### Main Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Đăng nhập và nhận JWT token |
| POST | `/api/auth/register` | Đăng ký tài khoản mới |
| POST | `/api/auth/refresh-token` | Làm mới access token |
| GET | `/api/account/profile` | Lấy thông tin profile |
| GET | `/api/menu` | Lấy menu theo quyền người dùng |

## 🔧 Configuration

### JWT Settings
Cấu hình JWT trong `appsettings.json`:
```json
"Jwt": {
  "SecretKey": "YourSuperSecretKeyHere_MustBeLongEnough",
  "Issuer": "ERPPortalAPI",
  "Audience": "ERPPortalClients",
  "ExpiryInMinutes": 60
}
```

### CORS Policy
Mặc định API cho phép tất cả origins. Để bảo mật hơn trong production, cấu hình lại CORS policy trong `Program.cs`.

## 🐳 Docker Support

Project đã được Dockerize hoàn chỉnh với multi-stage build, non-root user, healthcheck và inject secret qua biến môi trường. Xem chi tiết trong [DOCKER.md](./DOCKER.md).

### ⚡ Quick Start với Docker Compose

**Bước 1 — Chuẩn bị file `.env`**

Copy file mẫu và điền giá trị thật (password DB, JWT secret, SMTP, API keys...):

```bash
cp .env.example .env
# sau đó mở .env bằng editor và điền các giá trị
```

> ⚠️ File `.env` đã được `.gitignore` — KHÔNG BAO GIỜ commit file này lên git.

**Bước 2 — Build & Run**

```bash
# Build image và start container ở background
docker compose up -d --build

# Xem log realtime
docker compose logs -f erp-portal-api

# Kiểm tra trạng thái (cột STATUS phải là "healthy")
docker compose ps

# Dừng & xoá
docker compose down
```

**Bước 3 — Truy cập API**

- Swagger UI: <http://localhost:5000>
- Swagger JSON: <http://localhost:5000/swagger/v1/swagger.json>

### 🛠️ Build thủ công (không dùng compose)

```bash
# Build image
docker build -t erp-portal-api:latest .

# Run
docker run -d \
  --name erp-portal-api \
  -p 5000:80 \
  --env-file .env \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e FileConfig__PhysicalRootPath=/app/Attachments \
  -e EContractLogConfig__LogPath=/app/Logs/EContract \
  -v $(pwd)/data/attachments:/app/Attachments \
  -v $(pwd)/data/logs:/app/Logs \
  erp-portal-api:latest
```

### 📐 Kiến trúc Docker

| Stage | Base image | Mục đích |
|---|---|---|
| `build` | `mcr.microsoft.com/dotnet/sdk:8.0` | Restore NuGet + build solution |
| `publish` | kế thừa `build` | `dotnet publish` tạo artifacts |
| `runtime` | `mcr.microsoft.com/dotnet/aspnet:8.0` | Image cuối (~250 MB), chạy dưới user non-root `appuser` |

### 🔐 Inject secret qua biến môi trường

ASP.NET Core tự động map env vars vào `IConfiguration` theo quy tắc **`__` = `:`**.

Ví dụ:
```bash
ConnectionStrings__BosAccount=Server=...       →  Configuration["ConnectionStrings:BosAccount"]
Jwt__SecretKey=xxxx                            →  Configuration["Jwt:SecretKey"]
SmtpSettings__Host=mail.example.com            →  Configuration["SmtpSettings:Host"]
```

Toàn bộ secret (JWT key, connection strings, SMTP password, API keys) được inject qua `.env` → `docker-compose.yml` → container. **Không hard-code trong `appsettings.json`** khi deploy production.

### 📂 Volumes persist

Data được mount ra host để không mất khi rebuild container:

| Host path | Container path | Dùng cho |
|---|---|---|
| `./data/attachments` | `/app/Attachments` | File upload của user |
| `./data/logs` | `/app/Logs` | Log file (EContract, ...) |
| `./data/uploads` | `/app/Uploads` | Thư mục upload phụ |

### 🌐 HTTPS trong container

App trong container chỉ listen HTTP port `80`. Có 2 cách terminate TLS:

1. **Khuyến nghị:** Đặt reverse proxy (Nginx / Traefik / Caddy) phía trước container, proxy lo HTTPS.
2. Hoặc config Kestrel với certificate `.pfx` (xem chi tiết trong [DOCKER.md](./DOCKER.md)).

### ✅ Health Check

Container tự động kiểm tra sức khoẻ mỗi 30s qua endpoint `/swagger/v1/swagger.json`. Dùng lệnh sau để xem trạng thái:

```bash
docker inspect --format='{{.State.Health.Status}}' erp-portal-api
```

### 📋 Checklist trước khi deploy

- [ ] File `.env` đã có đầy đủ secret và được `.gitignore`
- [ ] Password DB, JWT secret, API key đã được **rotate** nếu đã lộ trên git trước đó
- [ ] Đã set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Đã có reverse proxy với HTTPS phía trước
- [ ] CORS policy đã giới hạn origin thay vì `AllowAll`
- [ ] Volume `./data/` đã được backup định kỳ

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Contact

<div align="center">

### 📬 Get in Touch

**Developer:** Minh Nhật Lương

[![Email](https://img.shields.io/badge/Email-cusocisme@gmail.com-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:cusocisme@gmail.com)
[![Email](https://img.shields.io/badge/Email-minhnhatluongwork@gmail.com-D14836?style=for-the-badge&logo=gmail&logoColor=white)](mailto:minhnhatluongwork@gmail.com)
[![GitHub](https://img.shields.io/badge/GitHub-minhnhatluongg-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/minhnhatluongg)

---

**⭐ If you find this project useful, please give it a star! ⭐**

Made with ❤️ by Minh Nhật Lương

</div>

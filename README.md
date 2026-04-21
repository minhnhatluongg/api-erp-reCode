<div align="center">

# 🚀 ERP Portal API

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![JWT](https://img.shields.io/badge/JWT-Authentication-000000?style=for-the-badge&logo=JSON%20web%20tokens&logoColor=white)](https://jwt.io/)
[![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)](https://swagger.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg?style=for-the-badge)](LICENSE)

**Modern ERP Portal Backend API built with .NET 8.0 & Clean Architecture**

[Features](#-features) • [Tech Stack](#-tech-stack) • [Modules](#-modules) • [Getting Started](#-getting-started) • [Docker](#-docker-support) • [API Docs](#-api-documentation)

</div>

---

## 📋 Overview

**ERP Portal API** là backend hệ thống ERP (Enterprise Resource Planning) của Win Tech, được xây dựng theo kiến trúc **Clean Architecture** trên nền tảng **.NET 8.0**. Hệ thống phục vụ các nghiệp vụ chính: quản lý tài khoản, xác thực JWT, hợp đồng điện tử (E-Contract), chữ ký số (HSM), hoá đơn điện tử (E-Invoice), quản lý menu/phân quyền, và tích hợp với nhiều hệ thống ngoài (Incom, WinInvoice, HR Account API...).

## ✨ Features

- 🔐 **JWT Authentication** - Xác thực bảo mật với Access Token + Refresh Token, hỗ trợ logout / revoke token
- 👥 **Account & User Management** - Quản lý tài khoản, đổi mật khẩu, check tồn tại, quản lý người dùng kỹ thuật
- 📊 **Menu & Permissions** - Menu động theo người dùng, kiểm tra quyền truy cập theo `menuId`
- 📝 **Electronic Contract (E-Contract)** - Tạo, ký, theo dõi, tìm kiếm, phân trang, đính kèm file hợp đồng điện tử
- ✍️ **Digital Signatures & HSM** - Ký số qua thiết bị (USB Token) và HSM (Hardware Security Module)
- 🧾 **Electronic Invoice** - Tạo, preview, count hoá đơn điện tử, tích hợp WinInvoice
- 🏢 **Company & Sales Hierarchy** - Quản lý công ty, cấu trúc phân cấp kinh doanh, vendor, department
- 🔌 **External Integrations** - Tích hợp Incom, WinInvoice, HR Account API, SMTP
- 📂 **File Upload/Storage** - Upload, quản lý file đính kèm, serve static file
- 🗃️ **Multi-Database** - Kết nối 19+ database SQL Server (Bos* family + external servers)
- 🏗️ **Clean Architecture** - Tách biệt rõ Domain / Application / Infrastructure / API layers
- 📚 **Swagger UI** - API documentation tự động, hỗ trợ Bearer token authentication
- 🧪 **Unit Tests** - xUnit + Moq + FluentAssertions + Coverlet
- 🐳 **Docker Ready** - Multi-stage build, non-root user, healthcheck, secret qua env

## 🛠️ Tech Stack

### Framework & Runtime
- **.NET 8.0** — LTS framework
- **ASP.NET Core Web API** — High-performance API
- **C# 12** — Primary language

### Authentication & Security
- **JWT Bearer Authentication** — Stateless auth
- **System.IdentityModel.Tokens.Jwt 8.0** — Token handling
- **ASP.NET Core Identity** — User management scaffolding

### Data Access
- **Dapper 2.1** — Micro-ORM nhẹ, truy vấn raw SQL hiệu năng cao
- **System.Data.SqlClient 4.8** — SQL Server driver
- **Entity Framework Core 8 (SqlServer)** — Dùng cho một số scenario ORM-first

### Libraries
- **AutoMapper 12** — Object-to-object mapping
- **Swashbuckle.AspNetCore 6.6** — Swagger/OpenAPI
- **Newtonsoft.Json 13** — JSON serialization
- **RestSharp 114** — HTTP client cho third-party integrations

### Testing
- **xUnit** — Test framework
- **Moq** — Mocking
- **FluentAssertions** — Assertion syntax
- **Coverlet** — Code coverage

### DevOps
- **Docker + Docker Compose** — Containerization
- **Multi-stage build** — Optimize image size (~250 MB)

## 🧩 Modules

| Module | Controller | Scope |
|---|---|---|
| **Auth** | `AuthController` | Login, refresh-token, logout, revoke, change-password, check-exists |
| **Account** | `AccountController` | Menu theo user, current-user, check-permission, check-account |
| **E-Contract** | `EcontractController`, `ContractAttachmentController`, `ContractSignController` | CRUD hợp đồng điện tử, ký hợp đồng, đính kèm file, lịch sử |
| **Digital Signatures** | `DSignaturesController`, `SignHSMController` | Quản lý chứng thư số, ký bằng HSM |
| **Invoice** | `InvoiceController`, `InvoicePreviewController`, `CountInvoiceController` | Tạo, preview, đếm hoá đơn điện tử |
| **Technical User** | `TechnicalUserController` | Đăng ký / login user kỹ thuật |
| **Sales Hierarchy** | `SalesHierarchyController` | Cấu trúc phân cấp bán hàng, manager tree |
| **Service Types** | `ServiceTypesController` | Danh mục loại dịch vụ |
| **File** | `FileController` | Upload / download / serve static |
| **Create Account Workflow** | `CapTaiKhoanController` | Luồng đề xuất cấp tài khoản |
| **Integration** | `IntegrationIncomEcontractController` | Sync với hệ thống Incom |
| **Database Test** | `DatabaseTestController` | Endpoint test kết nối DB |

## 🏗️ Project Structure

```
ERP_Portal_RC/
├── 📁 ERP_Portal_RC/                    # API Layer (Presentation)
│   ├── Controllers/                     # 17 API Controllers
│   ├── Program.cs                       # DI & pipeline configuration
│   ├── appsettings.Sample.json          # Config template
│   └── Uploads/                         # File upload local
│
├── 📁 ERP_Portal_RC.Application/        # Application Layer
│   ├── Services/                        # Business logic (19 services)
│   ├── Interfaces/                      # Service contracts
│   ├── DTOs/                            # Data Transfer Objects (60+)
│   ├── Mappings/                        # AutoMapper profiles
│   └── Common/                          # Shared helpers
│
├── 📁 ERP_Portal_RC.Domain/             # Domain Layer
│   ├── Entities/                        # Core entities (60+)
│   ├── EntitiesIntergration/            # Integration entities (Incom)
│   ├── EntitiesInvoice/                 # WinInvoice entities
│   ├── Enum/                            # Domain enums
│   ├── Interfaces/                      # Repository contracts
│   └── Common/                          # Domain helpers, filters, ApiResponse
│
├── 📁 ERP_Portal_RC.Infrastructure/     # Infrastructure Layer
│   ├── Repositories/                    # Data access (Dapper + EF Core)
│   └── Persistence/                     # DbConnectionFactory, context
│
├── 📁 ERP_Portal_RC.Application.Tests/  # Unit Tests (xUnit + Moq)
│   └── Services/                        # Service test suites
│
├── 🐳 Dockerfile                        # Multi-stage Docker build
├── 🐳 docker-compose.yml                # Compose config với env injection
├── 🔐 .env.example                      # Template cho env variables
├── 📄 ERP_Portal_RC.slnx                # Solution file
└── 📘 HuongDan_UnitTest.docx            # Hướng dẫn viết unit test nội bộ
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (local hoặc remote)
- IDE: Visual Studio 2022 / Rider / VS Code
- (Optional) [Docker Desktop](https://www.docker.com/products/docker-desktop/) nếu muốn chạy container

### Installation (Local)

**1. Clone repository**
```bash
git clone https://github.com/minhnhatluongg/api-erp-reCode.git
cd ERP_Portal_RC
```

**2. Copy config template**

Dùng `appsettings.Sample.json` làm template, tạo `appsettings.json` rồi điền connection string & secret thật:
```bash
cp ERP_Portal_RC/appsettings.Sample.json ERP_Portal_RC/appsettings.json
```

**3. Restore & Build**
```bash
dotnet restore
dotnet build
```

**4. Run**
```bash
dotnet run --project ERP_Portal_RC/API.ERP_Portal_RC.csproj
```

**5. Access Swagger UI**

Mở trình duyệt: <http://localhost:5000> hoặc <https://localhost:5001>

### Run Unit Tests

```bash
# Restore + chạy toàn bộ test
dotnet test

# Filter theo tên
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Code coverage
dotnet test --collect:"XPlat Code Coverage"
```

Xem chi tiết convention viết test trong [HuongDan_UnitTest.docx](./HuongDan_UnitTest.docx) hoặc [ERP_Portal_RC.Application.Tests/README.md](./ERP_Portal_RC.Application.Tests/README.md).

## 🔧 Configuration

### JWT Settings
```json
"Jwt": {
  "SecretKey": "YourSuperSecretKeyHere_MustBeAtLeast32Chars",
  "Issuer": "ERPPortalAPI",
  "Audience": "ERPPortalClient",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7
}
```

### Connection Strings

Hệ thống kết nối **19 database** thuộc họ `Bos*` và một vài server riêng. Xem danh sách đầy đủ trong `appsettings.Sample.json`. Một số connection chính:

| Key | Purpose |
|---|---|
| `BosAccount` | DB tài khoản người dùng |
| `BosApproval` | DB phê duyệt |
| `BosEVAT` | DB hoá đơn điện tử |
| `BosDocument` | DB hợp đồng/tài liệu |
| `BosHumanResource` | DB nhân sự |
| `Bos235`, `Server234` | External server |

### External Integrations

| Section | Dùng cho |
|---|---|
| `SmtpSettings` | Gửi email qua SMTP nội bộ |
| `WinInvoice` | API hoá đơn điện tử WinInvoice |
| `ERPPortal` | Base URL của ERP Portal |
| `HRAccountApi` | API nhân sự (LotViet) |
| `WebApp` | Callback URL cho web client |

### CORS Policy

Mặc định `AllowAll`. **Để bảo mật production, cần giới hạn origin cụ thể** trong `Program.cs`.

## 🐳 Docker Support

Project đã được dockerize hoàn chỉnh: multi-stage build, chạy dưới user non-root, healthcheck endpoint, inject toàn bộ secret qua biến môi trường.

### ⚡ Quick Start

**1. Chuẩn bị `.env`**
```bash
cp .env.example .env
# mở .env và điền giá trị thật (password DB, JWT key, API key...)
```

> ⚠️ File `.env` đã được `.gitignore` — KHÔNG commit lên repo.

**2. Build & Run**
```bash
docker compose up -d --build
docker compose logs -f erp-portal-api
docker compose ps   # cột STATUS phải là "healthy"
```

**3. Truy cập**
- Swagger UI: <http://localhost:5000>

### Kiến trúc Docker

| Stage | Image | Size | Vai trò |
|---|---|---|---|
| `build` | `dotnet/sdk:8.0` | ~700 MB | Restore + build |
| `publish` | kế thừa `build` | — | `dotnet publish` |
| `runtime` | `dotnet/aspnet:8.0` | ~250 MB | Image cuối, chạy `appuser` (non-root) |

### Inject Secret qua Env

ASP.NET Core tự động map env vars vào `IConfiguration` theo quy tắc `__` = `:`:

```bash
ConnectionStrings__BosAccount=Server=...  →  Configuration["ConnectionStrings:BosAccount"]
Jwt__SecretKey=xxxx                       →  Configuration["Jwt:SecretKey"]
SmtpSettings__Host=mail.example.com       →  Configuration["SmtpSettings:Host"]
```

### Volumes Persist

| Host | Container | Dùng cho |
|---|---|---|
| `./data/attachments` | `/app/Attachments` | File upload của user |
| `./data/logs` | `/app/Logs` | Log EContract, ... |
| `./data/uploads` | `/app/Uploads` | Thư mục upload phụ |

### HTTPS

Container chỉ listen HTTP `80`. Khuyến nghị đặt **reverse proxy** (Nginx / Traefik / Caddy) phía trước để terminate TLS. Nếu muốn HTTPS trực tiếp, mount certificate `.pfx` và config Kestrel qua env `ASPNETCORE_Kestrel__Certificates__Default__Path`.

### Manual Build (không dùng compose)

```bash
docker build -t erp-portal-api:latest .
docker run -d --name erp-portal-api -p 5000:80 \
  --env-file .env \
  -v $(pwd)/data/attachments:/app/Attachments \
  -v $(pwd)/data/logs:/app/Logs \
  erp-portal-api:latest
```

### Deploy Checklist

- [ ] File `.env` đã có đầy đủ secret và được `.gitignore`
- [ ] Đã rotate password DB / JWT secret / API keys nếu từng lộ trên git
- [ ] `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Reverse proxy + HTTPS đã sẵn sàng
- [ ] CORS giới hạn origin cụ thể (không còn `AllowAll`)
- [ ] Volume `./data/` có backup định kỳ

## 📖 API Documentation

Sau khi chạy ứng dụng, truy cập Swagger UI để xem và test toàn bộ endpoints:

- **Local:** <http://localhost:5000>
- **Docker:** <http://localhost:5000>
- **Swagger JSON:** `/swagger/v1/swagger.json`

### Main Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Đăng nhập và nhận JWT token |
| POST | `/api/auth/refresh-token` | Làm mới access token |
| POST | `/api/auth/logout` | Đăng xuất (revoke refresh token) |
| POST | `/api/auth/change-password` | Đổi mật khẩu |
| GET  | `/api/auth/me` | Lấy thông tin user hiện tại |
| GET  | `/api/account/menu` | Lấy menu theo quyền user |
| GET  | `/api/account/check-permission/{menuId}` | Kiểm tra quyền truy cập menu |
| POST | `/api/econtract/...` | Các endpoint hợp đồng điện tử |
| POST | `/api/invoice/...` | Các endpoint hoá đơn điện tử |
| POST | `/api/sign-hsm/...` | Các endpoint ký HSM |

## 🧪 Testing

Dự án có sẵn bộ unit test cho tầng `Application/Services` sử dụng **xUnit + Moq + FluentAssertions**.

```bash
dotnet test                                              # Chạy tất cả
dotnet test --filter "FullyQualifiedName~AuthService"    # Filter theo class
dotnet test --collect:"XPlat Code Coverage"              # Coverage report
```

Convention đặt tên test: `[MethodUnderTest]_[Scenario]_[ExpectedResult]`
```csharp
// Ví dụ
ChangePasswordAsync_WhenNewPasswordEqualsOldPassword_ShouldReturnFail
ParseApiLoginString_WhenInputIsEmpty_ShouldReturnEmptyDictionary
```

## 📝 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

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

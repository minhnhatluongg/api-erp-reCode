# 🐳 Hướng dẫn Dockerize ERP Portal API

Tài liệu này hướng dẫn viết `Dockerfile`, `.dockerignore` và `docker-compose.yml` để đóng gói và chạy project **ERP Portal API** (.NET 8.0, Clean Architecture) trong Docker.

---

## 📋 Tổng quan project

| Mục | Giá trị |
|---|---|
| Framework | .NET 8.0 |
| Startup project | `ERP_Portal_RC/API.ERP_Portal_RC.csproj` |
| Output assembly | `API.ERP_Portal_RC.dll` |
| Các project phụ thuộc | `ERP_Portal_RC.Application`, `ERP_Portal_RC.Infrastructure`, `ERP_Portal_RC.Domain` |
| Cổng mặc định | `80` (HTTP) trong container |
| DB | SQL Server (nhiều connection strings, dùng `System.Data.SqlClient` / `Dapper` / `EF Core`) |
| Thư mục cần mount | `D:\Attachments` (upload file) và `D:\Logs\EContract` (log) |

> ⚠️ Lưu ý: Khi chạy trong Linux container, các đường dẫn Windows như `D:\Attachments` trong `appsettings.json` **phải được override** sang đường dẫn Linux (ví dụ `/app/Attachments`) qua biến môi trường hoặc file cấu hình riêng.

---

## 1. File `Dockerfile`

Tạo file tên `Dockerfile` (không có phần mở rộng) ở **thư mục gốc solution** (cùng cấp với `ERP_Portal_RC.slnx`):

```dockerfile
# ============================================================
# Stage 1: BUILD - dùng SDK để restore & build & publish
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 1.1. Copy các file .csproj trước để tận dụng Docker layer cache
#      (nếu chỉ source code thay đổi mà csproj không đổi, step restore sẽ dùng cache)
COPY ["ERP_Portal_RC/API.ERP_Portal_RC.csproj",                      "ERP_Portal_RC/"]
COPY ["ERP_Portal_RC.Application/ERP_Portal_RC.Application.csproj",  "ERP_Portal_RC.Application/"]
COPY ["ERP_Portal_RC.Infrastructure/ERP_Portal_RC.Infrastructure.csproj", "ERP_Portal_RC.Infrastructure/"]
COPY ["ERP_Portal_RC.Domain/ERP_Portal_RC.Domain.csproj",            "ERP_Portal_RC.Domain/"]

# 1.2. Restore NuGet packages theo project startup (kéo theo tất cả reference)
RUN dotnet restore "ERP_Portal_RC/API.ERP_Portal_RC.csproj"

# 1.3. Copy toàn bộ source code còn lại
COPY . .

# 1.4. Build project ở cấu hình Release
WORKDIR "/src/ERP_Portal_RC"
RUN dotnet build "API.ERP_Portal_RC.csproj" -c Release -o /app/build --no-restore

# ============================================================
# Stage 2: PUBLISH - tạo output tối ưu cho production
# ============================================================
FROM build AS publish
RUN dotnet publish "API.ERP_Portal_RC.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ============================================================
# Stage 3: RUNTIME - image nhẹ chỉ chứa ASP.NET runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 3.1. Cài timezone + icu (cho globalization) + curl (cho healthcheck)
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*
ENV TZ=Asia/Ho_Chi_Minh

# 3.2. Tạo user không phải root để chạy app (bảo mật hơn)
RUN groupadd -g 1000 appuser && useradd -u 1000 -g appuser -m appuser

# 3.3. Tạo sẵn các folder mà app cần (Attachments, Logs)
RUN mkdir -p /app/Attachments /app/Logs/EContract /app/Uploads \
    && chown -R appuser:appuser /app

# 3.4. Copy output đã publish từ stage trước
COPY --from=publish --chown=appuser:appuser /app/publish .

# 3.5. Cấu hình runtime
ENV ASPNETCORE_URLS=http://+:80 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true

EXPOSE 80

# 3.6. Healthcheck - kiểm tra app còn sống không
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:80/swagger/v1/swagger.json || exit 1

USER appuser

ENTRYPOINT ["dotnet", "API.ERP_Portal_RC.dll"]
```

### Giải thích các stage

| Stage | Mục đích | Image cơ sở |
|---|---|---|
| `build` | Chứa SDK để restore + build | `dotnet/sdk:8.0` (~700 MB) |
| `publish` | Chạy `dotnet publish` tạo artifacts | kế thừa `build` |
| `runtime` | Image cuối cùng để deploy, chỉ có runtime | `dotnet/aspnet:8.0` (~220 MB) |

Multi-stage build giúp image cuối cùng **không chứa source code và SDK**, nhẹ hơn gấp 3 lần.

---

## 2. File `.dockerignore`

Tạo file `.dockerignore` ở **thư mục gốc solution** để loại trừ file không cần thiết khi build (tăng tốc build, tránh copy file bí mật):

```gitignore
# Build output
**/bin/
**/obj/
**/out/

# IDE files
.vs/
.vscode/
.idea/
*.user
*.suo
*.userosscache

# Git
.git/
.gitignore
.github/

# Docker
Dockerfile
.dockerignore
docker-compose*.yml

# Logs & temp
**/Logs/
**/*.log
**/Uploads/

# Docs (không cần vào image)
*.md
*.docx
LICENSE

# Secrets (BẮT BUỘC)
**/appsettings.Development.json
**/appsettings.Local.json
**/secrets.json
**/*.pfx
**/*.key
**/*.pem

# Test project (nếu không muốn đưa vào image production)
ERP_Portal_RC.Application.Tests/
```

---

## 3. File `docker-compose.yml`

Tạo file `docker-compose.yml` ở thư mục gốc để chạy nhanh và quản lý volume, network, biến môi trường:

```yaml
services:
  erp-portal-api:
    container_name: erp-portal-api
    build:
      context: .
      dockerfile: Dockerfile
    image: erp-portal-api:latest
    restart: unless-stopped
    ports:
      - "5000:80"   # host:container
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:80
      TZ: Asia/Ho_Chi_Minh

      # ---- Override các đường dẫn Windows sang Linux ----
      FileConfig__PhysicalRootPath: /app/Attachments
      FileConfig__BaseUrl: https://api-erprc.win-tech.vn
      EContractLogConfig__LogPath: /app/Logs/EContract
      EContractLogConfig__RetentionDays: "14"

      # ---- JWT (nên để trong .env, không hard-code ở đây) ----
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Jwt__Issuer: ERPPortalAPI
      Jwt__Audience: ERPPortalClient
      Jwt__AccessTokenExpirationMinutes: "60"
      Jwt__RefreshTokenExpirationDays: "7"

      # ---- Connection strings (có thể override từ .env) ----
      ConnectionStrings__BosAccount: ${CONN_BOS_ACCOUNT}
      ConnectionStrings__BosApproval: ${CONN_BOS_APPROVAL}
      # ... thêm các connection string khác theo appsettings.json

    volumes:
      - ./data/attachments:/app/Attachments
      - ./data/logs:/app/Logs
      - ./data/uploads:/app/Uploads
    networks:
      - erp-network
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:80/swagger/v1/swagger.json"]
      interval: 30s
      timeout: 5s
      retries: 3
      start_period: 30s

networks:
  erp-network:
    driver: bridge
```

### File `.env` đi kèm (không commit lên git!)

```bash
# .env
JWT_SECRET_KEY=UmVidWlsZC1Qb3J0YWwtRVJQLVdpdGgtTG90VGVhbURldi1NZXNzYWdlQnktTU5MLVdpblRlY2g=
CONN_BOS_ACCOUNT=Server=10.10.111.3;Initial Catalog=BosAccount;User ID=bos;Password=xxxxx;TrustServerCertificate=True;
CONN_BOS_APPROVAL=Server=10.10.111.3;Initial Catalog=BosApproval;User ID=bos;Password=xxxxx;TrustServerCertificate=True;
# ... các connection string khác
```

> ✅ Thêm `.env` vào `.gitignore` để không lộ credential lên repo.

---

## 4. Build & Run

### 4.1. Dùng Docker trực tiếp

```bash
# Build image
docker build -t erp-portal-api:latest .

# Chạy container
docker run -d \
  --name erp-portal-api \
  -p 5000:80 \
  -v $(pwd)/data/attachments:/app/Attachments \
  -v $(pwd)/data/logs:/app/Logs \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e FileConfig__PhysicalRootPath=/app/Attachments \
  -e EContractLogConfig__LogPath=/app/Logs/EContract \
  erp-portal-api:latest

# Xem log
docker logs -f erp-portal-api

# Dừng & xoá
docker stop erp-portal-api && docker rm erp-portal-api
```

### 4.2. Dùng docker-compose (khuyến khích)

```bash
# Build + start
docker compose up -d --build

# Theo dõi log
docker compose logs -f erp-portal-api

# Restart nhanh
docker compose restart erp-portal-api

# Dừng & xoá container
docker compose down

# Dừng & xoá luôn volume (CẨN THẬN)
docker compose down -v
```

Truy cập:
- Swagger UI: <http://localhost:5000>
- Swagger JSON: <http://localhost:5000/swagger/v1/swagger.json>

---

## 5. Những lưu ý quan trọng

### 5.1. Đường dẫn Windows → Linux

Trong `Program.cs` và `appsettings.json` đang hard-code:

```csharp
new PhysicalFileProvider(@"D:\Attachments")
```
```json
"PhysicalRootPath": "D:\\Attachments",
"LogPath": "D:\\Logs\\EContract"
```

**Giải pháp** (ưu tiên theo thứ tự):
1. **Tốt nhất:** Sửa `Program.cs` để đọc path từ config thay vì hard-code:
   ```csharp
   var uploadPath = builder.Configuration["FileConfig:PhysicalRootPath"] ?? "/app/Attachments";
   app.UseStaticFiles(new StaticFileOptions
   {
       FileProvider = new PhysicalFileProvider(uploadPath),
       RequestPath = "/uploads",
       ContentTypeProvider = provider
   });
   ```
2. Sau đó override bằng biến môi trường khi chạy Docker (như trong `docker-compose.yml` ở trên).

### 5.2. HTTPS trong container

Project có `app.UseHttpsRedirection()`. Trong môi trường container ta thường:
- Không terminate TLS trong app, mà để **reverse proxy (Nginx / Traefik)** phía trước lo HTTPS.
- Trong Dockerfile chỉ expose port `80`.
- Nếu vẫn muốn HTTPS trực tiếp, cần mount certificate `.pfx` và set `ASPNETCORE_Kestrel__Certificates__Default__Path` + `__Password`.

### 5.3. SQL Server connection

- App kết nối tới SQL Server **ngoài container** (`10.10.111.3`, `103.252.1.235` ...). Máy Docker host phải truy cập được các IP này qua mạng nội bộ/VPN.
- Nếu muốn chạy SQL Server trong container để dev, thêm service `mssql` vào `docker-compose.yml`:
  ```yaml
  mssql:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "YourStrong@Passw0rd"
      MSSQL_PID: Developer
    ports:
      - "1433:1433"
    volumes:
      - mssql-data:/var/opt/mssql
    networks:
      - erp-network
  ```

### 5.4. Secret không bao giờ commit

File `appsettings.json` hiện đang chứa password SQL, JWT secret, SMTP password thật. Khi đưa lên Docker:
- **Không COPY** `appsettings.json` production vào image.
- Đưa `appsettings.json` vào `.dockerignore`, dùng `appsettings.Sample.json` làm template.
- Inject secret qua:
  - Biến môi trường (`-e` / `environment:` trong compose)
  - Docker secrets
  - Vault (HashiCorp Vault / Azure Key Vault)

### 5.5. Kích thước image

Sau khi build, image cuối cùng khoảng **~250 MB**. Nếu muốn nhỏ hơn nữa có thể:
- Đổi base image sang `dotnet/aspnet:8.0-alpine` (~120 MB, nhưng cần test kỹ do Alpine dùng musl libc).
- Publish dạng self-contained + trimmed: `dotnet publish -c Release -r linux-x64 --self-contained -p:PublishTrimmed=true`.

---

## 6. Quy trình đẩy image lên registry

```bash
# Tag image
docker tag erp-portal-api:latest your-registry.com/erp-portal-api:1.0.0
docker tag erp-portal-api:latest your-registry.com/erp-portal-api:latest

# Đăng nhập registry
docker login your-registry.com

# Push
docker push your-registry.com/erp-portal-api:1.0.0
docker push your-registry.com/erp-portal-api:latest
```

---

## 7. Checklist trước khi deploy production

- [ ] Đã đưa `appsettings.json`, `.env`, `*.pfx`, `*.key` vào `.dockerignore`
- [ ] Đã sửa hard-code path `D:\Attachments` → đọc từ config
- [ ] Đã inject secret qua env/secret manager, không hard-code
- [ ] Đã set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Đã đặt reverse proxy (Nginx/Traefik) với HTTPS phía trước
- [ ] Đã cấu hình CORS chặt chẽ thay vì `AllowAll`
- [ ] Đã mount volume cho `Attachments` và `Logs` để dữ liệu không mất khi rebuild
- [ ] Đã verify healthcheck hoạt động (`docker ps` thấy cột STATUS là `healthy`)
- [ ] Đã test restore + run được trên môi trường staging trước

---

## 8. Tham khảo

- [.NET in Docker - Official Docs](https://learn.microsoft.com/dotnet/core/docker/build-container)
- [ASP.NET Core in Docker](https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/)
- [Multi-stage builds](https://docs.docker.com/build/building/multi-stage/)
- [Docker Compose reference](https://docs.docker.com/compose/compose-file/)

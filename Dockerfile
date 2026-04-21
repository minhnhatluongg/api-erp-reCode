#
# Stage 1 : BUILD - dùng SDK 
#

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

#1.1. Copy các file .csproj trước để tận dụng Docker layer cache
# 
COPY ["ERP_Portal_RC/API.ERP_Portal_RC.csproj",                      "ERP_Portal_RC/"]
COPY ["ERP_Portal_RC.Application/ERP_Portal_RC.Application.csproj",  "ERP_Portal_RC.Application/"]
COPY ["ERP_Portal_RC.Infrastructure/ERP_Portal_RC.Infrastructure.csproj", "ERP_Portal_RC.Infrastructure/"]
COPY ["ERP_Portal_RC.Domain/ERP_Portal_RC.Domain.csproj",            "ERP_Portal_RC.Domain/"]

# 1.2. Restore NuGet packages theo project startup (kéo theo tất cả reference)
RUN dotnet restore "ERP_Portal_RC/API.ERP_Portal_RC.csproj"

# 1.3 Copy toàn bộ source code còn lại

COPY . .

# 1.4 Build project ở cấu hình Release
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
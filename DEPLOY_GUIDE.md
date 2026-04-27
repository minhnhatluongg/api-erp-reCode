# Hướng dẫn Deploy api.kknt lên IIS Server

> **Dự án:** api.kknt  
> **Phương thức:** PowerShell Remoting (WinRM)  
> **Ngày tạo:** 24/04/2026  

---

## Mục lục

1. [Tổng quan](#1-tổng-quan)
2. [Cấu trúc file](#2-cấu-trúc-file)
3. [Cấu hình máy Remote (Server IIS)](#3-cấu-hình-máy-remote-server-iis)
4. [Cấu hình máy Local (Máy Dev)](#4-cấu-hình-máy-local-máy-dev)
5. [Cách chạy Deploy](#5-cách-chạy-deploy)
6. [Luồng hoạt động chi tiết](#6-luồng-hoạt-động-chi-tiết)
7. [Cấu hình tùy chỉnh](#7-cấu-hình-tùy-chỉnh)
8. [Xử lý lỗi thường gặp](#8-xử-lý-lỗi-thường-gặp)

---

## 1. Tổng quan

Hệ thống deploy gồm 2 file hoạt động cùng nhau:

| File | Ngôn ngữ | Chức năng |
|------|-----------|-----------|
| `deploy.bat` | Batch | Script chính — build project rồi gọi PowerShell script |
| `deploy-remote.ps1` | PowerShell | Kết nối server qua WinRM, copy file, restart IIS App Pool |

**Quy trình tổng quát:**

```
[Máy Dev] dotnet publish → copy file qua WinRM → [Server] restart IIS App Pool
```

Ưu điểm so với copy thủ công:
- **Không cần mở RDP** để copy file
- **Không cần share folder (SMB)** — chỉ dùng WinRM
- **Tự động stop/start IIS App Pool** — tránh file bị lock
- **Loại trừ file cấu hình** — không ghi đè `appsettings.Production.json`, `web.config`

---

## 2. Cấu trúc file

```
d:\Code\api.kknt\
├── deploy.bat              ← Double-click để chạy
├── deploy-remote.ps1       ← Script PowerShell (được gọi bởi deploy.bat)
├── _publish_output\        ← Thư mục output sau khi build (tự tạo)
└── api.kknt\
    └── api.kknt.API.csproj ← Project chính
```

---

## 3. Cấu hình máy Remote (Server IIS)

> ⚠️ **Tất cả lệnh dưới đây chạy trên SERVER, trong PowerShell Administrator**

### Bước 3.1 — Bật PowerShell Remoting

```powershell
Enable-PSRemoting -Force
```

### Bước 3.2 — Cho phép kết nối từ máy không cùng domain

```powershell
winrm set winrm/config/service '@{AllowUnencrypted="true"}'
winrm set winrm/config/service/auth '@{Basic="true"}'
```

### Bước 3.3 — Mở Firewall cho WinRM (nếu chưa mở)

```powershell
# Kiểm tra rule đã có chưa
Get-NetFirewallRule -Name "WINRM-HTTP-In-TCP" -ErrorAction SilentlyContinue

# Nếu chưa có, tạo mới
New-NetFirewallRule -Name "WINRM-HTTP-In-TCP" `
    -DisplayName "WinRM HTTP" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 5985 `
    -Action Allow `
    -Profile Any
```

### Bước 3.4 — Kiểm tra WinRM đang chạy

```powershell
# Kiểm tra service
Get-Service WinRM

# Kiểm tra listener
winrm enumerate winrm/config/listener
```

Kết quả mong đợi: Service ở trạng thái **Running**, có listener trên port **5985**.

### Bước 3.5 — Đảm bảo IIS WebAdministration module có sẵn

```powershell
Import-Module WebAdministration
Get-WebAppPoolState -Name "api-kknt.win-tech.vn"
```

---

## 4. Cấu hình máy Local (Máy Dev)

> ⚠️ **Tất cả lệnh dưới đây chạy trên MÁY DEV, trong PowerShell Administrator**

### Bước 4.1 — Bật WinRM service trên máy local

```powershell
winrm quickconfig -Force
```

> Lệnh này sẽ:
> - Khởi động WinRM service
> - Đặt WinRM service auto-start
> - Tạo firewall rule cho WinRM

### Bước 4.2 — Thêm server vào TrustedHosts

```powershell
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "10.10.202.1" -Force
```

Nếu cần thêm **nhiều server**:

```powershell
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "10.10.202.1,10.10.202.2" -Force
```

### Bước 4.3 — Kiểm tra kết nối

```powershell
# Test kết nối không cần credential (cùng domain)
Test-WSMan -ComputerName 10.10.202.1

# Hoặc test với credential (khác domain)
$cred = Get-Credential
Test-WSMan -ComputerName 10.10.202.1 -Credential $cred -Authentication Negotiate
```

### Bước 4.4 — Đảm bảo .NET SDK đã cài

```powershell
dotnet --version
```

Cần **.NET 8 SDK** trở lên.

---

## 5. Cách chạy Deploy

### Cách 1: Double-click

Mở thư mục `d:\Code\api.kknt\` → double-click file **`deploy.bat`**

### Cách 2: Chạy từ Terminal

```cmd
:: Deploy lên server mặc định (10.10.202.1)
deploy.bat

:: Deploy lên server khác
deploy.bat 10.10.202.2
```

### Cách 3: Chạy từ PowerShell

```powershell
cmd /c "d:\Code\api.kknt\deploy.bat"
```

> **Lưu ý:** Lần đầu chạy, nếu máy dev và server không cùng domain, script sẽ hỏi **credential** (user/pass của server). Nhập dạng:
> - User: `Administrator` hoặc `SERVERNAME\Administrator`
> - Password: mật khẩu của account đó trên server

---

## 6. Luồng hoạt động chi tiết

```
deploy.bat
│
├─ [Step 1/3] dotnet publish
│   ├── Build project api.kknt.API.csproj (Release mode)
│   └── Output → _publish_output\
│
└─ [Step 2/3] Gọi deploy-remote.ps1
    │
    ├─ [1/4] Test WinRM Connection
    │   ├── Thử kết nối không credential
    │   └── Nếu fail → hỏi credential → thử lại
    │
    ├─ [2/4] Stop IIS App Pool
    │   ├── Stop-WebAppPool "api-kknt.win-tech.vn"
    │   └── Đợi tối đa 10 giây cho pool tắt hẳn
    │
    ├─ [3/4] Copy files qua WinRM
    │   ├── Quét tất cả file trong _publish_output\
    │   ├── Loại trừ: appsettings.Production.json, web.config, Logs\
    │   ├── Tạo thư mục trên server nếu chưa có
    │   └── Copy từng file với progress bar
    │
    └─ [4/4] Start IIS App Pool
        ├── Start-WebAppPool "api-kknt.win-tech.vn"
        └── Xác nhận trạng thái: Started
```

---

## 7. Cấu hình tùy chỉnh

### 7.1 Thay đổi server mặc định

Mở `deploy.bat`, sửa dòng:

```batch
set "SERVER=10.10.202.1"
```

### 7.2 Thay đổi đường dẫn trên server

Mở `deploy.bat`, sửa dòng:

```batch
set "REMOTE_PATH=C:\DiskD\IIS WEB\api-kknt.win-tech.vn"
set "APP_POOL=api-kknt.win-tech.vn"
```

### 7.3 Thay đổi file/folder loại trừ (không ghi đè khi deploy)

Mở `deploy-remote.ps1`, sửa phần đầu:

```powershell
# File sẽ KHÔNG bị ghi đè trên server
$excludeFiles = @('appsettings.Production.json', 'web.config')

# Folder sẽ KHÔNG bị ghi đè trên server
$excludeDirs  = @('Logs')
```

**Thêm file loại trừ:**

```powershell
$excludeFiles = @('appsettings.Production.json', 'web.config', 'nlog.config')
```

**Thêm folder loại trừ:**

```powershell
$excludeDirs = @('Logs', 'Uploads', 'TempData')
```

---

## 8. Xử lý lỗi thường gặp

### ❌ Lỗi: Cửa sổ chớp rồi tắt khi double-click

**Nguyên nhân:** File `.bat` chứa ký tự Unicode (tiếng Việt có dấu, ký tự đặc biệt). `cmd.exe` không thể parse UTF-8 multi-byte.

**Giải pháp:** Đảm bảo file `.bat` chỉ chứa ký tự ASCII thuần (không dấu tiếng Việt, không ký tự Unicode).

---

### ❌ Lỗi: `The client cannot connect to the destination specified in the request`

```
Set-Item : The client cannot connect to the destination specified in the request.
Verify that the service on the destination is running...
```

**Nguyên nhân:** WinRM service trên **máy local** chưa bật.

**Giải pháp:**

```powershell
# Chạy trên máy local (PowerShell Administrator)
winrm quickconfig -Force
```

---

### ❌ Lỗi: `WinRM cannot process the request... TrustedHosts`

```
The WinRM client cannot process the request. If the authentication scheme is
different from Kerberos, or if the client computer is not joined to a domain,
then HTTPS transport must be used or the destination machine must be added to
the TrustedHosts configuration setting.
```

**Nguyên nhân:** Server chưa được thêm vào TrustedHosts trên máy local.

**Giải pháp:**

```powershell
# Chạy trên máy local (PowerShell Administrator)
Set-Item WSMan:\localhost\Client\TrustedHosts -Value "10.10.202.1" -Force
```

---

### ❌ Lỗi: `Access is denied` khi kết nối

**Nguyên nhân:** Sai credential hoặc account không có quyền Administrator trên server.

**Giải pháp:** Đảm bảo dùng account **Administrator** hoặc account trong group Administrators trên server.

---

### ❌ Lỗi: `Cannot bind argument to parameter 'LocalPath' because it is an empty string`

**Nguyên nhân:** Biến `%LOCAL_PUBLISH%` không được set do file `.bat` bị lỗi parse (thường do ký tự Unicode).

**Giải pháp:** Đảm bảo file `deploy.bat` là ASCII thuần, không có BOM.

---

### ❌ Lỗi: `Publish that bai!`

**Nguyên nhân:** `dotnet publish` thất bại — có thể do lỗi code hoặc thiếu dependency.

**Giải pháp:**

```cmd
:: Thử restore trước rồi publish
dotnet restore "d:\Code\api.kknt\api.kknt\api.kknt.API.csproj"
dotnet publish "d:\Code\api.kknt\api.kknt\api.kknt.API.csproj" -c Release
```

---

## Checklist Setup Nhanh

### Máy Remote (Server) ✅

- [ ] Mở PowerShell Administrator
- [ ] Chạy `Enable-PSRemoting -Force`
- [ ] Chạy `winrm set winrm/config/service '@{AllowUnencrypted="true"}'`
- [ ] Chạy `winrm set winrm/config/service/auth '@{Basic="true"}'`
- [ ] Kiểm tra firewall port 5985 đã mở
- [ ] Kiểm tra IIS App Pool `api-kknt.win-tech.vn` tồn tại

### Máy Local (Dev) ✅

- [ ] Mở PowerShell Administrator
- [ ] Chạy `winrm quickconfig -Force`
- [ ] Chạy `Set-Item WSMan:\localhost\Client\TrustedHosts -Value "10.10.202.1" -Force`
- [ ] Kiểm tra `dotnet --version` (cần .NET 8+)
- [ ] Test: `Test-WSMan -ComputerName 10.10.202.1`

---

> **Lưu ý bảo mật:** WinRM mặc định dùng HTTP (port 5985, không mã hóa). Nếu deploy qua mạng không tin cậy, nên cấu hình HTTPS (port 5986) với SSL certificate.

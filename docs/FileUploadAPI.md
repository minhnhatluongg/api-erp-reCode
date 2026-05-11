# API File Upload — Tài liệu cho Frontend

> Base path: `/api/files`
> Authentication: **Bearer JWT** (`Authorization: Bearer <token>`)
> Tất cả endpoint dưới đây đều yêu cầu đăng nhập, trừ `GET /list` và `GET /listImage` đang dùng nội bộ.

---

## 1. Tổng quan & quy ước

### 1.1 OID hợp đồng
- OID gốc có dạng `000642/260415:104748280`.
- Khi truyền qua URL, FE **không cần encode** đặc biệt — BE sẽ tự `Replace("/", "_").Replace(":", "_")`.
- Ví dụ folder thực tế trên đĩa: `Attachments/{year}/{month}/{UserCode}_{OID_clean}/`.

### 1.2 Cấu trúc thư mục lưu trữ
```
{PhysicalRootPath}/
└── 2026/
    └── 05/
        └── HAUNP_000642_260415_104748280/
            ├── metadata.json
            ├── {guid1}_baocao.pdf
            └── {guid2}_hopdong.docx
```
- Folder name = `{UserCode}_{OID_clean}` → giúp BE biết file thuộc về user nào (filter `my-files`).
- `metadata.json` lưu danh sách `ContractFileMetadata` của riêng folder đó.

### 1.3 Response wrapper chung
Mọi endpoint trả về `ApiResponse<T>`:
```json
{
  "success": true,
  "message": "Thành công",
  "data": { ... },
  "statusCode": 200,
  "errors": null,
  "timestamp": "2026-05-11T10:00:00Z"
}
```

### 1.4 Model `ContractFileMetadata`
```ts
interface ContractFileMetadata {
  oid: string;            // OID gốc, vd "000642/260415:104748280"
  fileName: string;       // tên trên đĩa: "{guid}_{safeName}"
  originalName: string;   // tên gốc user upload
  url: string;            // URL public để preview/download
  relativePath: string;   // "2026/05/HAUNP_000642_.../file.pdf"
  sizeBytes: number;
  extension: string;      // ".pdf"
  uploadedAt: string;     // ISO datetime
  uploadedBy: string;     // UserCode
}
```

---

## 2. POST `/api/files/upload` — Upload file (≤ 5MB / file)

Upload 1 hoặc nhiều file ≤ `ChunkSizeBytes` (mặc định 5MB). File lớn hơn dùng API chunk.

### Request
- **Method**: `POST`
- **Auth**: Bearer JWT
- **Query**:
  - `oid` (string, required) — OID hợp đồng
- **Body**: `multipart/form-data`
  - `files` (file[], required) — danh sách file

### Response 200 OK
```json
{
  "success": true,
  "message": "Upload thành công 2 file.",
  "data": [
    {
      "oid": "000642/260415:104748280",
      "fileName": "abc123_baocao.pdf",
      "originalName": "báo cáo.pdf",
      "url": "https://api-erprc.win-tech.vn/uploads/2026/05/HAUNP_000642.../abc123_baocao.pdf",
      "relativePath": "2026/05/HAUNP_000642_260415_104748280/abc123_baocao.pdf",
      "sizeBytes": 245678,
      "extension": ".pdf",
      "uploadedAt": "2026-05-11T10:23:45",
      "uploadedBy": "HAUNP"
    }
  ]
}
```

### Response 400 — Lỗi
- `"Thiếu OID."`
- `"Không có file nào."`
- `"{file}: File quá lớn, vui lòng dùng API upload-chunk."`
- `"{file}: {validation error từ IFileValidationService}"`

### Response 401
- Token không hợp lệ hoặc thiếu claim `UserCode`.

### Curl mẫu
```bash
curl -X POST "https://api-erprc.win-tech.vn/api/files/upload?oid=000642/260415:104748280" \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@./baocao.pdf" \
  -F "files=@./hopdong.docx"
```

---

## 3. Chunked Upload (file > 5MB)

### 3.1 POST `/api/files/upload-chunk`
- **Headers**:
  - `X-Session-Id` — GUID FE tự sinh, dùng chung cho mọi chunk của 1 file.
  - `X-Chunk-Index` — số thứ tự (0-based).
  - `X-Total-Chunks` — tổng số chunk.
  - `X-File-Name` — tên file gốc.
- **Query**: `oid`
- **Body**: 1 chunk (`multipart/form-data` field name bất kỳ).

### 3.2 POST `/api/files/merge-chunks`
- **Body** (JSON):
  ```json
  {
    "sessionId": "guid-...",
    "fileName": "video.mp4",
    "oid": "000642/260415:104748280",
    "totalChunks": 12
  }
  ```
- **Response**: `data` = URL file đã ghép xong.

> Lưu ý: hiện tại endpoint `merge-chunks` chưa lưu `UploadedBy` vào metadata cho file chunked. Khi cần đồng bộ với pattern `{userCode}_{oid}`, FE nên dùng API `/upload` cho file nhỏ và phối hợp với BE để mở rộng cho chunked.

---

## 4. GET `/api/files/contract-files` — Xem file của 1 hợp đồng

User xem toàn bộ file đính kèm trong hợp đồng.

### Request
- **Query**: `oid` (required)
- **Auth**: Bearer JWT

### Response 200
```json
{
  "success": true,
  "message": "Tìm thấy 3 file đính kèm.",
  "data": {
    "oid": "000642/260415:104748280",
    "totalFiles": 3,
    "files": [
      { ...ContractFileMetadata },
      { ...ContractFileMetadata }
    ]
  }
}
```

### Curl
```bash
curl "https://api-erprc.win-tech.vn/api/files/contract-files?oid=000642/260415:104748280" \
  -H "Authorization: Bearer $TOKEN"
```

---

## 5. GET `/api/files/my-files` — Danh sách tất cả file của user

Trả danh sách chi tiết tất cả file user đã upload (Attachments + KyThuatMau).

### Request
- **Query**: `year` (optional, vd `2026`)
- **Auth**: Bearer JWT

### Response 200
```json
{
  "success": true,
  "message": "User HAUNP: 12 file đính kèm, 3 file mẫu.",
  "data": {
    "oid": "HAUNP",
    "attachments": [
      { "fileName": "...", "originalName": "...", "url": "...", "extension": ".pdf",
        "sizeBytes": 12345, "uploadedAt": "2026-05-10T...", "category": "attach" }
    ],
    "templates": [
      { "fileName": "...", "category": "template", ... }
    ],
    "totalAttachments": 12,
    "totalTemplates": 3,
    "totalAll": 15
  }
}
```

---

## 6. GET `/api/files/my-files/summary` — Dashboard tổng quan (MỚI)

Báo cáo tổng hợp dành cho trang "My Files" dashboard.

### Request
- **Query**: `year` (optional)
- **Auth**: Bearer JWT

### Response 200
```json
{
  "success": true,
  "message": "User HAUNP: 28 file, 7 hợp đồng, 142.5 MB.",
  "data": {
    "userCode": "HAUNP",
    "totalFiles": 28,
    "totalSizeBytes": 149422080,
    "totalSizeFormatted": "142.5 MB",
    "totalContracts": 7,
    "byContract": [
      {
        "oid": "000642/260415:104748280",
        "fileCount": 5,
        "sizeBytes": 25000000,
        "lastUploadedAt": "2026-05-10T12:34:56"
      }
    ],
    "byExtension": [
      { "extension": ".pdf", "fileCount": 15, "sizeBytes": 80000000 },
      { "extension": ".docx", "fileCount": 10, "sizeBytes": 50000000 }
    ],
    "byMonth": [
      { "month": "2026-05", "fileCount": 8, "sizeBytes": 30000000 },
      { "month": "2026-04", "fileCount": 20, "sizeBytes": 119422080 }
    ],
    "recentFiles": [
      { ...ContractFileMetadata }  // tối đa 10 file mới nhất
    ]
  }
}
```

### Use case FE
- `totalFiles / totalSizeFormatted` → KPI card trên cùng dashboard.
- `byContract` → bảng/list "Hợp đồng có file của tôi".
- `byExtension` → pie chart "Phân bố loại file".
- `byMonth` → bar chart "Hoạt động upload theo tháng".
- `recentFiles` → widget "Upload gần đây".

---

## 7. POST `/api/files/rebuild-metadata` — (Admin/Dev) đồng bộ metadata

Quét lại thư mục thực tế, bổ sung file vào `metadata.json` nếu thiếu.

### Request
- **Query**: `oid`, `year`, `month`

> Endpoint này dành cho admin khi data bị lệch, không cho FE thường gọi.

---

## 8. Các mã lỗi thường gặp

| HTTP | Nguyên nhân | Xử lý FE |
|------|-------------|----------|
| 400  | Thiếu OID / Thiếu file / file lớn quá ChunkSize | Hiện toast lỗi từ `message` |
| 401  | Chưa đăng nhập / token hết hạn | Redirect login |
| 500  | Lỗi merge chunks / lỗi IO | Báo "Lỗi hệ thống, thử lại" |

---

## 9. Cấu hình BE liên quan (tham khảo)

```json
"FileUpload": {
  "BaseUrl": "https://api-erprc.win-tech.vn",
  "PhysicalRootPath": "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\Attachments",
  "KyThuatMauPath":   "D:\\IIS WEB\\api-erprc.win-tech.vn\\wwwroot\\KyThuatMau",
  "MaxFileSizeBytes": 52428800,
  "ChunkSizeBytes":   5242880,
  "AllowedExtensions": [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png"]
}
```

---

## 10. Checklist FE tích hợp

1. [ ] Đăng nhập lấy JWT → lưu vào localStorage/cookie.
2. [ ] Khi user mở hợp đồng → gọi `GET /contract-files?oid={oid}` để render danh sách file đã có.
3. [ ] Upload form → `POST /upload?oid={oid}` (multipart), hiện progress bar.
4. [ ] Trang dashboard cá nhân → `GET /my-files/summary` để render KPI và biểu đồ.
5. [ ] Trang "All my files" → `GET /my-files?year=2026` để hiện list chi tiết.
6. [ ] Xử lý 401 → tự động refresh token / redirect login.

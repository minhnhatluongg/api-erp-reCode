# ERP_Portal_RC.Application.Tests

Unit test cho tầng **Application/Services** của hệ thống ERP_Portal_RC.

## Stack
- **xUnit** – test framework
- **Moq** – mocking dependency
- **FluentAssertions** – assertion có ngữ pháp tiếng Anh tự nhiên
- **coverlet** – đo code coverage

## Cấu trúc
```
ERP_Portal_RC.Application.Tests/
├── ERP_Portal_RC.Application.Tests.csproj
└── Services/
    ├── AccountServiceTests.cs   # test method thuần (no mock needed)
    └── AuthServiceTests.cs      # test với nhiều mock dependency
```

## Chạy test (tại thư mục gốc solution)
```bash
# Restore packages
dotnet restore

# Build & chạy toàn bộ test
dotnet test

# Chạy test có lọc theo tên
dotnet test --filter "FullyQualifiedName~AuthServiceTests"

# Chạy test + sinh report coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Convention đặt tên test
```
[MethodUnderTest]_[Scenario]_[ExpectedResult]
```
Ví dụ:
- `ChangePasswordAsync_WhenNewPasswordEqualsOldPassword_ShouldReturnFail`
- `ParseApiLoginString_WhenInputIsEmpty_ShouldReturnEmptyDictionary`

## Mẫu AAA
```csharp
[Fact]
public void Method_Scenario_Expected()
{
    // Arrange  – chuẩn bị input + setup mock
    // Act      – gọi method cần test
    // Assert   – kiểm tra kết quả
}
```

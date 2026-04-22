using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Application.Mappings;
using ERP_Portal_RC.Application.Services;
using ERP_Portal_RC.Domain.Common.Logging;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using ERP_Portal_RC.Domain.Interfaces.Accounts_payable;
using ERP_Portal_RC.Infrastructure.Repositories;
using ERP_Portal_RC.Infrastructure.Repositories.Accounts_payable;
using Infrastructure.Repositories;
using Interface.ReleaseInvoice.Repo;
using Interface.ReleaseInvoice.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<TokenCleanupWorker>();
builder.Services.AddSingleton<EContractFileLogger>();
builder.Services.AddKeyedScoped<EContractFileLogger>("InvoiceLogger", (sp, _) =>
    new EContractFileLogger(
        configuration: builder.Configuration,
        filePrefix: "Invoice_craft"
    ));
//Config upload file
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".xslt"] = "text/xml";
provider.Mappings[".json"] = "application/json";

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".ERPPortal.Session";
});

// Đăng ký DbConnectionFactory
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
// Gộp tất cả vào một lần gọi duy nhất
builder.Services.AddAutoMapper(new[] {
    typeof(AccountMappingProfile).Assembly,
    typeof(AuthMappingProfile).Assembly,
    typeof(MenuMappingProfile).Assembly,
    typeof(TechnicalMappingProfile).Assembly,
    typeof(ServiceTypeMappingProfile).Assembly,
    typeof(TvanRenewalProfile).Assembly
});
//HTTPS config

// HttpClient cho Invoice module
builder.Services.AddHttpClient("WinInvoiceClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WinInvoice:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));

    var credentials = Convert.ToBase64String(
        Encoding.UTF8.GetBytes(
            $"{builder.Configuration["WinInvoice:ClientId"]}:{builder.Configuration["WinInvoice:ClientSecret"]}"));
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Basic", credentials);
});

builder.Services.AddHttpClient("ERPPortalClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ERPPortal:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("HRAccountClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["HRAccountApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.Add("X-API-Key", builder.Configuration["HRAccountApi:ApiKey"]);
});

// Đăng ký cấu hình FileConfig
builder.Services.Configure<FileConfig>(builder.Configuration.GetSection("FileConfig"));
// Đăng ký Application Services
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ISalesHierarchyService, SalesHierarchyService>();
builder.Services.AddScoped<IDSignaturesService, DSignaturesService>();
builder.Services.AddScoped<IEcontractService, EcontractService>();
builder.Services.AddScoped<IRegistrationCodeService, RegistrationCodeService>();
builder.Services.AddScoped<IInvoicePreviewService, InvoicePreviewService>();
builder.Services.AddScoped<IContractSignService, ContractSignService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IIntegrationService, IntegrationService>();
builder.Services.AddScoped<ICapTaiKhoanService, CreateAccountService>();
builder.Services.AddScoped<ISignHSMService, SignHSMService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IServiceTypeService, ServiceTypeService>();
builder.Services.AddScoped<ITvanRenewalService, TvanRenewalService>();

// Đăng ký Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<ICustomStore, CustomStore>();
builder.Services.AddScoped<ISalesHierarchyRepository, SalesHierarchyRepository>();
builder.Services.AddScoped<IDSignaturesRepository, DSignaturesRepository>();
builder.Services.AddScoped<IEContractRepository, EContractRepository>();
builder.Services.AddScoped<ITechnicalUserRepository, TechnicalUserRepository>();
builder.Services.AddScoped<IRuleRepository,RuleRepository>();
builder.Services.AddScoped<IXmlDataRepository, XmlDataRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IContractCheckRepository, ContractCheckRepository>();
builder.Services.AddScoped<IContractSignRepository, ContractSignRepository>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IEvatRepository, EvatRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<ICreateAccountRepository, CapTaiKhoanRepository>();
builder.Services.AddScoped<ISignHSMRepository, SignHSMRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IServiceTypeRepository, ServiceTypeRepository>();
builder.Services.AddScoped<ITvanRenewalRepository, TvanRenewalRepository>();

// Configure Identity (cần cấu hình DbContext riêng cho Identity nếu sử dụng)
// Tạm thời comment để không bị lỗi nếu chưa có DbContext
// builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
// {
//     options.Password.RequireDigit = false;
//     options.Password.RequiredLength = 6;
//     options.Password.RequireNonAlphanumeric = false;
//     options.Password.RequireUppercase = false;
//     options.Password.RequireLowercase = false;
// })
// .AddEntityFrameworkStores<ApplicationDbContext>()
// .AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"))),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
    };
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ERP Portal API",
        Version = "v1",
        Description = "API cho hệ thống ERP Portal",
        Contact = new OpenApiContact
        {
            Name = "ERP Team",
            Email = "cusocisme@gmail.com or minhnhatluongwork@gmail.com"
        }
    });
    options.EnableAnnotations();

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);

    // Thêm authorization header
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        BearerFormat = "JWT",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Enable XML comments if available
    options.EnableAnnotations();
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

//Enable Files
var uploadPath = builder.Configuration["FileConfig:PhysicalRootPath"] ?? "/app/Attachments";
// Tự tạo folder nếu chưa tồn tại (tránh crash khi start container lần đầu)
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads",
    ContentTypeProvider = provider
});
// Enable Session
app.UseSession();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP Portal API V1");
        options.RoutePrefix = "";
        options.DocumentTitle = "ERP Portal API Documentation";
        options.DefaultModelsExpandDepth(-1);
        options.DefaultModelExpandDepth(-1);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
    });
}
else
{
    // Production cũng có thể bật Swagger nếu cần
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ERP Portal API V1");
        options.RoutePrefix = "";
    });
}

if (!app.Environment.IsEnvironment("Docker") &&
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");


// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
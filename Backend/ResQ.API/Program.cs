using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResQ.API.Data;
using ResQ.API.Models.Settings;
using FluentValidation;
using FluentValidation.AspNetCore;
using ResQ.API.Middleware;
using ResQ.API.Repositories.Admin;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.MercadoPago;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Admin;
using ResQ.API.Services.Auth;
using ResQ.API.Services.Reporting;
using ResQ.API.Services.Reporting.Implementations;
using ResQ.API.Services.Catalog;
using ResQ.API.Services.Consumers;
using ResQ.API.Services.Encryption;
using ResQ.API.Services.Jwt;
using Hangfire;
using Hangfire.PostgreSql;
using ResQ.API.Services.MercadoPago;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Orders;
using ResQ.API.Services.Password;
using ResQ.API.Services.Products;
using ResQ.API.Services.Reviews;
using ResQ.API.Services.Storage;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// ── QuestPDF licence (free Community tier — required before any PDF generation) ─
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ── Controllers + FluentValidation ───────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ── OpenAPI + Swagger UI ──────────────────────────────────────────────────────
builder.Services.AddOpenApi(options =>
{
    // Swagger UI (Swashbuckle) only renders up to 3.0.x — .NET 10 emits 3.1 by default
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;

    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title       = "ResQ API";
        document.Info.Version     = "v1";
        document.Info.Description = "API de ResQ — plataforma de rescate alimentario";

        document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Type         = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT",
                Description  = "Ingresá el access token obtenido en /api/auth/login"
            };

        document.Security ??= [];
        document.Security.Add(new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    });
});

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ResQDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── MinIO ─────────────────────────────────────────────────────────────────────
builder.Services.Configure<ResQ.API.Models.Settings.MinioSettings>(
    builder.Configuration.GetSection("Minio"));

var minioSettings = builder.Configuration.GetSection("Minio")
    .Get<ResQ.API.Models.Settings.MinioSettings>()!;

builder.Services.AddMinio(cfg => cfg
    .WithEndpoint(minioSettings.Endpoint)
    .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
    .WithSSL(minioSettings.UseSSL));

// ── JWT Settings ──────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// ── Mercado Pago Settings ─────────────────────────────────────────────────────
builder.Services.Configure<MpSettings>(builder.Configuration.GetSection("MercadoPago"));

// ── Google Settings ───────────────────────────────────────────────────────────
builder.Services.Configure<ResQ.API.Models.Settings.GoogleSettings>(
    builder.Configuration.GetSection("Google"));

// ── SMTP Settings ─────────────────────────────────────────────────────────────
builder.Services.Configure<ResQ.API.Models.Settings.SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

// ── Hangfire ──────────────────────────────────────────────────────────────────
var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(hangfireConn)));
builder.Services.AddHangfireServer();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey        = new SymmetricSecurityKey(
                                          Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateIssuer   = true,
            ValidIssuer      = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience    = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero
        };
    });

// ── Encryption ────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEncryptionService, AesEncryptionService>();

// ── Mercado Pago HttpClient ───────────────────────────────────────────────────
builder.Services.AddHttpClient<IMercadoPagoHttpClient, MercadoPagoHttpClient>(client =>
{
    client.BaseAddress = new Uri("https://api.mercadopago.com");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IConsumerProfileRepository, ConsumerProfileRepository>();
builder.Services.AddScoped<IMerchantProfileRepository, MerchantProfileRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IMerchantCategoryRepository, MerchantCategoryRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IMerchantMpCredentialRepository, MerchantMpCredentialRepository>();
builder.Services.AddScoped<IMpWebhookEventRepository, MpWebhookEventRepository>();
builder.Services.AddScoped<IAdminRepository, AdminRepository>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<ResQ.API.Services.Email.IEmailService, ResQ.API.Services.Email.SmtpEmailService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IConsumerService, ConsumerService>();
builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IMercadoPagoOAuthService, MercadoPagoOAuthService>();
builder.Services.AddScoped<IMpWebhookIngestionService, MpWebhookIngestionService>();
builder.Services.AddScoped<IMpWebhookProcessorService, MpWebhookProcessorService>();
builder.Services.AddScoped<IMpTokenRefreshJob, MpTokenRefreshJob>();
builder.Services.AddScoped<IAdminService, AdminService>();

// ── Reporting (PDF via QuestPDF, Excel via ClosedXML) ──────────────────────────
builder.Services.AddScoped<IReportExporter, PdfReportExporter>();
builder.Services.AddScoped<IReportExporter, ExcelReportExporter>();
builder.Services.AddScoped<IReportFileFactory, ReportFileFactory>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// Apply pending migrations on startup so the Docker DB schema is always in sync.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResQDbContext>();
    db.Database.Migrate();
    DatabaseSeeder.Seed(db);

    // Idempotently provision the platform admin (runs even on already-seeded databases).
    var adminEmail    = builder.Configuration["Admin:Email"]    ?? "admin@resq.com";
    var adminPassword = builder.Configuration["Admin:Password"] ?? "ResQ1234!";
    DatabaseSeeder.EnsureAdmin(db, adminEmail, adminPassword);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ResQ API v1");
        options.DocumentTitle = "ResQ API — Swagger";
    });
    app.UseHangfireDashboard("/hangfire");
}

RecurringJob.AddOrUpdate<IMpTokenRefreshJob>(
    "mp-token-refresh-daily",
    job => job.ExecuteAsync(),
    Cron.Daily);

app.UseMiddleware<ExceptionHandlingMiddleware>();

var runningInContainer = bool.TryParse(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer)
    && inContainer;

if (!runningInContainer)
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResQ.API.Data;
using ResQ.API.Models.Settings;
using FluentValidation;
using FluentValidation.AspNetCore;
using ResQ.API.Middleware;
using ResQ.API.Repositories.Auth;
using ResQ.API.Repositories.Catalog;
using ResQ.API.Repositories.Orders;
using ResQ.API.Repositories.Reviews;
using ResQ.API.Services.Auth;
using ResQ.API.Services.Catalog;
using ResQ.API.Services.Consumers;
using ResQ.API.Services.Jwt;
using ResQ.API.Services.Merchants;
using ResQ.API.Services.Orders;
using ResQ.API.Services.Password;
using ResQ.API.Services.Products;

var builder = WebApplication.CreateBuilder(args);

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

// ── JWT Settings ──────────────────────────────────────────────────────────────
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

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

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IMerchantService, MerchantService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IConsumerService, ConsumerService>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// Apply pending migrations on startup so the Docker DB schema is always in sync.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ResQDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ResQ API v1");
        options.DocumentTitle = "ResQ API — Swagger";
    });
}

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

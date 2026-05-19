using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ResQ.API.Data;
using ResQ.API.Data.UnitOfWork;
using ResQ.API.Models.Settings;
using ResQ.API.Services.Auth;
using ResQ.API.Services.Jwt;
using ResQ.API.Services.Password;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & OpenAPI ────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ── Database ─────────────────────────────────────────────────────────────────
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

// ── Dependency Injection ─────────────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ── Pipeline ──────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

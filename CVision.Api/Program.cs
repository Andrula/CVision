using System.Text;
using CVision.Api.Configuration;
using CVision.Api.Data;
using CVision.Api.Data.Models;
using CVision.Api.Services.Implementations;
using CVision.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/cvision-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting CVision API...");
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var AllowFrontendCommunication = "_AllowFrontendCommunication";

    // Add services to the container.
    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<ICandidateService, CandidateService>();
    builder.Services.AddScoped<IJobService, JobService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IPythonCvParserService, PythonCVParserService>();

    builder.Services.Configure<FileStorageSettings>(
        builder.Configuration.GetSection("FileStorage"));
    builder.Services.Configure<CvParserSettings>(
        builder.Configuration.GetSection("CvParser"));
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection("Cors"));
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection("JwtSettings"));

    // Database Context
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Identity Configuration
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // User settings
        options.User.RequireUniqueEmail = true;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

    builder.Services.AddAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(name: AllowFrontendCommunication,
            policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
    });

app.MapControllers();

app.Run();

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

    builder.Services.AddScoped<IFileStorageService, FileStorageService>();
    builder.Services.AddScoped<ICandidateService, CandidateService>();
    builder.Services.AddScoped<IJobService, JobService>();

    builder.Services.Configure<FileStorageSettings>(
        builder.Configuration.GetSection("FileStorage"));
    builder.Services.Configure<CvParserSettings>(
        builder.Configuration.GetSection("CvParser"));
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection("Cors"));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();

    // HTTP client for parser
    builder.Services.AddHttpClient<IPythonCvParserService, PythonCVParserService>(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(5);
    });

    // DB
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors(AllowFrontendCommunication);
    app.UseHttpsRedirection();
    
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    
    app.MapControllers();

    Log.Information("CVision API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
using CVision.Api.Configuration;
using CVision.Api.Services.Implementations;
using CVision.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var AllowFrontendCommunication = "_AllowFrontendCommunication";

// Add services to the container.
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<ICandidateService, CandidateService>();
builder.Services.AddScoped<IJobService, JobService>();

builder.Services.Configure<FileStorageSettings>(
    builder.Configuration.GetSection("FileStorage"));
builder.Services.Configure<CvParserSettings>(
    builder.Configuration.GetSection("CvParser"));
builder.Services.Configure<CorsSettings>(
    builder.Configuration.GetSection("Cors"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient<IPythonCvParserService, PythonCVParserService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapControllers();
}

app.UseCors(AllowFrontendCommunication);
app.UseHttpsRedirection();

app.Run();

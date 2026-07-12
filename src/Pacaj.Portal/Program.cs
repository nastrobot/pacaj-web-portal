using Pacaj.Portal.Services;

var builder = WebApplication.CreateBuilder(args);

// Register controllers and OpenAPI Swagger documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Pacaj Digital Web Operations API",
        Version = "v1",
        Description = "Automated uptime, response latency, and TLS/SSL certificate monitoring engine for client web applications."
    });
});

// Configure resilient HTTP client for live endpoint health probing
builder.Services.AddHttpClient("SiteMonitorClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "PacajPortal-HealthMonitor/2.0 (+https://pacaj.com)");
});

// Register Core Domain Services
builder.Services.AddSingleton<ISiteMonitoringService, SiteMonitoringService>();

// Configure CORS for modern web portals
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClientDashboard", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pacaj Web Portal API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI directly at application root
    });
}

app.UseCors("AllowClientDashboard");
app.UseAuthorization();
app.MapControllers();

app.Run();

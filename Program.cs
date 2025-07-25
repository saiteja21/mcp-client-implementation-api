using EnterpriseMcpIntegration.Services;
using EnterpriseMcpIntegration.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Enterprise MCP Integration API",
        Version = "v1",
        Description = "Enterprise API for Microsoft Documentation Search using Model Context Protocol",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Enterprise Development Team",
            Email = "dev-team@company.com"
        }
    });
    
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Register our enterprise MCP services
builder.Services.AddScoped<IMcpClientService, McpClientService>();
builder.Services.AddScoped<IMicrosoftDocsService, MicrosoftDocsService>();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    
    // Set logging levels based on environment
    if (builder.Environment.IsDevelopment())
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
});

// Add CORS if needed for frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000") // Add your frontend URLs
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Enterprise MCP Integration API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowSpecificOrigins");

// Add request/response logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Incoming request: {Method} {Path}",
        context.Request.Method, context.Request.Path);
    
    await next();
    
    logger.LogInformation("Completed request: {Method} {Path} -> {StatusCode}",
        context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

// Map controllers
app.MapControllers();

// Add a simple root endpoint for API discovery
app.MapGet("/", () => new
{
    service = "Enterprise MCP Integration API",
    version = "1.0.0",
    status = "Running",
    timestamp = DateTime.UtcNow,
    endpoints = new[]
    {
        "/api/msdocsping - POST - Search Microsoft documentation",
        "/swagger - Swagger UI documentation"
    }
})
.WithName("GetApiInfo")
.WithTags("API Info");

app.Run();

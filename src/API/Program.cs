using MultiTenantIdentityApi.Application;
using MultiTenantIdentityApi.Infrastructure;
using MultiTenantIdentityApi.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Application layer services
builder.Services.AddApplication();

// Add Infrastructure layer services (DB, Identity, JWT, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Multi-Tenant Identity API",
        Version = "v1",
        Description = "A Clean Architecture API with multi-tenant support"
    });

    // Add JWT Authentication support in Swagger
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular default port
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline

// Global exception handler - must be first
app.UseGlobalExceptionHandler();

// Swagger (available in all environments for API documentation)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Tenant Identity API v1");
    options.RoutePrefix = "swagger";
});

// Enable CORS
app.UseCors("AllowAngularApp");

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Multi-tenant middleware - resolves tenant from claims/header/route/query
app.UseMultiTenant();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Root redirect to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

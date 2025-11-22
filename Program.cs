using MultiTenantIdentityApi.Extensions;
using MultiTenantIdentityApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure database contexts
builder.Services.AddDatabaseContexts(builder.Configuration);

// Configure multi-tenancy with Finbuckle
builder.Services.AddMultiTenancy(builder.Configuration);

// Configure Identity
builder.Services.AddIdentityServices();

// Configure JWT authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure application services
builder.Services.AddApplicationServices();

// Configure Swagger
builder.Services.AddSwaggerConfiguration();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline

// Swagger (available in all environments for API documentation)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Multi-Tenant Identity API v1");
    options.RoutePrefix = "swagger";
});

// Enable CORS
app.UseCors("AllowAll");

// Enable authentication & authorization
app.UseAuthentication();
app.UseAuthorization();

// Multi-tenant middleware - resolves tenant from claims/header/route/query
app.UseMultiTenant();

// Custom tenant validation middleware
app.UseTenantValidation();

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Root redirect to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

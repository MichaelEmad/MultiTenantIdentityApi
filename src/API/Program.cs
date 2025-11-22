using Finbuckle.MultiTenant;
using MultiTenantIdentityApi.Application;
using MultiTenantIdentityApi.Infrastructure;
using MultiTenantIdentityApi.API.Handlers;
using Serilog;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Configure Serilog from appsettings.json
// Create bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Multi-Tenant Identity API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog from configuration file
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.WithProperty("Application", "MultiTenantIdentityApi")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName));

    // Add services to the container
    builder.Services.AddControllers();

    // Add problem details
    builder.Services.AddProblemDetails();

    // Add global exception handler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // Add Application layer services
    builder.Services.AddApplication();

    // Add Infrastructure layer services (DB, Identity, JWT, etc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Configure OpenTelemetry
    var serviceName = "MultiTenantIdentityApi";
    var serviceVersion = "1.0.0";

    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = builder.Environment.EnvironmentName
            }))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.Filter = (httpContext) =>
                {
                    // Don't trace health check and swagger endpoints
                    var path = httpContext.Request.Path.Value ?? string.Empty;
                    return !path.Contains("/health") && !path.Contains("/swagger");
                };
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddSqlClientInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.RecordException = true;
            })
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                // Configure OTLP endpoint (e.g., Jaeger, Zipkin, etc.)
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                    ?? "http://localhost:4317");
            }))
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Note: AddRuntimeInstrumentation and AddProcessInstrumentation
            // require additional packages that may not be available in all versions
            .AddConsoleExporter()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"]
                    ?? "http://localhost:4317");
            }));

    // Configure Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new()
        {
            Title = "Multi-Tenant Identity API",
            Version = "v1",
            Description = "A Clean Architecture API with multi-tenant support, observability, and structured logging"
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

    //// Add health checks
    //builder.Services.AddHealthChecks()
    //    .AddDbContextCheck<MultiTenantIdentityApi.Infrastructure.Persistence.ApplicationDbContext>("database");

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
            diagnosticContext.Set("TenantId", httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault());
        };
    });

    // Global exception handler
    app.UseExceptionHandler();

    // Status code pages for error handling
    app.UseStatusCodePages();

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

    Log.Information("Multi-Tenant Identity API started successfully");
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

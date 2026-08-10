using DocVault.Shared.Contracts.Events;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Database
// Configure SQL Server connection string with environment variable fallback.
var sqlConnEnv = Environment.GetEnvironmentVariable("SQLSERVER__CONNECTIONSTRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

string resolvedSqlConn;

if (!string.IsNullOrEmpty(sqlConnEnv))
{
    resolvedSqlConn = sqlConnEnv;
}
else
{
    // Build from explicit parts if provided  
    var host = builder.Configuration["SqlServer:Host"] ?? "localhost";
    var port = builder.Configuration["SqlServer:Port"];
    var user = builder.Configuration["SqlServer:User"];
    var password = builder.Configuration["SqlServer:Password"];

    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
    {
        var dataSource = string.IsNullOrEmpty(port)
            ? host
            : $"{host},{port}";

        resolvedSqlConn =
            $"Server={dataSource};Database=DocVaultDB;User Id={user};Password={password};TrustServerCertificate=True;";
    }
    else
    {
        // Fallback to integrated security
        resolvedSqlConn =
            builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=DocVaultDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}

// ---------- DEBUG OUTPUT ----------
Console.WriteLine("======================================");
Console.WriteLine("SQL CONNECTION STRING");
Console.WriteLine(resolvedSqlConn);
Console.WriteLine("======================================");

try
{
    var csb = new System.Data.Common.DbConnectionStringBuilder();
    csb.ConnectionString = resolvedSqlConn;

    var server = csb.ContainsKey("Server")
        ? csb["Server"]
        : csb["Data Source"];

    Console.WriteLine($"[DocKnowledge] SQL Server connection resolved: {server}");
}
catch
{
}
// ---------- END DEBUG OUTPUT ----------

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(resolvedSqlConn));

// Azure Blob Storage
builder.AddAzureBlobServiceClient("blobs");

// JWT Validation Only
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

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
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey!)),
        RoleClaimType = "role",
        NameClaimType = JwtRegisteredClaimNames.Sub
    };
});

// RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitConnString = Environment.GetEnvironmentVariable("RABBITMQ__CONNECTIONSTRING")
            ?? builder.Configuration.GetConnectionString("rabbitmq");
        string resolvedHost;
        int resolvedPort;

        if (!string.IsNullOrEmpty(rabbitConnString))
        {
            try
            {
                var uri = new Uri(rabbitConnString);
                resolvedHost = uri.Host;
                resolvedPort = uri.Port > 0 ? uri.Port : 5672;
            }
            catch
            {
                resolvedHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
                resolvedPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var p) ? p : 5672;
            }
        }
        else
        {
            resolvedHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
            resolvedPort = int.TryParse(builder.Configuration["RabbitMQ:Port"], out var p) ? p : 5672;
        }

        Console.WriteLine($"[DocKnowledge] RabbitMQ resolved host={resolvedHost} port={resolvedPort}");
        cfg.Host(resolvedHost, (ushort)resolvedPort, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            h.Heartbeat(60);
        });

        cfg.Message<ProjectCreatedEvent>(m =>
        {
            m.SetEntityName("project-created");
        });
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Services
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();


// register of the AI service implementation
builder.Services.AddHttpClient<IAiService, GeminiService>();
builder.Services.AddScoped<ITextExtractionService, TextExtractionService>();

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

// Exposes /health and /alive from AddServiceDefaults()
app.MapDefaultEndpoints();

// Wait for User Management readiness before fully starting the document service.
var userHealthUrl = builder.Configuration["UserService:HealthUrl"] ?? "http://localhost:5261/health";
var userWaitTimeoutSeconds = int.TryParse(builder.Configuration["UserService:WaitTimeoutSeconds"], out var t) ? t : 60;

async Task<bool> WaitForUserServiceAsync()
{
    try
    {
        using var http = new HttpClient();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < userWaitTimeoutSeconds)
        {
            try
            {
                var res = await http.GetAsync(userHealthUrl);
                if (res.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[DocKnowledge] User service ready: {userHealthUrl}");
                    return true;
                }
            }
            catch
            {
                // ignore and retry
            }
            await Task.Delay(1000);
        }
    }
    catch { }
    Console.WriteLine($"[DocKnowledge] User service did not become ready within {userWaitTimeoutSeconds}s");
    return false;
}

var userReady = await WaitForUserServiceAsync();
if (!userReady)
{
    Console.WriteLine("[DocKnowledge] Continuing startup despite user service not ready.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DocVault API";
        options.Theme = ScalarTheme.Moon;
    });
}

var httpsPortEnv = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
var httpsPortCfg = builder.Configuration["HttpsPort"] ?? builder.Configuration["UserService:HttpsPort"];
var httpsPort = httpsPortEnv ?? httpsPortCfg;
if (!string.IsNullOrEmpty(httpsPort) && int.TryParse(httpsPort, out var _))
{
    app.UseHttpsRedirection();
}
else
{
    Console.WriteLine("[DocKnowledge] Skipping HTTPS redirection because no HTTPS port is configured.");
}
app.UseAuthentication();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
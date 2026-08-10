using DocVault.UserManagement.Application.Interfaces;
using DocVault.UserManagement.Infrastructure.Identity;
using DocVault.UserManagement.Infrastructure.Messaging.Consumers;
using DocVault.UserManagement.Infrastructure.Seeders;
using DocVault.UserManagement.Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Database - resolve connection string from env or config
var sqlConnEnv = Environment.GetEnvironmentVariable("SQLSERVER__CONNECTIONSTRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");
string resolvedSqlConn;
if (!string.IsNullOrEmpty(sqlConnEnv))
{
    resolvedSqlConn = sqlConnEnv;
}
else
{
    var host = builder.Configuration["SqlServer:Host"] ?? "localhost";
    var port = builder.Configuration["SqlServer:Port"]; // optional
    var user = builder.Configuration["SqlServer:User"];
    var password = builder.Configuration["SqlServer:Password"];

    if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
    {
        var dataSource = string.IsNullOrEmpty(port) ? host : $"{host},{port}";
        resolvedSqlConn = $"Server={dataSource};Database=DocVaultUserDB;User Id={user};Password={password};TrustServerCertificate=True;";
    }
    else
    {
        resolvedSqlConn = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=DocVaultUserDB;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}

try
{
    var csb = new System.Data.Common.DbConnectionStringBuilder();
    csb.ConnectionString = resolvedSqlConn;
    var server = csb.ContainsKey("Server") ? csb["Server"] : csb["Data Source"];
    Console.WriteLine($"[UserMgmt] SQL Server connection resolved: {server}");
}
catch { }

builder.Services.AddDbContext<UserManagementDbContext>(options => options.UseSqlServer(resolvedSqlConn));

// Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<UserManagementDbContext>()
    .AddDefaultTokenProviders();

// JWT
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

// Http client to call Document (Projects) service to resolve project names when needed
builder.Services.AddHttpClient("DocumentService", client =>
{
    var baseUrl = builder.Configuration["DocumentService:BaseUrl"] ?? builder.Configuration["UserService:DocumentServiceBaseUrl"] ?? "http://localhost:5078";
    try { client.BaseAddress = new Uri(baseUrl); } catch { }
});

builder.Services.AddHttpContextAccessor();

// Register claims transformer to normalize role claims
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, DocVault.UserManagement.API.Infrastructure.ClaimsTransformer>();

// RabbitMQ with MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProjectCreatedConsumer>();

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

        Console.WriteLine($"[UserMgmt] RabbitMQ resolved host={resolvedHost} port={resolvedPort}");
        cfg.Host(resolvedHost, (ushort)resolvedPort, "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
            h.Heartbeat(60);
        });

        cfg.ReceiveEndpoint("user-management-project-created", e =>
        {
            e.Durable = true;
            e.AutoDelete = false;
            e.Bind("project-created", s =>
            {
                s.ExchangeType = "fanout";
                try { s.Durable = true; } catch { }
            });

            e.ConfigureConsumer<ProjectCreatedConsumer>(context);
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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddAuthorization();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin")
        )
    );
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Auto migrate
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
    await db.Database.MigrateAsync();
}

// Seeder
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DataSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Seeding error.");
    }
}

// Backfill: ensure users with ProjectId have a corresponding UserProject entry
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<UserManagementDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        var usersWithProject = await db.Users
            .Where(u => u.ProjectId != null)
            .ToListAsync();

        foreach (var u in usersWithProject)
        {
            var exists = await db.UserProjects.AnyAsync(up => up.ProjectId == u.ProjectId && up.UserId == u.Id);
            if (!exists)
            {
                db.UserProjects.Add(new DocVault.UserManagement.Domain.Entities.UserProject
                {
                    Id = Guid.NewGuid(),
                    ProjectId = u.ProjectId!.Value,
                    ProjectName = string.Empty,
                    UserId = u.Id,
                    Role = "User",
                    AssignedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }
        }
        await db.SaveChangesAsync();
        logger.LogInformation("Backfilled UserProject entries for existing users.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to backfill UserProject entries.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "DocVault User Management API";
        options.Theme = ScalarTheme.Moon;
    });
}

var httpsPortEnv = Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORT");
var httpsPortCfg = builder.Configuration["HttpsPort"];
var httpsPort = httpsPortEnv ?? httpsPortCfg;
if (!string.IsNullOrEmpty(httpsPort) && int.TryParse(httpsPort, out var _))
{
    app.UseHttpsRedirection();
}
else
{
    Console.WriteLine("[UserMgmt] Skipping HTTPS redirection because no HTTPS port is configured.");
}
app.UseAuthentication();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.Run();
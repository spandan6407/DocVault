using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DocVault.UserManagement.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using System.Net.Sockets;

namespace DocVault.UserManagement.API.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly UserManagementDbContext _context;
    private readonly IConfiguration _configuration;

    public HealthController(UserManagementDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Check DB connectivity as a readiness indication
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
                return StatusCode(503, new { status = "unavailable", reason = "db" });

            // Check RabbitMQ broker connectivity (resolve from env or config)
            var rabbitConn = Environment.GetEnvironmentVariable("RABBITMQ__CONNECTIONSTRING")
                ?? _configuration.GetConnectionString("rabbitmq");
            string host = _configuration["RabbitMQ:Host"] ?? "localhost";
            int port = int.TryParse(_configuration["RabbitMQ:Port"], out var p) ? p : 5672;

            if (!string.IsNullOrEmpty(rabbitConn))
            {
                try
                {
                    var uri = new Uri(rabbitConn);
                    host = uri.Host;
                    port = uri.Port > 0 ? uri.Port : port;
                }
                catch { }
            }

            using var tcp = new TcpClient();
            try
            {
                var connectTask = tcp.ConnectAsync(host, port);
                var ct = await Task.WhenAny(connectTask, Task.Delay(2000));
                if (ct != connectTask || !tcp.Connected)
                {
                    return StatusCode(503, new { status = "unavailable", reason = "rabbitmq" });
                }
            }
            catch
            {
                return StatusCode(503, new { status = "unavailable", reason = "rabbitmq" });
            }

            return Ok(new { status = "ready" });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "unavailable", reason = ex.Message });
        }
    }
}

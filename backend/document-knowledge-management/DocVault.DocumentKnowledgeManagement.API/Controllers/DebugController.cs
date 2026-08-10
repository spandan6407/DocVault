using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocVault.DocumentKnowledgeManagement.API.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    [HttpGet("claims")]
    [Authorize]
    public IActionResult GetClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var isAdmin = User.IsInRole("Admin");

        return Ok(new { isAuthenticated = User.Identity?.IsAuthenticated, isAdmin, claims });
    }
}

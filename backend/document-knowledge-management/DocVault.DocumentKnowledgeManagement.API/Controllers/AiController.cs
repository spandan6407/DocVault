using DocVault.DocumentKnowledgeManagement.Application.DTOs.AI;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocVault.DocumentKnowledgeManagement.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiService _aiService;
    public AiController(IAiService aiService) { _aiService = aiService; }

    // Every "project:{guid}" claim on the token: project id -> that user's role in it.
    // Matches DocumentsController's claim-parsing pattern exactly.
    private static Dictionary<Guid, string> GetProjectRoleClaims(ClaimsPrincipal user)
    {
        var map = new Dictionary<Guid, string>();
        foreach (var claim in user.Claims)
        {
            if (claim.Type.StartsWith("project:") &&
                Guid.TryParse(claim.Type.Substring("project:".Length), out var projectId))
            {
                map[projectId] = claim.Value;
            }
        }
        return map;
    }

    private bool IsAdmin() =>
        User.HasClaim(c => (c.Type == ClaimTypes.Role || c.Type == "role") && c.Value == "Admin");

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] AiSearchRequestDto request)
    {
        var results = await _aiService.SearchAsync(request.Query, IsAdmin(), GetProjectRoleClaims(User));
        return Ok(results);
    }

    [HttpGet("documents/{id}/summary")]
    public async Task<IActionResult> GetSummary(Guid id)
    {
        var result = await _aiService.GetDocumentSummaryAsync(id, IsAdmin(), GetProjectRoleClaims(User));
        if (result == null) return NotFound(new { message = "Not found or access denied." });
        return Ok(result);
    }

    [HttpPost("documents/{id}/ask")]
    public async Task<IActionResult> Ask(Guid id, [FromBody] AskQuestionDto request)
    {
        var answer = await _aiService.AskDocumentAsync(id, request.Question, IsAdmin(), GetProjectRoleClaims(User));
        if (answer == null) return NotFound(new { message = "Not found or access denied." });
        return Ok(new { answer });
    }
}
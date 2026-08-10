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

    private (string role, Guid? projectId) GetContext()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var pc = User.FindFirstValue("projectId");
        Guid? pid = !string.IsNullOrEmpty(pc) ? Guid.Parse(pc) : null;
        return (role, pid);
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] AiSearchRequestDto request)
    {
        var (role, pid) = GetContext();
        var results = await _aiService.SearchAsync(request.Query, role, pid);
        return Ok(results);
    }

    [HttpGet("documents/{id}/summary")]
    public async Task<IActionResult> GetSummary(Guid id)
    {
        var (role, pid) = GetContext();
        var result = await _aiService.GetDocumentSummaryAsync(id, role, pid);
        if (result == null) return NotFound(new { message = "Not found or access denied." });
        return Ok(result);
    }

    [HttpPost("documents/{id}/ask")]
    public async Task<IActionResult> Ask(Guid id, [FromBody] AskQuestionDto request)
    {
        var (role, pid) = GetContext();
        var answer = await _aiService.AskDocumentAsync(id, request.Question, role, pid);
        if (answer == null) return NotFound(new { message = "Not found or access denied." });
        return Ok(new { answer });
    }
}
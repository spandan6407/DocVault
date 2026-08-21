using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;

namespace DocVault.DocumentKnowledgeManagement.API.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    // Every "project:{guid}" claim on the token: project id -> that user's role in it.
    // Replaces the old single projectId/role claim pair now that membership is multi-project.
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

    // POST /api/documents
    [HttpPost("documents")]
    [Authorize(Roles = "ProjectHead,User")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadDocumentDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var uploadedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // The project is known up front (it's on the form) — just read that one claim.
        var uploaderRole = User.FindFirstValue($"project:{request.ProjectId}") ?? string.Empty;
        if (string.IsNullOrEmpty(uploaderRole))
            return Forbid();

        var result = await _documentService.UploadDocumentAsync(request, uploadedBy, uploaderRole, request.ProjectId);

        if (result == null)
        {
            return BadRequest(new { message = "Upload failed. Only PDF and Word documents are allowed." });
        }

        return Ok(result);
    }

    // OPTIONS /api/documents/compose
    // Explicitly handle preflight in case CORS middleware is not intercepting OPTIONS early enough.
    [HttpOptions("documents/compose")]
    public IActionResult ComposeOptions()
    {
        return Ok();
    }

    // POST /api/documents/compose
    // Create a text document (saved as .txt in blob storage)
    [HttpPost("documents/compose")]
    [Authorize(Roles = "ProjectHead,User")]
    public async Task<IActionResult> ComposeDocument([FromBody] CreateTextDocumentDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var uploaderRole = User.FindFirstValue($"project:{request.ProjectId}") ?? string.Empty;
        if (string.IsNullOrEmpty(uploaderRole))
            return Forbid();

        var result = await _documentService.CreateTextDocumentAsync(request, createdBy, uploaderRole, request.ProjectId);
        if (result == null)
            return BadRequest(new { message = "Create failed or access denied." });
        return Ok(result);
    }

    // GET /api/projects/{projectId}/documents
    [HttpGet("projects/{projectId}/documents")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> GetProjectDocuments(Guid projectId)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var isAdmin = IsAdmin();
        var requesterRole = isAdmin ? "Admin" : (User.FindFirstValue($"project:{projectId}") ?? string.Empty);

        if (!isAdmin && string.IsNullOrEmpty(requesterRole))
            return Forbid();

        var result = await _documentService.GetProjectDocumentsAsync(projectId, requesterId, requesterRole, projectId);
        return Ok(result);
    }

    // GET /api/documents/{id}/download
    // Document's project isn't known until the service loads it, so pass the full claim map.
    [HttpGet("documents/{id}/download")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.DownloadDocumentAsync(id, requesterId, IsAdmin(), GetProjectRoleClaims(User));

        if (result == null)
        {
            return NotFound(new { message = "Document not found or access denied." });
        }

        return File(result.Value.FileBytes, result.Value.ContentType, result.Value.FileName);
    }

    [HttpGet("documents/search")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> SearchDocuments([FromQuery] string q)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.SearchDocumentsAsync(q, requesterId, IsAdmin(), GetProjectRoleClaims(User));
        return Ok(result);
    }

    // DELETE /api/documents/{id}
    [HttpDelete("documents/{id}")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.DeleteDocumentAsync(id, requesterId, IsAdmin(), GetProjectRoleClaims(User));

        if (!result)
        {
            return BadRequest(new { message = "Delete failed. Check document access." });
        }

        return Ok(new { message = "Document deleted successfully." });
    }

    // PUT /api/documents/{id}
    [HttpPut("documents/{id}")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> UpdateDocument(Guid id, [FromForm] UpdateDocumentDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _documentService.UpdateDocumentAsync(id, request, requesterId, IsAdmin(), GetProjectRoleClaims(User));

        if (result == null)
            return BadRequest(new { message = "Update failed or access denied." });

        return Ok(result);
    }
}
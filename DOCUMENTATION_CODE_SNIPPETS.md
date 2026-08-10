# Document Knowledge Management & User Management — Code Snippets

This file contains the full source code of key files across the Document Knowledge Management and User Management services. It's intended to be used when sharing the project with an external AI or engineer so they can reproduce the code in a single place.

---

## document-knowledge-management

### Controllers/DocumentsController.cs
using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    // POST /api/documents
    [HttpPost("documents")]
   [Authorize(Roles = "ProjectHead,User")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] UploadDocumentDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var uploadedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? string.Empty;

       var uploaderRole = User.FindFirstValue(ClaimTypes.Role)
                               ?? string.Empty;

       var projectIdClaim = User.FindFirstValue("projectId");

        // Handle empty string for Admin
        Guid? uploaderProjectId = !string.IsNullOrEmpty(projectIdClaim)
            ? Guid.Parse(projectIdClaim)
            : null;

       var result = await _documentService.UploadDocumentAsync(
            request,
           uploadedBy,
            uploaderRole,
            uploaderProjectId);

       if (result == null)
        {
            return BadRequest(new
           {
                message = "Upload failed. Only PDF and Word documents are allowed."
            });
        }

       return Ok(result);
    }

   // GET /api/projects/{projectId}/documents
    [HttpGet("projects/{projectId}/documents")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> GetProjectDocuments(Guid projectId)
   {
       var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? string.Empty;

        var requesterRole = User.FindFirstValue(ClaimTypes.Role)
                                ?? string.Empty;

        var projectIdClaim = User.FindFirstValue("projectId");

        // ✅ Fix: Handle empty string for Admin
        Guid? requesterProjectId = !string.IsNullOrEmpty(projectIdClaim)
            ? Guid.Parse(projectIdClaim)
            : null;

       var result = await _documentService.GetProjectDocumentsAsync(
            projectId,
           requesterId,
           requesterRole,
           requesterProjectId);

       return Ok(result);
    }

    // GET /api/documents/{id}/download
    [HttpGet("documents/{id}/download")]
    [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> DownloadDocument(Guid id)
    {
       var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? string.Empty;

       var requesterRole = User.FindFirstValue(ClaimTypes.Role)
                                ?? string.Empty;

        var projectIdClaim = User.FindFirstValue("projectId");

        //  Fix: Handle empty string for Admin
        Guid? requesterProjectId = !string.IsNullOrEmpty(projectIdClaim)
            ? Guid.Parse(projectIdClaim)
           : null;

        var result = await _documentService.DownloadDocumentAsync(
           id,
            requesterId,
           requesterRole,
            requesterProjectId);

        if (result == null)
       {
           return NotFound(new
           {
                message = "Document not found or access denied."
            });
        }

       return File(
            result.Value.FileBytes,
            result.Value.ContentType,
           result.Value.FileName);
    }

    // DELETE /api/documents/{id}
    [HttpDelete("documents/{id}")]
   [Authorize(Roles = "Admin,ProjectHead,User")]
    public async Task<IActionResult> DeleteDocument(Guid id)
    {
        var requesterId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? string.Empty;

        var requesterRole = User.FindFirstValue(ClaimTypes.Role)
                                ?? string.Empty;

       var projectIdClaim = User.FindFirstValue("projectId");


       Guid? requesterProjectId = !string.IsNullOrEmpty(projectIdClaim)
            ? Guid.Parse(projectIdClaim)
            : null;

        var result = await _documentService.DeleteDocumentAsync(
            id,
            requesterId,
            requesterRole,
            requesterProjectId);

        if (!result)
        {
            return BadRequest(new
            {
                message = "Delete failed. Check document access."
            });
       }

        return Ok(new
        {
            message = "Document deleted successfully."
        });
    }
}

```csharp

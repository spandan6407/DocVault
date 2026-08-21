using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DomainDocument = DocVault.DocumentKnowledgeManagement.Domain.Entities.Document;
using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IAiService _aiService;

    private const string ContainerName = "documents";

    private static readonly Dictionary<string, string> AllowedFileTypes = new()
    {
        { ".pdf",  "application/pdf" },
        { ".doc",  "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
    };

    public DocumentService(
        ApplicationDbContext context,
        BlobServiceClient blobServiceClient,
        ITextExtractionService textExtractionService,
        IAiService aiService)
    {
        _context = context;
        _blobServiceClient = blobServiceClient;
        _textExtractionService = textExtractionService;
        _aiService = aiService;
    }

    // Create a text document (written content saved as a .txt file in blob storage)
    public async Task<DocumentResponseDto?> CreateTextDocumentAsync(
        CreateTextDocumentDto request,
        string createdBy,
        string creatorRole,
        Guid projectId)
    {
        // only ProjectHead or User allowed to create text documents in a project
        if (creatorRole != "ProjectHead" && creatorRole != "User")
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);
        if (project == null)
            return null;

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var fileNameSafe = (string.IsNullOrWhiteSpace(request.Title) ? "document" : request.Title).Replace(" ", "_") + ".txt";
        var uniqueBlobName = $"{projectId}/{Guid.NewGuid()}_{fileNameSafe}";
        var blobClient = containerClient.GetBlobClient(uniqueBlobName);

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(request.Content ?? string.Empty);
        using (var stream = new System.IO.MemoryStream(contentBytes))
        {
            await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = "text/plain" });
        }

        var document = new DomainDocument
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            FileName = fileNameSafe,
            FilePath = uniqueBlobName,
            FileSize = contentBytes.Length,
            ContentType = "text/plain",
            ProjectId = projectId,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            ExtractedText = request.Content
        };

        // Optionally build tree from the single text content
        try
        {
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                document.TreeJson = await _ai_service_build_tree_async_placeholder(request.Content);
            }
        }
        catch { /* best-effort */ }

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToResponse(document);
    }

    // Helper wrapper to call _aiService.BuildTreeAsync compatible with original code expectations
    private async Task<string?> _ai_service_build_tree_async_placeholder(string content)
    {
        // The existing AI service expects pages; for simple text create a single page representation
        var pages = new System.Collections.Generic.List<DocVault.DocumentKnowledgeManagement.Application.Interfaces.PageText>
        {
            new DocVault.DocumentKnowledgeManagement.Application.Interfaces.PageText { PageNumber = 1, Text = content }
        };
        return await _aiService.BuildTreeAsync(pages);
    }

    // ── Upload ────────────────────────────────────────────────────────────────
    // BR-006: Admin explicitly cannot upload (enforced at controller via [Authorize(Roles="ProjectHead,User")]).
    // uploaderRole is the caller's role *in the specific project* from the "project:{id}" claim.
    public async Task<DocumentResponseDto?> UploadDocumentAsync(
        UploadDocumentDto request,
        string uploadedBy,
        string uploaderRole,
        Guid projectId)
    {
        // uploaderRole must be ProjectHead or User in this project — anything else is rejected.
        if (uploaderRole != "ProjectHead" && uploaderRole != "User")
            return null;

        var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
        if (!AllowedFileTypes.ContainsKey(fileExtension))
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.IsActive);
        if (project == null)
            return null;

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var uniqueBlobName = $"{projectId}/{Guid.NewGuid()}_{request.File.FileName}";
        var blobClient = containerClient.GetBlobClient(uniqueBlobName);

        using (var stream = request.File.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = AllowedFileTypes[fileExtension]
            });
        }

        var document = new DomainDocument
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            FileName = request.File.FileName,
            FilePath = uniqueBlobName,
            FileSize = request.File.Length,
            ContentType = AllowedFileTypes[fileExtension],
            ProjectId = projectId,
            CreatedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Extract pages + build section tree (best-effort, non-blocking)
        try
        {
            using var extractionStream = request.File.OpenReadStream();
            var pages = await _textExtractionService.ExtractPagesAsync(extractionStream, request.File.FileName);
            if (pages.Count > 0)
            {
                document.ExtractedText = string.Join("\n\n", pages.Select(p => p.Text));
                document.TreeJson = await _aiService.BuildTreeAsync(pages);
            }
        }
        catch { /* AI indexing best-effort — don't block upload */ }

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToResponse(document);
    }

    // ── Get project documents ─────────────────────────────────────────────────
    // BR-007: non-Admin can only see their own project's documents.
    // requesterRole here is the role in the specific project (from "project:{id}" claim).
    public async Task<List<DocumentResponseDto>> GetProjectDocumentsAsync(
        Guid projectId,
        string requesterId,
        string requesterRole,
        Guid requesterProjectId)  // requesterProjectId == projectId for non-Admin (validated in controller)
    {
        var documents = await _context.Documents
            .Where(d => d.ProjectId == projectId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(MapToResponse).ToList();
    }

    // ── Download ──────────────────────────────────────────────────────────────
    // isAdmin bypasses project check; otherwise caller must be a member of the document's project.
    public async Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadDocumentAsync(
        Guid documentId,
        string requesterId,
        bool isAdmin,
        IReadOnlyDictionary<Guid, string> projectRoles)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return null;

        if (!isAdmin && !projectRoles.ContainsKey(document.ProjectId))
            return null;

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(document.FilePath);

        if (!await blobClient.ExistsAsync())
            return null;

        var response = await blobClient.DownloadContentAsync();
        return (response.Value.Content.ToArray(), document.ContentType, document.FileName);
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    // BR-009: ProjectHead can delete any doc in their project.
    // BR-010: User can only delete their own uploads.
    // Admin can delete any doc in any project.
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        string requesterId,
        bool isAdmin,
        IReadOnlyDictionary<Guid, string> projectRoles)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return false;

        if (!isAdmin)
        {
            if (!projectRoles.TryGetValue(document.ProjectId, out var roleInProject))
                return false; // not a member of this document's project

            if (roleInProject == "User" && document.CreatedBy != requesterId)
                return false; // BR-010: User can only delete own uploads

            // ProjectHead can delete any doc in their project — no extra check needed.
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(document.FilePath);
        try { await blobClient.DeleteIfExistsAsync(); }
        catch { /* ignore blob delete failures, proceed to soft-delete */ }

        document.IsActive = false;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    // Same ownership rules as Delete.
    public async Task<DocumentResponseDto?> UpdateDocumentAsync(
        Guid documentId,
        UpdateDocumentDto request,
        string requesterId,
        bool isAdmin,
        IReadOnlyDictionary<Guid, string> projectRoles)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return null;

        if (!isAdmin)
        {
            if (!projectRoles.TryGetValue(document.ProjectId, out var roleInProject))
                return null;

            if (roleInProject == "User" && document.CreatedBy != requesterId)
                return null;
        }

        document.Title = request.Title;
        document.Description = request.Description;
        document.UpdatedAt = DateTime.UtcNow;

        if (request.File != null)
        {
            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!AllowedFileTypes.ContainsKey(fileExtension))
                return null;

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var oldBlob = containerClient.GetBlobClient(document.FilePath);
            try { await oldBlob.DeleteIfExistsAsync(); } catch { }

            var uniqueBlobName = $"{document.ProjectId}/{Guid.NewGuid()}_{request.File.FileName}";
            var blobClient = containerClient.GetBlobClient(uniqueBlobName);

            using (var stream = request.File.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders
                {
                    ContentType = AllowedFileTypes[fileExtension]
                });
            }

            document.FileName = request.File.FileName;
            document.FilePath = uniqueBlobName;
            document.FileSize = request.File.Length;
            document.ContentType = AllowedFileTypes[fileExtension];
        }

        await _context.SaveChangesAsync();
        return MapToResponse(document);
    }

    // ── Search (plain SQL keyword search) ─────────────────────────────────────
    // Admin sees all projects; others see only their member projects.
    public async Task<List<DocumentResponseDto>> SearchDocumentsAsync(
        string query,
        string requesterId,
        bool isAdmin,
        IReadOnlyDictionary<Guid, string> projectRoles)
    {
        var q = _context.Documents.Where(d => d.IsActive);

        if (!isAdmin)
        {
            if (projectRoles.Count == 0)
                return new List<DocumentResponseDto>();

            var memberProjectIds = projectRoles.Keys.ToList();
            q = q.Where(d => memberProjectIds.Contains(d.ProjectId));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(d => d.Title.Contains(query) || d.Description.Contains(query));
        }

        var results = await q.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return results.Select(MapToResponse).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static DocumentResponseDto MapToResponse(DomainDocument document)
    {
        return new DocumentResponseDto
        {
            Id = document.Id,
            Title = document.Title,
            Description = document.Description,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            FileSizeFormatted = FormatFileSize(document.FileSize),
            ProjectId = document.ProjectId,
            CreatedBy = document.CreatedBy,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
            IsActive = document.IsActive,
            FileUrl = $"/api/documents/{document.Id}/download"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
        return $"{bytes / (1024.0 * 1024):F2} MB";
    }
}
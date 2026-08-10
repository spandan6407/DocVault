using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlWordDoc = DocumentFormat.OpenXml.Wordprocessing.Document;
using DomainDocument = DocVault.DocumentKnowledgeManagement.Domain.Entities.Document;
using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using System.IO;

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

    public async Task<DocumentResponseDto?> UploadDocumentAsync(
    UploadDocumentDto request,
    string uploadedBy,
    string uploaderRole,
    Guid? uploaderProjectId)
    {
        if (uploaderRole != "Admin")
        {
            if (uploaderProjectId != request.ProjectId)
                return null;
        }

        var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

        if (!AllowedFileTypes.ContainsKey(fileExtension))
            return null;

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

        if (project == null)
            return null;

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var uniqueBlobName = $"{request.ProjectId}/{Guid.NewGuid()}_{request.File.FileName}";
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
            ProjectId = request.ProjectId,
            CreatedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Extract pages + build section tree (best-effort, non-blocking failure)
        try
        {
            using var extractionStream = request.File.OpenReadStream();

            var pages = await _textExtractionService.ExtractPagesAsync(
                extractionStream,
                request.File.FileName);

            if (pages.Count > 0)
            {
                document.ExtractedText = string.Join("\n\n", pages.Select(p => p.Text));
                document.TreeJson = await _aiService.BuildTreeAsync(pages);
            }
        }
        catch
        {
            // AI indexing best-effort — don't block upload
        }

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToResponse(document);
    }

    // Get Project Documents
    public async Task<List<DocumentResponseDto>> GetProjectDocumentsAsync(
        Guid projectId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId)
    {
        if (requesterRole != "Admin")
        {
            if (requesterProjectId != projectId)
                return new List<DocumentResponseDto>();
        }

        var documents = await _context.Documents
            .Where(d => d.ProjectId == projectId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(d => MapToResponse(d)).ToList();
    }

    // Download Document
    public async Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadDocumentAsync(
        Guid documentId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return null;

        if (requesterRole != "Admin")
        {
            if (requesterProjectId != document.ProjectId)
                return null;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(document.FilePath);

        if (!await blobClient.ExistsAsync())
            return null;

        var response = await blobClient.DownloadContentAsync();
        var fileBytes = response.Value.Content.ToArray();

        return (fileBytes, document.ContentType, document.FileName);
    }

    // Delete Document
    public async Task<bool> DeleteDocumentAsync(
        Guid documentId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return false;

        if (requesterRole == "ProjectHead")
        {
            if (requesterProjectId != document.ProjectId)
                return false;
        }
        else if (requesterRole == "User")
        {
            if (document.CreatedBy != requesterId)
                return false;
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(document.FilePath);
        try
        {
            await blobClient.DeleteIfExistsAsync();
        }
        catch
        {
            // ignore blob delete failures, proceed to soft-delete metadata
        }

        document.IsActive = false;
        document.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    // Update Document (title/description and optional file replacement)
    // update document metadata and optionally replace the file in blob storage///...
    public async Task<DocumentResponseDto?> UpdateDocumentAsync(
        Guid documentId,
        UpdateDocumentDto request,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);

        if (document == null)
            return null;

        // Authorization: Admin (any), ProjectHead (own project), User (own upload only)
        if (requesterRole == "ProjectHead")
        {
            if (requesterProjectId != document.ProjectId)
                return null;
        }
        else if (requesterRole == "User")
        {
            if (document.CreatedBy != requesterId)
                return null;
        }
        else if (requesterRole != "Admin")
        {
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

    // Create a text-based document (.docx) from typed content
    //public async Task<DocumentResponseDto?> CreateTextDocumentAsync(
    //    CreateTextDocumentDto request,
    //    string uploadedBy,
    //    string uploaderRole,
    //    Guid? uploaderProjectId)
    //{
    //    if (uploaderRole != "Admin")
    //    {
    //        if (uploaderProjectId != request.ProjectId)
    //            return null;
    //    }

    //    var project = await _context.Projects
    //        .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

    //    if (project == null)
    //        return null;

    //    byte[] fileBytes;
    //    using (var ms = new MemoryStream())
    //    {
    //        using (var wordDoc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    //        {
    //            var mainPart = wordDoc.AddMainDocumentPart();
    //            mainPart.Document = new OpenXmlWordDoc(
    //                new DocumentFormat.OpenXml.Wordprocessing.Body(
    //                    new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
    //                        new DocumentFormat.OpenXml.Wordprocessing.Run(
    //                            new DocumentFormat.OpenXml.Wordprocessing.Text(request.Content ?? string.Empty)
    //                        )
    //                    )
    //                )
    //            );
    //            mainPart.Document.Save();
    //        }
    //        fileBytes = ms.ToArray();
    //    }

    //    var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
    //    await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

    //    var fileName = $"{request.Title}.docx";
    //    var uniqueBlobName = $"{request.ProjectId}/{Guid.NewGuid()}_{fileName}";
    //    var blobClient = containerClient.GetBlobClient(uniqueBlobName);

    //    using (var uploadStream = new MemoryStream(fileBytes))
    //    {
    //        await blobClient.UploadAsync(uploadStream, new BlobHttpHeaders
    //        {
    //            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    //        });
    //    }

    //    var document = new DomainDocument
    //    {
    //        Id = Guid.NewGuid(),
    //        Title = request.Title,
    //        Description = request.Description,
    //        FileName = fileName,
    //        FilePath = uniqueBlobName,
    //        FileSize = fileBytes.Length,
    //        ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    //        ProjectId = request.ProjectId,
    //        CreatedBy = uploadedBy,
    //        CreatedAt = DateTime.UtcNow,
    //        IsActive = true
    //    };

    //    _context.Documents.Add(document);
    //    await _context.SaveChangesAsync();

    //    return MapToResponse(document);
    //}

    // Search Documents
    public async Task<List<DocumentResponseDto>> SearchDocumentsAsync(
        string query, string requesterId, string requesterRole, Guid? requesterProjectId)
    {
        var q = _context.Documents.Where(d => d.IsActive);

        if (requesterRole != "Admin")
        {
            if (requesterProjectId == null) return new List<DocumentResponseDto>();
            q = q.Where(d => d.ProjectId == requesterProjectId);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            q = q.Where(d => d.Title.Contains(query) || d.Description.Contains(query));
        }

        var results = await q.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return results.Select(d => MapToResponse(d)).ToList();
    }

    // Map to Response
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
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes / (1024.0 * 1024):F2} MB";
    }
}
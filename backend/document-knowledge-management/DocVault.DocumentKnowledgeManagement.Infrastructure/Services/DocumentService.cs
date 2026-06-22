using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Domain.Entities;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly string _storageBasePath;

    // ✅ Allowed file types (extension only)
    private static readonly Dictionary<string, string> AllowedFileTypes = new()
    {
        { ".pdf",  "application/pdf" },
        { ".doc",  "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }
    };

    public DocumentService(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        _context = context;
        _storageBasePath = configuration["FileStorage:BasePath"]
                        ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
    }

    // ✅ Upload Document
    public async Task<DocumentResponseDto?> UploadDocumentAsync(
        UploadDocumentDto request,
        string uploadedBy,
        string uploaderRole,
        Guid? uploaderProjectId)
    {
        // BR-007: User/ProjectHead can only upload to own project
        if (uploaderRole != "Admin")
        {
            if (uploaderProjectId != request.ProjectId)
                return null;
        }

        // ✅ Validate file type by extension only
        var fileExtension = Path.GetExtension(request.File.FileName)
                                .ToLowerInvariant();

        if (!AllowedFileTypes.ContainsKey(fileExtension))
            return null;

        // ✅ Check project exists
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.IsActive);

        if (project == null)
            return null;

        // ✅ Save file to local storage
        var uploadFolder = Path.Combine(
            _storageBasePath, request.ProjectId.ToString());
        Directory.CreateDirectory(uploadFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{request.File.FileName}";
        var filePath = Path.Combine(uploadFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await request.File.CopyToAsync(stream);
        }

        // ✅ Save to database
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            FileName = request.File.FileName,
            FilePath = filePath,
            FileSize = request.File.Length,
            ContentType = AllowedFileTypes[fileExtension],
            ProjectId = request.ProjectId,
            CreatedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return MapToResponse(document);
    }

    // ✅ Get Project Documents
    public async Task<List<DocumentResponseDto>> GetProjectDocumentsAsync(
        Guid projectId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId)
    {
        // BR-007: Users can only access own project documents
        if (requesterRole != "Admin")
        {
            if (requesterProjectId != projectId)
                return new List<DocumentResponseDto>();
        }

        var documents = await _context.Documents
            .Where(d => d.ProjectId == projectId && d.IsActive)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(MapToResponse).ToList();
    }

    // ✅ Download Document
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

        // BR-007: ProjectHead and User can only access own project
        if (requesterRole != "Admin")
        {
            if (requesterProjectId != document.ProjectId)
                return null;
        }

        if (!File.Exists(document.FilePath))
            return null;

        var fileBytes = await File.ReadAllBytesAsync(document.FilePath);
        return (fileBytes, document.ContentType, document.FileName);
    }

    // ✅ Delete Document
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

        // BR-009: ProjectHead can delete any doc in own project
        // BR-010: User can delete only own uploaded documents
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

        // ✅ Soft Delete
        document.IsActive = false;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // ✅ Map to Response
    private static DocumentResponseDto MapToResponse(Document document)
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
            IsActive = document.IsActive
        };
    }

    // 
    private static string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        return $"{bytes / (1024.0 * 1024):F2} MB";
    }
}
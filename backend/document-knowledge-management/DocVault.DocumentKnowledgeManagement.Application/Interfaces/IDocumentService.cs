using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto?> UploadDocumentAsync(
        UploadDocumentDto request, string uploadedBy, string uploaderRole, Guid projectId);

    Task<List<DocumentResponseDto>> GetProjectDocumentsAsync(
        Guid projectId, string requesterId, string requesterRole, Guid requesterProjectId);

    // isAdmin bypasses the project-role check entirely.
    // projectRoles: every project the requester belongs to, mapped to their role in it —
    // the service loads the document, finds its ProjectId, then looks it up in this map.
    Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadDocumentAsync(
        Guid documentId, string requesterId, bool isAdmin, IReadOnlyDictionary<Guid, string> projectRoles);

    Task<List<DocumentResponseDto>> SearchDocumentsAsync(
        string query, string requesterId, bool isAdmin, IReadOnlyDictionary<Guid, string> projectRoles);

    Task<bool> DeleteDocumentAsync(
        Guid documentId, string requesterId, bool isAdmin, IReadOnlyDictionary<Guid, string> projectRoles);

    Task<DocumentResponseDto?> UpdateDocumentAsync(
        Guid documentId, UpdateDocumentDto request, string requesterId, bool isAdmin, IReadOnlyDictionary<Guid, string> projectRoles);
}
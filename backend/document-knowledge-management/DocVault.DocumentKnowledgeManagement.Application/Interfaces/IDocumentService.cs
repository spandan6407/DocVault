using DocVault.DocumentKnowledgeManagement.Application.DTOs.Documents;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto?> UploadDocumentAsync(
        UploadDocumentDto request,
        string uploadedBy,
        string uploaderRole,
        Guid? uploaderProjectId);

    Task<List<DocumentResponseDto>> GetProjectDocumentsAsync(

        Guid projectId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId);

    Task<(byte[] FileBytes, string ContentType, string FileName)?> DownloadDocumentAsync(
        Guid documentId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId);

    Task<bool> DeleteDocumentAsync(
        Guid documentId,
        string requesterId,
        string requesterRole,
        Guid? requesterProjectId);
}
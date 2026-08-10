using DocVault.DocumentKnowledgeManagement.Application.DTOs.AI;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IAiService
{
    Task<string> SummarizeAsync(string text);
    Task<string> AnswerQuestionAsync(string documentText, string question);
    Task<List<AiSearchResultDto>> SearchAsync(string query, string requesterRole, Guid? requesterProjectId);
    Task<DocumentSummaryDto?> GetDocumentSummaryAsync(Guid documentId, string requesterRole, Guid? requesterProjectId);
    Task<string?> AskDocumentAsync(Guid documentId, string question, string requesterRole, Guid? requesterProjectId);
    Task<string> BuildTreeAsync(List<PageText> pages);
}
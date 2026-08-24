using DocVault.DocumentKnowledgeManagement.Application.DTOs.AI;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;

namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface IAiService
{
    Task<string> SummarizeAsync(string text);
    Task<string> AnswerQuestionAsync(string documentText, string question);
    Task<List<AiSearchResultDto>> SearchAsync(string query, bool isAdmin, Dictionary<Guid, string> projectRoles);
    Task<DocumentSummaryDto?> GetDocumentSummaryAsync(Guid documentId, bool isAdmin, Dictionary<Guid, string> projectRoles);
    Task<string?> AskDocumentAsync(Guid documentId, string question, bool isAdmin, Dictionary<Guid, string> projectRoles);
    Task<string> BuildTreeAsync(List<PageText> pages);
}
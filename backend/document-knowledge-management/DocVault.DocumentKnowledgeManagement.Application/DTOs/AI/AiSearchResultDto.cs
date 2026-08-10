namespace DocVault.DocumentKnowledgeManagement.Application.DTOs.AI;

public class AiSearchResultDto
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public double Score { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}
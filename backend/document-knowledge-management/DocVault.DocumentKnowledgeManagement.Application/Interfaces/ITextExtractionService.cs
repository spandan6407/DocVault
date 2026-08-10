namespace DocVault.DocumentKnowledgeManagement.Application.Interfaces;

public interface ITextExtractionService
{
    Task<List<PageText>> ExtractPagesAsync(Stream fileStream, string fileName);
}

public class PageText
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;

}
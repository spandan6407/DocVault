using System.Text;
using System.Text.Json;
using DocVault.DocumentKnowledgeManagement.Application.DTOs.AI;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;
using DocVault.DocumentKnowledgeManagement.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class GeminiService : IAiService
{
    private readonly HttpClient _http;
    private readonly ApplicationDbContext _context;
    private readonly string _apiKey;

    public GeminiService(HttpClient http, ApplicationDbContext context, IConfiguration config)
    {
        _http = http;
        _context = context;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
    }

    // ---------- Core Gemini call ------------ ...
    private async Task<string> GenerateAsync(string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent?key={_apiKey}";
        var body = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var res = await _http.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"Gemini API error ({res.StatusCode}): {json}");

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? string.Empty;
    }

    private async Task<string> CallGeminiAndCleanJson(string prompt)
    {
        var raw = await GenerateAsync(prompt);
        raw = raw.Trim();
        if (raw.StartsWith("```"))
        {
            raw = raw.Substring(raw.IndexOf('\n') + 1);
            raw = raw.Substring(0, raw.LastIndexOf("```"));
        }
        return raw.Trim();
    }

    // ---------- Summarization ----------...
    public async Task<string> SummarizeAsync(string text)
    {
        var prompt = $"Summarize this in 3-5 sentences covering overview, key points, and main topics:\n\n{text.Substring(0, Math.Min(text.Length, 8000))}";
        return await GenerateAsync(prompt);
    }

    // ---------- Q&A ----------
    public async Task<string> AnswerQuestionAsync(string documentText, string question)
    {
        var prompt = $"Answer using ONLY the information given below. If not found, say you cannot find it.\n\nContent:\n{documentText.Substring(0, Math.Min(documentText.Length, 8000))}\n\nQuestion: {question}";
        return await GenerateAsync(prompt);
    }

    // ---------- Tree building (Step 2) ----------....
    public async Task<string> BuildTreeAsync(List<PageText> pages)
    {
        var fullText = string.Join("\n\n", pages.Select(p => $"[PAGE {p.PageNumber}]\n{p.Text}"));
        fullText = fullText.Substring(0, Math.Min(fullText.Length, 20000));

        var prompt = $@"You are building a table-of-contents tree for this document.
Break it into top-level sections. For each section provide:
- title
- startPage and endPage (integers, based on the [PAGE N] markers in the text)
- a 1-2 sentence summary

Return ONLY valid JSON, this exact shape, no extra text:
[{{""title"": ""..."", ""nodeId"": ""0001"", ""startPage"": 1, ""endPage"": 3, ""summary"": ""...""}}]

Document:
{fullText}";

        var topLevelJson = await CallGeminiAndCleanJson(prompt);
        var topLevelNodes = JsonSerializer.Deserialize<List<TreeNode>>(topLevelJson) ?? new List<TreeNode>();

        foreach (var node in topLevelNodes)
        {
            var nodePages = pages.Where(p => p.PageNumber >= node.StartPage && p.PageNumber <= node.EndPage).ToList();
            if (nodePages.Count == 0) continue;

            var nodeText = string.Join("\n\n", nodePages.Select(p => $"[PAGE {p.PageNumber}]\n{p.Text}"));
            nodeText = nodeText.Substring(0, Math.Min(nodeText.Length, 8000));

            var childPrompt = $@"Break this section into sub-sections (2-5 is fine, or return an empty array if it doesn't need sub-sections).
Same JSON shape as before: [{{""title"": ""..."", ""nodeId"": ""..."", ""startPage"": N, ""endPage"": N, ""summary"": ""...""}}]

Section text:
{nodeText}";

            try
            {
                var childJson = await CallGeminiAndCleanJson(childPrompt);
                node.Children = JsonSerializer.Deserialize<List<TreeNode>>(childJson) ?? new List<TreeNode>();
            }
            catch
            {
                node.Children = new List<TreeNode>();
            }
        }

        return JsonSerializer.Serialize(topLevelNodes);
    }

    // ---------- Document Summary (reads from tree) ----------
    public async Task<DocumentSummaryDto?> GetDocumentSummaryAsync(Guid documentId, string requesterRole, Guid? requesterProjectId)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);
        if (doc == null) return null;
        if (requesterRole != "Admin" && requesterProjectId != doc.ProjectId) return null;

        string sourceText;
        if (!string.IsNullOrWhiteSpace(doc.TreeJson))
        {
            var nodes = JsonSerializer.Deserialize<List<TreeNode>>(doc.TreeJson) ?? new List<TreeNode>();
            sourceText = string.Join("\n", nodes.Select(n => $"{n.Title}: {n.Summary}"));
        }
        else
        {
            sourceText = doc.ExtractedText ?? doc.Description;
        }

        var summary = await SummarizeAsync(sourceText);
        return new DocumentSummaryDto { Summary = summary };
    }

    // ---------- Document Q&A (tree-guided) ----------
    public async Task<string?> AskDocumentAsync(Guid documentId, string question, string requesterRole, Guid? requesterProjectId)
    {
        var doc = await _context.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.IsActive);
        if (doc == null) return null;
        if (requesterRole != "Admin" && requesterProjectId != doc.ProjectId) return null;

        if (string.IsNullOrWhiteSpace(doc.TreeJson))
        {
            return await AnswerQuestionAsync(doc.ExtractedText ?? string.Empty, question);
        }

        var nodes = JsonSerializer.Deserialize<List<TreeNode>>(doc.TreeJson) ?? new List<TreeNode>();
        var nodeList = string.Join("\n", nodes.Select(n => $"{n.NodeId}: {n.Title} — {n.Summary}"));

        var pickPrompt = $@"Given this document's sections, which nodeId is most relevant to answering the question?
Reply with ONLY the nodeId, nothing else.

Sections:
{nodeList}

Question: {question}";

        var pickedNodeId = (await GenerateAsync(pickPrompt)).Trim();
        var pickedNode = nodes.FirstOrDefault(n => n.NodeId == pickedNodeId) ?? nodes.FirstOrDefault();

        var context = pickedNode != null
            ? $"{pickedNode.Title}: {pickedNode.Summary}\n" + string.Join("\n", pickedNode.Children.Select(c => $"{c.Title}: {c.Summary}"))
            : doc.ExtractedText ?? string.Empty;

        return await AnswerQuestionAsync(context, question);
    }

    // ---------- Search across documents (tree-based relevance) ----------
    public async Task<List<AiSearchResultDto>> SearchAsync(string query, string requesterRole, Guid? requesterProjectId)
    {
        var docsQuery = _context.Documents.Where(d => d.IsActive && d.TreeJson != null);
        if (requesterRole != "Admin")
        {
            if (requesterProjectId == null) return new List<AiSearchResultDto>();
            docsQuery = docsQuery.Where(d => d.ProjectId == requesterProjectId);
        }
        var docs = await docsQuery.ToListAsync();
        var results = new List<AiSearchResultDto>();

        foreach (var doc in docs)
        {
            var prompt = $@"Query: {query}
Document outline (JSON): {doc.TreeJson}
Is this document relevant to the query? Reply with ONLY a number 0-10 (relevance score), nothing else.";

            string scoreText;
            try { scoreText = await GenerateAsync(prompt); }
            catch { continue; }

            if (double.TryParse(scoreText.Trim(), out var score) && score > 3)
            {
                results.Add(new AiSearchResultDto
                {
                    DocumentId = doc.Id,
                    Title = doc.Title,
                    Summary = await SummarizeAsync(doc.TreeJson!),
                    Score = score,
                    FileUrl = $"/api/documents/{doc.Id}/download"
                });
            }
        }
        return results.OrderByDescending(r => r.Score).Take(10).ToList();
    }
}

public class TreeNode
{
    public string Title { get; set; } = string.Empty;
    public string NodeId { get; set; } = string.Empty;
    public int StartPage { get; set; }
    public int EndPage { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<TreeNode> Children { get; set; } = new();
}
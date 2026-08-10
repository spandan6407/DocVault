using System.IO.Compression;
using System.Xml.Linq;
using UglyToad.PdfPig;
using DocVault.DocumentKnowledgeManagement.Application.Interfaces;

namespace DocVault.DocumentKnowledgeManagement.Infrastructure.Services;

public class TextExtractionService : ITextExtractionService
{
    public async Task<List<PageText>> ExtractPagesAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        if (ext == ".pdf")
        {
            using var pdf = PdfDocument.Open(ms);
            return pdf.GetPages()
                .Select(p => new PageText { PageNumber = p.Number, Text = p.Text })
                .ToList();
        }
        if (ext == ".docx")
        {
            // DOCX has no reliable native page boundaries — treated as one page.
            var text = ExtractDocxText(ms);
            return new List<PageText> { new PageText { PageNumber = 1, Text = text } };
        }
        return new List<PageText>();
    }

    private static string ExtractDocxText(Stream docxStream)
    {
        using var archive = new ZipArchive(docxStream, ZipArchiveMode.Read);
        var entry = archive.GetEntry("word/document.xml");
        if (entry == null) return string.Empty;

        using var entryStream = entry.Open();
        var doc = XDocument.Load(entryStream);
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        return string.Join(" ", doc.Descendants(w + "t").Select(t => t.Value));
    }
}
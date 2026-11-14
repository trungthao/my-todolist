using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/reading")]
[Authorize]
public partial class ReadingController(AppDbContext db, IWebHostEnvironment env) : ControllerBase
{
    private int GetUserId() => int.Parse(User.FindFirstValue("uid")!);

    // ── Sources ──────────────────────────────────────────────────────────────

    [HttpGet("sources")]
    public async Task<ActionResult<IEnumerable<SourceResponse>>> GetSources()
    {
        var sources = await db.ReadingSources
            .OrderBy(s => s.CreatedAt)
            .Select(s => new SourceResponse(
                s.Id, s.Title, s.Description, s.Slug, s.CoverUrl,
                s.Documents.Count, s.CreatedAt))
            .ToListAsync();
        return Ok(sources);
    }

    [HttpGet("sources/{slug}")]
    public async Task<ActionResult<object>> GetSource(string slug)
    {
        var userId = GetUserId();
        var source = await db.ReadingSources
            .Where(s => s.Slug == slug)
            .Select(s => new
            {
                s.Id, s.Title, s.Description, s.Slug, s.CoverUrl,
                Documents = s.Documents
                    .OrderBy(d => d.SortOrder)
                    .Select(d => new DocumentResponse(
                        d.Id, d.SourceId, d.Title, d.Subtitle, d.Slug, d.PostDate, d.SortOrder,
                        d.Progresses
                            .Where(p => p.UserId == userId)
                            .Select(p => (float?)p.ScrollPercent)
                            .FirstOrDefault()))
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (source == null) return NotFound();
        return Ok(source);
    }

    // ── Document content ─────────────────────────────────────────────────────

    [HttpGet("documents/{id:int}/content")]
    public async Task<ActionResult<string>> GetContent(int id)
    {
        var doc = await db.ReadingDocuments
            .Include(d => d.Source)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();

        var filePath = Path.Combine(env.WebRootPath, "reading", doc.Source.Slug, doc.HtmlFile);
        if (!System.IO.File.Exists(filePath)) return NotFound(new { message = "File nội dung không tồn tại." });

        var html = await System.IO.File.ReadAllTextAsync(filePath);
        var body = ExtractBody(html);
        return Ok(body);
    }

    // Strip outer HTML shell, return only article content
    private static string ExtractBody(string html)
    {
        var m = ArticleBodyRegex().Match(html);
        if (m.Success) return m.Groups[1].Value;
        m = BodyTagRegex().Match(html);
        if (m.Success) return m.Groups[1].Value;
        return html;
    }

    [GeneratedRegex(@"<article[^>]*>([\s\S]*?)</article>", RegexOptions.IgnoreCase)]
    private static partial Regex ArticleBodyRegex();
    [GeneratedRegex(@"<body[^>]*>([\s\S]*)</body>", RegexOptions.IgnoreCase)]
    private static partial Regex BodyTagRegex();

    // ── Progress ─────────────────────────────────────────────────────────────

    [HttpPut("progress")]
    public async Task<IActionResult> UpsertProgress(ProgressUpsertRequest req)
    {
        var userId = GetUserId();
        var exists = await db.ReadingProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.DocumentId == req.DocumentId);

        if (exists != null)
        {
            exists.ScrollPercent = req.ScrollPercent;
            exists.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ReadingProgresses.Add(new ReadingProgress
            {
                UserId = userId,
                DocumentId = req.DocumentId,
                ScrollPercent = req.ScrollPercent,
            });
        }
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Highlights ───────────────────────────────────────────────────────────

    [HttpGet("highlights/{documentId:int}")]
    public async Task<ActionResult<IEnumerable<HighlightResponse>>> GetHighlights(int documentId)
    {
        var userId = GetUserId();
        var highlights = await db.Highlights
            .Where(h => h.UserId == userId && h.DocumentId == documentId)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new HighlightResponse(h.Id, h.DocumentId, h.RangeJson, h.SelectedText, h.Color, h.CreatedAt))
            .ToListAsync();
        return Ok(highlights);
    }

    [HttpPost("highlights")]
    public async Task<ActionResult<HighlightResponse>> CreateHighlight(CreateHighlightRequest req)
    {
        var userId = GetUserId();
        var hl = new Highlight
        {
            UserId = userId,
            DocumentId = req.DocumentId,
            RangeJson = req.RangeJson,
            SelectedText = req.SelectedText,
            Color = req.Color,
        };
        db.Highlights.Add(hl);
        await db.SaveChangesAsync();
        return Ok(new HighlightResponse(hl.Id, hl.DocumentId, hl.RangeJson, hl.SelectedText, hl.Color, hl.CreatedAt));
    }

    [HttpDelete("highlights/{id:int}")]
    public async Task<IActionResult> DeleteHighlight(int id)
    {
        var userId = GetUserId();
        var hl = await db.Highlights.FirstOrDefaultAsync(h => h.Id == id && h.UserId == userId);
        if (hl == null) return NotFound();
        db.Highlights.Remove(hl);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [HttpGet("notes/{documentId:int}")]
    public async Task<ActionResult<IEnumerable<NoteResponse>>> GetNotes(int documentId)
    {
        var userId = GetUserId();
        var notes = await db.ReadingNotes
            .Where(n => n.UserId == userId && n.DocumentId == documentId)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteResponse(n.Id, n.DocumentId, n.Content, n.CreatedAt, n.UpdatedAt))
            .ToListAsync();
        return Ok(notes);
    }

    [HttpPost("notes")]
    public async Task<ActionResult<NoteResponse>> CreateNote(CreateNoteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest(new { message = "Nội dung note không được trống." });
        var userId = GetUserId();
        var note = new ReadingNote { UserId = userId, DocumentId = req.DocumentId, Content = req.Content.Trim() };
        db.ReadingNotes.Add(note);
        await db.SaveChangesAsync();
        return Ok(new NoteResponse(note.Id, note.DocumentId, note.Content, note.CreatedAt, note.UpdatedAt));
    }

    [HttpPut("notes/{id:int}")]
    public async Task<ActionResult<NoteResponse>> UpdateNote(int id, UpdateNoteRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest(new { message = "Nội dung note không được trống." });
        var userId = GetUserId();
        var note = await db.ReadingNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note == null) return NotFound();
        note.Content = req.Content.Trim();
        note.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new NoteResponse(note.Id, note.DocumentId, note.Content, note.CreatedAt, note.UpdatedAt));
    }

    [HttpDelete("notes/{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var userId = GetUserId();
        var note = await db.ReadingNotes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
        if (note == null) return NotFound();
        db.ReadingNotes.Remove(note);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Import ───────────────────────────────────────────────────────────────

    [HttpPost("import")]
    public async Task<ActionResult> Import(ImportSourceRequest req)
    {
        var existing = await db.ReadingSources.FirstOrDefaultAsync(s => s.Slug == req.Slug);
        if (existing != null)
        {
            existing.Title = req.Title;
            existing.Description = req.Description;
            existing.CoverUrl = req.CoverUrl;
            foreach (var item in req.Documents)
            {
                var doc = await db.ReadingDocuments.FirstOrDefaultAsync(d => d.SourceId == existing.Id && d.Slug == item.Slug);
                if (doc == null)
                {
                    db.ReadingDocuments.Add(new ReadingDocument
                    {
                        SourceId = existing.Id,
                        Title = item.Title,
                        Subtitle = item.Subtitle,
                        Slug = item.Slug,
                        HtmlFile = item.HtmlFile,
                        PostDate = item.PostDate != null ? DateOnly.Parse(item.PostDate) : null,
                        SortOrder = item.SortOrder,
                    });
                }
                else
                {
                    doc.Title = item.Title;
                    doc.HtmlFile = item.HtmlFile;
                    doc.SortOrder = item.SortOrder;
                }
            }
        }
        else
        {
            var source = new ReadingSource
            {
                Title = req.Title, Description = req.Description,
                Slug = req.Slug, CoverUrl = req.CoverUrl,
                Documents = req.Documents.Select((item, _) => new ReadingDocument
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    Slug = item.Slug,
                    HtmlFile = item.HtmlFile,
                    PostDate = item.PostDate != null ? DateOnly.Parse(item.PostDate) : null,
                    SortOrder = item.SortOrder,
                }).ToList()
            };
            db.ReadingSources.Add(source);
        }
        await db.SaveChangesAsync();
        return Ok(new { message = "Import thành công." });
    }
}

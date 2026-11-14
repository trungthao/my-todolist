namespace TodoApi.Dtos;

public record SourceResponse(
    int Id, string Title, string? Description, string Slug,
    string? CoverUrl, int DocumentCount, DateTime CreatedAt);

public record DocumentResponse(
    int Id, int SourceId, string Title, string? Subtitle, string Slug,
    DateOnly? PostDate, int SortOrder, float? ScrollPercent);

public record ProgressUpsertRequest(int DocumentId, float ScrollPercent);

public record HighlightResponse(
    int Id, int DocumentId, string RangeJson, string SelectedText, string Color, DateTime CreatedAt);

public record CreateHighlightRequest(int DocumentId, string RangeJson, string SelectedText, string Color);

public record NoteResponse(int Id, int DocumentId, string Content, DateTime CreatedAt, DateTime UpdatedAt);

public record CreateNoteRequest(int DocumentId, string Content);

public record UpdateNoteRequest(string Content);

// ── Import ──────────────────────────────────────────────────────────────────
public record ImportSourceRequest(
    string Title, string? Description, string Slug, string? CoverUrl,
    List<ImportDocumentItem> Documents);

public record ImportDocumentItem(
    string Title, string? Subtitle, string Slug, string HtmlFile,
    string? PostDate, int SortOrder);

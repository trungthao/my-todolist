namespace TodoApi.Models;

public class ReadingDocument
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public ReadingSource Source { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string HtmlFile { get; set; } = string.Empty;
    public DateOnly? PostDate { get; set; }
    public int SortOrder { get; set; }
    public ICollection<ReadingProgress> Progresses { get; set; } = [];
    public ICollection<Highlight> Highlights { get; set; } = [];
    public ICollection<ReadingNote> Notes { get; set; } = [];
}

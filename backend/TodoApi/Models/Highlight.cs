namespace TodoApi.Models;

public class Highlight
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int DocumentId { get; set; }
    public ReadingDocument Document { get; set; } = null!;
    public string RangeJson { get; set; } = string.Empty;
    public string SelectedText { get; set; } = string.Empty;
    public string Color { get; set; } = "yellow";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

namespace UserFeed.Application.DTOs;

public class ArticleInfoResponse
{
    public string ArticleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Stock { get; set; }
    public bool Enabled { get; set; }
    public int CommentCount { get; set; }
    public double AverageRating { get; set; }
}

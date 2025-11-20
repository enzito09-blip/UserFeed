namespace UserFeed.Application.DTOs;

public class CreateCommentRequest
{
    public string ArticleId { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; } // 1-5
}

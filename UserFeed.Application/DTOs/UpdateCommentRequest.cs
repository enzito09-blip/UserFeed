namespace UserFeed.Application.DTOs;

public class UpdateCommentRequest
{
    public string Comment { get; set; } = string.Empty;
    public int Rating { get; set; }
}

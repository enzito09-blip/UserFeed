using System.ComponentModel.DataAnnotations;
namespace UserFeed.Application.DTOs;

public class CreateCommentRequest
{
    [Required]
    public string ArticleId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    [Range(1,5)]
    public int Rating { get; set; }
}

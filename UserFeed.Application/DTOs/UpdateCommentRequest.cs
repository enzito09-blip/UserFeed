using System.ComponentModel.DataAnnotations;
namespace UserFeed.Application.DTOs;

public class UpdateCommentRequest
{
    [Required]
    [MaxLength(500)]
    public string Comment { get; set; } = string.Empty;

    [Range(1,5)]
    public int Rating { get; set; }
}

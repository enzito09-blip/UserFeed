using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using UserFeed.Domain.Entities;
using UserFeed.Tests.Helpers;
using Xunit;

namespace UserFeed.Tests.Controllers;

public class CommentsControllerDeleteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentsControllerDeleteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Reset();
    }

    [Fact]
    public async Task DeleteComment_Success_Returns204()
    {
        // Arrange
        var userId = "user123";
        var commentId = "comment1";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = commentId,
            UserId = userId,
            ArticleId = "article1",
            Comment = "To be deleted",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify comment is marked as deleted
        var comment = await _factory.CommentRepository.GetByIdAsync(commentId);
        comment.Should().NotBeNull();
        comment!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteComment_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.DeleteAsync("/api/v1/comments/comment1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteComment_CommentNotFound_Returns404()
    {
        // Arrange
        var userId = "user123";
        var token = JwtTokenHelper.GenerateToken(userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync("/api/v1/comments/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteComment_AlreadyDeleted_Returns400()
    {
        // Arrange
        var userId = "user123";
        var commentId = "comment1";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = commentId,
            UserId = userId,
            ArticleId = "article1",
            Comment = "Already deleted",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true // Already deleted!
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteComment_OtherUserComment_Returns401()
    {
        // Arrange
        var ownerId = "owner123";
        var otherUserId = "other456";
        var commentId = "comment1";
        var token = JwtTokenHelper.GenerateToken(otherUserId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = commentId,
            UserId = ownerId, // Different user
            ArticleId = "article1",
            Comment = "Other's comment",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/comments/{commentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteComment_VerifyLogicalDelete()
    {
        // Arrange
        var userId = "user123";
        var commentId = "comment1";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = commentId,
            UserId = userId,
            ArticleId = "article1",
            Comment = "Test",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        await _client.DeleteAsync($"/api/v1/comments/{commentId}");

        // Assert - Comment still exists but marked as deleted
        var allComments = _factory.CommentRepository.GetAll();
        allComments.Should().HaveCount(1);
        allComments.First().IsDeleted.Should().BeTrue();
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UserFeed.Application.DTOs;
using UserFeed.Domain.Entities;
using UserFeed.Tests.Helpers;
using Xunit;

namespace UserFeed.Tests.Controllers;

public class CommentsControllerUpdateTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentsControllerUpdateTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Reset();
    }

    [Fact]
    public async Task UpdateComment_Success_Returns200()
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
            Comment = "Original comment",
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        var request = new UpdateCommentRequest
        {
            Comment = "Updated comment",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CommentResponse>();
        result.Should().NotBeNull();
        result!.Comment.Should().Be("Updated comment");
        result.Rating.Should().Be(5);
    }

    [Fact]
    public async Task UpdateComment_WithoutToken_Returns401()
    {
        // Arrange
        var request = new UpdateCommentRequest
        {
            Comment = "Update",
            Rating = 5
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/comments/comment1", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task UpdateComment_InvalidRating_Returns400(int rating)
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
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        var request = new UpdateCommentRequest
        {
            Comment = "Update",
            Rating = rating
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateComment_CommentTooLong_Returns400()
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
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        var request = new UpdateCommentRequest
        {
            Comment = new string('A', 501),
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateComment_CommentNotFound_Returns404()
    {
        // Arrange
        var userId = "user123";
        var token = JwtTokenHelper.GenerateToken(userId);

        var request = new UpdateCommentRequest
        {
            Comment = "Update",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync("/api/v1/comments/nonexistent", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateComment_DeletedComment_Returns400()
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
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true // Deleted!
        });

        var request = new UpdateCommentRequest
        {
            Comment = "Update",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateComment_OtherUserComment_Returns401()
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
            Comment = "Test",
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        var request = new UpdateCommentRequest
        {
            Comment = "Trying to update",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateComment_Exactly500Characters_Success()
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
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        var request = new UpdateCommentRequest
        {
            Comment = new string('B', 500),
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/comments/{commentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

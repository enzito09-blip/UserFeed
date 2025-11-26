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

public class CommentsControllerGetByUserTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentsControllerGetByUserTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Reset();
    }

    [Fact]
    public async Task GetMyComments_Success_ReturnsUserComments()
    {
        // Arrange
        var userId = "user123";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "1",
            UserId = userId,
            ArticleId = "article1",
            Comment = "My comment 1",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "2",
            UserId = userId,
            ArticleId = "article2",
            Comment = "My comment 2",
            Rating = 4,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        // Add comment from different user
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "3",
            UserId = "otherUser",
            ArticleId = "article1",
            Comment = "Other user comment",
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/comments/my-comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.Should().AllSatisfy(c => c.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task GetMyComments_WithoutToken_Returns401()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/comments/my-comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyComments_NoComments_ReturnsEmptyList()
    {
        // Arrange
        var userId = "user123";
        var token = JwtTokenHelper.GenerateToken(userId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/comments/my-comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyComments_ExcludesDeletedComments()
    {
        // Arrange
        var userId = "user123";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "1",
            UserId = userId,
            ArticleId = "article1",
            Comment = "Active comment",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "2",
            UserId = userId,
            ArticleId = "article2",
            Comment = "Deleted comment",
            Rating = 4,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true
        });

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/v1/comments/my-comments");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
        result[0].Comment.Should().Be("Active comment");
    }
}

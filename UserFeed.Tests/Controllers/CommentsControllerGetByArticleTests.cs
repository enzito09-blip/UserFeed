using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UserFeed.Application.DTOs;
using UserFeed.Domain.Entities;
using Xunit;

namespace UserFeed.Tests.Controllers;

public class CommentsControllerGetByArticleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentsControllerGetByArticleTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Reset();
    }

    [Fact]
    public async Task GetCommentsByArticle_Success_ReturnsComments()
    {
        // Arrange
        var articleId = "article123";
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "1",
            UserId = "user1",
            ArticleId = articleId,
            Comment = "Great!",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "2",
            UserId = "user2",
            ArticleId = articleId,
            Comment = "Good",
            Rating = 4,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
    }

    [Fact]
    public async Task GetCommentsByArticle_EmptyList_ReturnsEmptyArray()
    {
        // Arrange
        var articleId = "nonexistent";

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCommentsByArticle_ExcludesDeletedComments()
    {
        // Arrange
        var articleId = "article123";
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "1",
            UserId = "user1",
            ArticleId = articleId,
            Comment = "Active",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "2",
            UserId = "user2",
            ArticleId = articleId,
            Comment = "Deleted",
            Rating = 3,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = true
        });

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(1);
        result[0].Comment.Should().Be("Active");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetCommentsByArticle_InvalidPage_Returns400(int page)
    {
        // Arrange
        var articleId = "article123";

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}?page={page}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(99)]
    [InlineData(101)]
    public async Task GetCommentsByArticle_InvalidPageSize_Returns400(int pageSize)
    {
        // Arrange
        var articleId = "article123";

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}?pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    [InlineData(80)]
    [InlineData(100)]
    public async Task GetCommentsByArticle_ValidPageSizes_ReturnsOk(int pageSize)
    {
        // Arrange
        var articleId = "article123";
        _factory.CommentRepository.AddComment(new UserComment
        {
            Id = "1",
            UserId = "user1",
            ArticleId = articleId,
            Comment = "Test",
            Rating = 5,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        });

        // Act
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}?pageSize={pageSize}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCommentsByArticle_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        var articleId = "article123";
        for (int i = 1; i <= 25; i++)
        {
            _factory.CommentRepository.AddComment(new UserComment
            {
                Id = $"comment{i}",
                UserId = $"user{i}",
                ArticleId = articleId,
                Comment = $"Comment {i}",
                Rating = 5,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });
        }

        // Act - Get page 2 with pageSize 10
        var response = await _client.GetAsync($"/api/v1/comments/article/{articleId}?page=2&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<CommentResponse>>();
        result.Should().NotBeNull();
        result!.Count.Should().Be(10);
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using UserFeed.Application.DTOs;
using UserFeed.Domain.Ports;
using UserFeed.Tests.Helpers;
using Xunit;

namespace UserFeed.Tests.Controllers;

public class CommentsControllerCreateTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CommentsControllerCreateTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.Reset();
    }

    [Fact]
    public async Task CreateComment_Success_Returns201()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated", Articles = 1 }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Excelente producto",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CommentResponse>();
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.ArticleId.Should().Be(articleId);
        result.Comment.Should().Be("Excelente producto");
        result.Rating.Should().Be(5);
    }

    [Fact]
    public async Task CreateComment_WithoutToken_Returns401()
    {
        // Arrange
        var request = new CreateCommentRequest
        {
            ArticleId = "article123",
            Comment = "Test",
            Rating = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task CreateComment_InvalidRating_Returns400(int rating)
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Test comment",
            Rating = rating
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateComment_CommentTooLong_Returns400()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = new string('A', 501), // 501 characters
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateComment_ArticleDoesNotExist_Returns400()
    {
        // Arrange
        var userId = "user123";
        var articleId = "nonexistent";
        var token = JwtTokenHelper.GenerateToken(userId);

        // Article NOT added to catalog
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Test",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateComment_UserDidNotPurchase_Returns401()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        // User has validated orders but did NOT purchase this article
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Test",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateComment_UserHasNoValidatedOrders_Returns401()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "pending" } // Not validated
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Test",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateComment_DuplicateComment_Returns409()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        // Create first comment
        var firstRequest = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "First comment",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.PostAsJsonAsync("/api/v1/comments", firstRequest);

        // Try to create duplicate
        var duplicateRequest = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Duplicate comment",
            Rating = 4
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", duplicateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("userId")]
    [InlineData("userID")]
    [InlineData("uid")]
    [InlineData("sub")]
    public async Task CreateComment_DifferentClaimTypes_Success(string claimType)
    {
        // Arrange
        var userId = "user123";
        var articleId = $"article_{claimType}";
        var token = JwtTokenHelper.GenerateToken(userId, claimType);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Test",
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateComment_ExactlyRating1_Success()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = "Mal producto",
            Rating = 1
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<CommentResponse>();
        result!.Rating.Should().Be(1);
    }

    [Fact]
    public async Task CreateComment_Exactly500Characters_Success()
    {
        // Arrange
        var userId = "user123";
        var articleId = "article456";
        var token = JwtTokenHelper.GenerateToken(userId);

        _factory.CatalogService.AddArticle(articleId);
        _factory.OrderService.AddUserPurchasedArticle(userId, articleId);
        _factory.OrderService.SetUserOrders(userId, new List<Order>
        {
            new Order { Id = "order1", Status = "validated" }
        });

        var request = new CreateCommentRequest
        {
            ArticleId = articleId,
            Comment = new string('A', 500), // Exactly 500
            Rating = 5
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/comments", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

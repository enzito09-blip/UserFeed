using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace UserFeed.Tests.Helpers;

public static class JwtTokenHelper
{
    public static string GenerateToken(string userId, string? claimType = null)
    {
        var claims = new List<Claim>();
        
        // Use specified claim type or default to userId
        var actualClaimType = claimType ?? "userId";
        claims.Add(new Claim(actualClaimType, userId));

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1)
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    public static string GenerateTokenWithMultipleClaims(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim("userId", userId),
            new Claim("sub", userId),
            new Claim("name", "Test User")
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1)
        );

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }
}

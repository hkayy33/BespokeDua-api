using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BespokeDuaApi.Auth;

public static class AppAuthenticationExtensions
{
    /// <summary>Default scheme: routes numeric bearer tokens to legacy auth, JWTs to Supabase.</summary>
    public const string CombinedScheme = "Combined";

    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var authBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CombinedScheme;
                options.DefaultChallengeScheme = CombinedScheme;
            })
            .AddPolicyScheme(CombinedScheme, CombinedScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    if (string.IsNullOrEmpty(authHeader) ||
                        !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return SupabaseAuthExtensions.SchemeName;
                    }

                    var token = authHeader["Bearer ".Length..].Trim();
                    if (int.TryParse(token, out _))
                    {
                        return LegacyUserIdBearerAuthenticationHandler.SchemeName;
                    }

                    return SupabaseAuthExtensions.SchemeName;
                };
            })
            .AddScheme<AuthenticationSchemeOptions, LegacyUserIdBearerAuthenticationHandler>(
                LegacyUserIdBearerAuthenticationHandler.SchemeName,
                _ => { });

        var supabaseUrl = (configuration["Supabase:Url"] ?? string.Empty).TrimEnd('/');
        var jwtSecret = configuration["Supabase:JwtSecret"];

        if (!string.IsNullOrWhiteSpace(supabaseUrl))
        {
            authBuilder.AddJwtBearer(SupabaseAuthExtensions.SchemeName, options =>
            {
                options.MapInboundClaims = false;

                // New Supabase projects sign user JWTs with ES256/RS256; public keys come from JWKS.
                options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                };

                // Legacy projects still on symmetric HS256 JWT secret (not sb_secret_* API keys).
                if (IsLegacyJwtSecret(jwtSecret))
                {
                    var symmetricKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret!));
                    options.TokenValidationParameters.ValidAlgorithms =
                    [
                        SecurityAlgorithms.EcdsaSha256,
                        SecurityAlgorithms.RsaSha256,
                        SecurityAlgorithms.HmacSha256,
                    ];
                    options.TokenValidationParameters.IssuerSigningKey = symmetricKey;
                }
            });
        }

        return services;
    }

    private static bool IsLegacyJwtSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !value.StartsWith("sb_secret_", StringComparison.OrdinalIgnoreCase) &&
        !value.StartsWith("sb_publishable_", StringComparison.OrdinalIgnoreCase);
}

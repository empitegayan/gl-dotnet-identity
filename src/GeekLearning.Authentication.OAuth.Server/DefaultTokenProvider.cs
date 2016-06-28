﻿namespace GeekLearning.Authentication.OAuth.Server
{
    using Microsoft.Extensions.Options;
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using Microsoft.Extensions.Primitives;
    using Microsoft.IdentityModel.Tokens;
    using System.Collections.Generic;
    using Internal;
    using System.Threading.Tasks;
    public class DefaultTokenProvider : ITokenProvider
    {
        private IOptions<OAuthServerOptions> options;
        private IClientProvider clientProvider;

        public DefaultTokenProvider(IOptions<OAuthServerOptions> options, IClientProvider clientProvider)
        {
            this.options = options;
            this.clientProvider = clientProvider;
        }

        public IToken GenerateAccessToken(ClaimsIdentity identity, IEnumerable<string> audiences)
        {
            var tokenId = Guid.NewGuid().ToString("N");
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = this.options.Value.Keys.First();

            var tokenClaimsIdentity = identity.Clone();
            tokenClaimsIdentity.AddClaims(new Claim[]
            {
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_id", tokenId),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_usage", "access_token"),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/keyid", signingKey.Key),
            });

            var token = tokenHandler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Audience = string.Join(",", audiences),
                Expires = (DateTime.Now + this.options.Value.AccesssTokenLifetime),
                NotBefore = DateTime.Now.AddMinutes(-1),
                IssuedAt = DateTime.Now,
                Issuer = this.options.Value.Issuer,
                SigningCredentials = signingKey.Value,
                Subject = tokenClaimsIdentity
            });

            return tokenHandler.GetAccessToken(tokenId, token);
        }

        public IToken GenerateAuthorizationToken(AuthorizationRequest request, ClaimsPrincipal identity)
        {
            if (request.Response_Type != "code")
            {
                throw new System.Security.SecurityException("Oauth request ResponseType MUST be \"code\"");
            }

            var tokenId = Guid.NewGuid().ToString("N");
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = this.options.Value.Keys.First();
            var token = tokenHandler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Audience = this.options.Value.Issuer,
                Expires = (DateTime.Now + this.options.Value.AuthorizationCodeLifetime),
                NotBefore = DateTime.Now.AddMinutes(-1),
                IssuedAt = DateTime.Now,
                Issuer = this.options.Value.Issuer,
                SigningCredentials = signingKey.Value,
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_id", tokenId),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_usage", "authorization_code"),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/keyid", signingKey.Key),
                    identity.FindFirst(ClaimTypes.NameIdentifier)
                })
            });

            return tokenHandler.GetAuthorizationCode(tokenId, token);
        }

        private static bool ValidateLifetime(DateTime? notBefore, DateTime? expires, SecurityToken securityToken, TokenValidationParameters validationParameters)
        {
            if (!notBefore.HasValue || !expires.HasValue)
            {
                return false;
            }
            var now = DateTimeOffset.UtcNow;
            return now - validationParameters.ClockSkew < new DateTimeOffset(expires.Value, TimeSpan.Zero)
                && now + validationParameters.ClockSkew > new DateTimeOffset(notBefore.Value, TimeSpan.Zero);
        }


        public ITokenValidationResult ValidateAuthorizationCode(string code)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            Microsoft.IdentityModel.Tokens.SecurityToken token;
            try
            {
                var claimsPrincipal = tokenHandler.ValidateToken(code, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = this.options.Value.Issuer,
                    ValidAudience = this.options.Value.Issuer,
                    IssuerSigningKeys = this.options.Value.Keys.Values.Select(x => x.Key)
                }, out token);
                return new TokenValidationResult
                {
                    Success = true,
                    NameIdentifier = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value
                };
            }
            catch
            {
                return new TokenValidationResult
                {
                    Success = false
                };
            }

        }

        public ITokenValidationResult ValidateRefreshToken(string refreshToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            Microsoft.IdentityModel.Tokens.SecurityToken token;
            try
            {
                var claimsPrincipal = tokenHandler.ValidateToken(refreshToken, new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidIssuer = this.options.Value.Issuer,
                    ValidAudience = this.options.Value.Issuer,
                    IssuerSigningKeys = this.options.Value.Keys.Values.Select(x => x.Key)
                }, out token);
                return new TokenValidationResult
                {
                    Success = true,
                    NameIdentifier = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value
                };
            }
            catch
            {
                return new TokenValidationResult
                {
                    Success = false
                };
            }

        }

        public IToken GenerateRefreshToken(ClaimsIdentity identity, IEnumerable<string> audiences)
        {
            var tokenId = Guid.NewGuid().ToString("N");
            var tokenHandler = new JwtSecurityTokenHandler();
            var signingKey = this.options.Value.Keys.First();
            var token = tokenHandler.CreateToken(new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
            {
                Audience = this.options.Value.Issuer,
                Expires = (DateTime.Now + this.options.Value.RefreshTokenLifetime),
                NotBefore = DateTime.Now.AddMinutes(-1),
                IssuedAt = DateTime.Now,
                Issuer = this.options.Value.Issuer,
                SigningCredentials = signingKey.Value,
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_id", tokenId),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/token_usage", "refresh_token"),
                    new Claim("https://claims.schemas.geeklearning.io/oauth2/keyid", signingKey.Key),
                    identity.FindFirst(ClaimTypes.NameIdentifier)
                })
            });

            return tokenHandler.GetRefreshToken(tokenId, token);
        }

        public class TokenValidationResult : ITokenValidationResult
        {
            public string NameIdentifier { get; set; }

            public bool Success { get; set; }
        }
    }
}

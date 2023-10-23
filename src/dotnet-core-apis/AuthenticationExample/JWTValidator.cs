using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AuthorizationExample
{
    internal class JWTValidator
    {
        public static async Task<ClaimsPrincipal> ValidateTokenAsync(string jwtToken, string audience, string issuer)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    throw new Exception("Missing identity bearer token");
                }

                jwtToken = jwtToken.Replace("Bearer", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

                var metadataEndpoint = $"{issuer}/.well-known/openid-configuration";

                var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(metadataEndpoint, new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever());
                var discoveryDocument = await configurationManager.GetConfigurationAsync();
                var signingKeys = discoveryDocument.SigningKeys;

                var tokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateAudience = true,
                    ValidAudiences = new string[] { audience }, // list all audiences
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = signingKeys,
                    RoleClaimType = "cognito:groups"
                };

                SecurityToken validatedToken = new JwtSecurityToken();

                var tokenHandler = new JwtSecurityTokenHandler();

                return tokenHandler.ValidateToken(jwtToken, tokenValidationParameters, out validatedToken);
            }
            catch
            {
                throw;
            }
        }

    }
}

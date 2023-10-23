using CustomScopesAuthorizationExample.CognitoHelpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// add authorization policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ReadPhotos", policy => policy.Requirements.Add(new CognitoScopeAuthorizationRequirement(new[] { "com.example.photos/basic", "com.example.photos/read" })));
    options.AddPolicy("WritePhotos", policy => policy.Requirements.Add(new CognitoScopeAuthorizationRequirement(new[] { "com.example.photos/basic", "com.example.photos/write" })));
});


// read configurations
string[] allowedDomains = builder.Configuration["AppSettings:AllowedOrigions"].Split(",");
string cognitoAppClientId = builder.Configuration["AppSettings:Cognito:AppClientId"].ToString();
string cognitoUserPoolId = builder.Configuration["AppSettings:Cognito:UserPoolId"].ToString();
string cognitoAWSRegion = builder.Configuration["AppSettings:Cognito:AWSRegion"].ToString();

string validIssuer = $"https://cognito-idp.{cognitoAWSRegion}.amazonaws.com/{cognitoUserPoolId}";
string validAudience = cognitoAppClientId;

// Add services to the container.

builder.Services.AddControllers();

// Add AWS Lambda support.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

// Configure CORS
builder.Services.AddCors(item =>
{
    item.AddPolicy("CORSPolicy", builder =>
    {
        builder.WithOrigins(allowedDomains)
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});



// Register authentication schemes, and specify the default authentication scheme
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        // Note: Authority is the address of the token-issuing authentication server.
        // The JWT bearer authentication middleware will use this URI to find and retrieve the public key that can be used to validate the token’s signature.
        // It will also confirm that the iss parameter in the token matches this URI. Hence, we don't need to specify ValidateIssuer explicitly in TokenValidationParameters.
        // See https://devblogs.microsoft.com/dotnet/jwt-validation-and-authorization-in-asp-net-core/

        options.Authority = validIssuer;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            // Note: Amazon Cognito returns audience "aud" field in ID Token, but it doesn't return it in the Access Token.
            // Instead the audience is set in "client_id" field in Access Token. So, you need to manually validate the audience.
            // Secondly, if AudienceValidator delegate is defined, it will be called even if ValidateAudience is set to false.
            AudienceValidator = (audiences, securityToken, validationParameters) =>
            {
                var castedToken = securityToken as JwtSecurityToken;
                var clientId = castedToken?.Payload["client_id"]?.ToString();
                return validAudience.Equals(clientId);
            },
            RoleClaimType = "cognito:groups"
        };
    });

// add a singleton of our cognito authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, CognitoScopeAuthorizationHandler>();

var app = builder.Build();

app.UsePathBase("/aspnet/custom-scopes-authorization-example");
app.UseRouting();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseCors("CORSPolicy");

app.UseAuthentication(); // resposible for constructing AuthenticationTicket objects representing the user's identity
app.UseAuthorization();

app.MapControllers();

app.Run();

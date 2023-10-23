using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AuthorizationExample;

public class Function
{
    private readonly IConfigurationRoot _configurations;
    private readonly string _validIssuer;
    private readonly string _validAudience;

    public Function()
    {
        _configurations = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.json", optional: true)
             .AddEnvironmentVariables()
             .Build();

        string allowedOrigins = _configurations["AppSettings:AllowedOrigions"];
        string appClientId = _configurations["AppSettings:Cognito:AppClientId"];
        string cognitoUserPoolId = _configurations["AppSettings:Cognito:UserPoolId"];
        string cognitoAWSRegion = _configurations["AppSettings:Cognito:AWSRegion"];

        _validIssuer = $"https://cognito-idp.{cognitoAWSRegion}.amazonaws.com/{cognitoUserPoolId}";
        _validAudience = appClientId;
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {
        // 1. Return if no bearer token found
        if (string.IsNullOrWhiteSpace(apigProxyEvent.Headers["Authorization"]))
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized
            };
        }

        // 2. Manually validate the tokena and obtain claim principal
        string bearerToken = apigProxyEvent.Headers["Authorization"];
        ClaimsPrincipal claimPrincipal = await JWTValidator.ValidateTokenAsync(bearerToken, _validAudience, _validIssuer);

        string name = claimPrincipal.Identity!.Name!;
        bool isAdminUser = claimPrincipal.IsInRole("Admin");
        var claims = claimPrincipal.Claims.Select(item => new KeyValuePair<string, string>(item.Type, item.Value)).ToList();

        // 3. Check for Admin role
        if (!isAdminUser)
        {
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden
            };
        }

        // 4. If all good, then do your stuff
        var sampleResponse = new { Name = "Ankush Jain", Country = "India" };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(sampleResponse)
        };
    }
}

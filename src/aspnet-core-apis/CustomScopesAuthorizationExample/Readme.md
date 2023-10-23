# Create and Deploy an ASP.NET Core Web API to AWS Lambda with Cognito Authentication & Authorization using Custom Scopes
To create and deploy an ASP.NET Core application that uses **Minimal APIs** or **top-level staements** to Lambda:

## Pre-requisite
1. Visual Studio
2. AWS Toolkit for Visual Studio
3. .NET 6
4. **Cognito user pool** with **App Client**
5. **Resource Server** with **Custom Scopes**	
> You don't have to manually create the above required resources, as CDK IaC code creates necessary resources for you.

## What is a Resource Server?
The [resource server](https://www.oauth.com/oauth2-servers/the-resource-server/) is the OAuth 2.0 term for your API server.
> Large scale deployments may have more than one resource server. Google’s services, for example, have dozens of resource servers, such as the Google Cloud platform, Google Maps, Google Drive, Youtube, Google+, and many others. Each of these resource servers are distinctly separate, but they all share the same authorization server.

## Steps to follow
1. Create new application using **ASP.NET Core Web API** template.
2. Install `Amazon.Lambda.AspNetCoreServer.Hosting` NuGet package.
3. Add a call to `AddAWSLambdaHosting` in your application when the services are being defined for the application.
	```cs
	// Add AWS Lambda support.
	builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
	.
	.
	var app = builder.Build();
	```
4. Install `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package.
5. Add the following settings in `appsettings.json` file.
	> Please note that, when you deploy the solution using CDK, these configurations are kept inside Lambda function environment variables. So you may not need to update it manually during deployment. The `appsettings.json` will be useful when running the application locally.
	```json
	{
	  .
	  .
	  "AppSettings": {
		"AllowedOrigions": "http://localhost:80", //change it as per your requirement, this should have a comma values
		"Cognito": {
		  "AppClientId": "",
		  "UserPoolId": "",
		  "AWSRegion": ""
		}
	  }
	}
	```
6. To enable authorization using custom scopes, add the following authorization policies.
	```cs
	var builder = WebApplication.CreateBuilder(args);

	// add authorization policy
	builder.Services.AddAuthorization(options =>
	{
		options.AddPolicy("ReadPhotos", policy => policy.Requirements.Add(new CognitoScopeAuthorizationRequirement(new[] { "com.example.photos/basic", "com.example.photos/read" })));
		options.AddPolicy("WritePhotos", policy => policy.Requirements.Add(new CognitoScopeAuthorizationRequirement(new[] { "com.example.photos/basic", "com.example.photos/write" })));
	});
	```

7. Add the below code after `WebApplicationBuilder` in your `Program.cs` file to read the configurations.
	```cs
	var builder = WebApplication.CreateBuilder(args);

	// read configurations
	string[] allowedDomains = builder.Configuration["AppSettings:AllowedOrigions"].Split(",");
	string cognitoAppClientId = builder.Configuration["AppSettings:Cognito:AppClientId"].ToString();
	string cognitoUserPoolId = builder.Configuration["AppSettings:Cognito:UserPoolId"].ToString();
	string cognitoAWSRegion = builder.Configuration["AppSettings:Cognito:AWSRegion"].ToString();

	string validIssuer = $"https://cognito-idp.{cognitoAWSRegion}.amazonaws.com/{cognitoUserPoolId}";
	string validAudience = cognitoAppClientId;
	```
7. Also, add the the following code in your `Program.cs` file to register necessary services.
	```cs
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
			options.Authority = validIssuer;
			options.TokenValidationParameters = new TokenValidationParameters
			{
				ValidateLifetime = true,
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
	```
8. Optionally, add the following line if the API is exposed through a child route in API Gateway.
    ```cs
    app.UsePathBase("/aspnet/custom-scopes-authorization-example");
    app.UseRouting();
    ```

9. Add **CORS** and authentication middlewares. 
    ```cs
    app.UseCors("CORSPolicy");

    app.UseAuthentication(); // resposible for constructing AuthenticationTicket objects representing the user's identity
    app.UseAuthorization();
    ```
    Note that authentication process is handled by the authentication middleware that we register using the `app.UseAuthentication()` code.
10. Create a `PhotoController` as shown below for the testing purpose.
    ```cs
	[Authorize]
	[ApiController]
	[Route("[controller]")]
	public class PhotoController : ControllerBase
	{

		[HttpGet]
		[Authorize(Policy = "ReadPhotos")]
		public IActionResult Get()
		{
			return Ok();
		}

		[HttpPost]
		[Authorize(Policy = "WritePhotos")]
		public IActionResult Post()
		{
			return Ok();
		}
	}  
    ```

11.  Open `.csproj` file and add `<AWSProjectType>Lambda</AWSProjectType>` in PropertyGroup section. This will let **AWS Toolkit for Visual Studio** know that your project is an **AWS project**. 
  
     Now, when you right-click on your project, you will start getting **Publish to AWS...** option, that you can deploy Lambda function from Visual Studio.


## How to deploy
Refer **How to deploy** section on root README file.

## How to test
Refer **How to test** section on root README file.

## IMPORTANT: Consuming APIs with Custom Scopes
> With the COGNITO_USER_POOLS authorizer, if the OAuth Scopes option isn't specified, API Gateway treats the supplied token as an identity token and verifies the claimed identity against the one from the user pool. Otherwise, API Gateway treats the supplied token as an access token and verifies the access scopes that are claimed in the token against the authorization scopes declared on the method.

Because we're running an ASP.NET Core API in a Single Lambda Function that has multiple endpoints inside it, and each endpoint may require different OAuth scopes. We can't make all the scopes mandatory at the API Gateway level on a single route.

As a workaround, we made one scope required at the API Gateway level, while managing fine-grained access at the application level.
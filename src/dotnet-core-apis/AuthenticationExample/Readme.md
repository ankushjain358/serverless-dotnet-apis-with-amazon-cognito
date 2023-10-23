# Create a .NET Core Lambda Function - Cognito Authentication & Authorization
To create a .NET Core Lambda function as an API endpoint:

## Pre-requisite
1. Visual Studio
2. AWS Toolkit for Visual Studio
3. .NET 6

## Steps to follow
1. Create new project using **AWS Lambda Project (.NET Core - C#)** template.
2. Select **Empty Function** from the blueprints dialog.
3. Install `Amazon.Lambda.APIGatewayEvents` NuGet package.
4. Also, install the following NuGet packages to configure Configurations and Dependecny Injection.
    ````xml
    <!-- Packages required for Configuration -->
    <PackageReference Include="Microsoft.Extensions.Configuration" Version="6.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.EnvironmentVariables" Version="6.0.1" />
    <PackageReference Include="Microsoft.Extensions.Configuration.FileExtensions" Version="6.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="6.0.0" />

    <!-- Packages required for JWT validation -->
    <PackageReference Include="Microsoft.IdentityModel.Protocols.OpenIdConnect" Version="6.10.0" />
    <PackageReference Include="System.Configuration.ConfigurationManager" Version="7.0.0" />
    <PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="6.10.0" />
    ```

4. Update you function as per `Function.cs` class.
5. Refer `JWTValidator` class to understand how to validate the Cognito JWT token manually.
6. Optionally, to deploy Lambda function from Visual Studio, right click on the project, select **Publish to AWS...** option, and follow the wizard steps.

## How to deploy
Refer **How to deploy** section on root README file.

## How to test
Refer **How to test** section on root README file.

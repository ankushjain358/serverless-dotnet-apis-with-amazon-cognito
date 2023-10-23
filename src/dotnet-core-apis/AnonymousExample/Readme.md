# Create a .NET Core Lambda Function - No authentication
To create a .NET Core Lambda function as an API endpoint:

## Pre-requisite
1. Visual Studio
2. AWS Toolkit for Visual Studio
3. .NET 6

## Steps to follow
1. Create new project using **AWS Lambda Project (.NET Core - C#)** template.
2. Select **Empty Function** from the blueprints dialog.
3. Install `Amazon.Lambda.APIGatewayEvents` NuGet package.
4. Update you function as per below code.
    ```cs
    public APIGatewayProxyResponse FunctionHandler(APIGatewayProxyRequest apigProxyEvent, ILambdaContext context)
    {

        var sampleResponse = new { Name = "Änkush Jain", Country = "Ïndia" };

        return new APIGatewayProxyResponse
        {
            StatusCode = (int)HttpStatusCode.OK,
            Body = JsonSerializer.Serialize(sampleResponse)
        };
    }
    ```
3. Optionally, to deploy Lambda function from Visual Studio, right click on the project, select **Publish to AWS...** option, and follow the wizard steps.

## How to deploy
Refer **How to deploy** section on root README file.

## How to test
Refer **How to test** section on root README file.

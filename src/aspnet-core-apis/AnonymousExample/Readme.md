# Create and Deploy an ASP.NET Core Web API on AWS Lambda - No authentication
To create and deploy an ASP.NET Core application that uses **Minimal APIs** or **top-level staements** to Lambda:

## Pre-requisite
1. Visual Studio
2. AWS Toolkit for Visual Studio
3. .NET 6

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
4. Optionally, add the following line if the API is exposed through a child route in API Gateway.
	```cs
	app.UsePathBase("/aspnet/anonymous-example");
	app.UseRouting();
	```
5. Open `.csproj` file and add `<AWSProjectType>Lambda</AWSProjectType>` in PropertyGroup section. This will let `**AWS Toolkit for Visual Studio** know that your project is an **AWS project**.

Now, when you right-click on your project, you will start getting Publish to AWS... option, that you can deploy Lambda function from Visual Studio.

For more detail, refer [Introducing the .NET 6 runtime for AWS Lambda](https://aws.amazon.com/blogs/compute/introducing-the-net-6-runtime-for-aws-lambda/)

## How to deploy
Refer **How to deploy** section on root README file.

## How to test
Refer **How to test** section on root README file.

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using System;
using System.Collections.Generic;
using AssetOptions = Amazon.CDK.AWS.S3.Assets.AssetOptions;

namespace Cdk
{
    public class CdkStack : Stack
    {
        private const string ASPNETCoreWebAPI_AnonymousExample = "AnonymousExample";
        private const string ASPNETCoreWebAPI_AuthenticationExample = "AuthenticationExample";
        private const string ASPNETCoreWebAPI_AuthorizationExample = "AuthorizationExample";
        private const string ASPNETCoreWebAPI_CustomScopesAuthorizationExample = "CustomScopesAuthorizationExample";
        private const string DotNetCoreAPI_AnonymousExample = "AnonymousExample";
        private const string DotNetCoreAPI_AuthorizationExample = "AuthorizationExample";

        // Provide a value which you think is unique for the userpool i.e. https://<domain-prefix>.auth.<region>.amazoncognito.com
        string COGNITO_USERPOOL_DOMAIN_PREFIX = "";
        string COGNITO_USERPOOL_NAME = "dotnet-demo-userpool";
        string RESOURCE_SERVER_IDENTIFIER = "com.example.photos";
        string ALLOWED_ORIGINS = ""; // comma seperated list of origins for CORS purpose
        string CALLBACK_URL = "https://www.thunderclient.com/oauth/callback"; // Assuming, you will use ThunderClient to test this API

        bool COMPILE_ON_CDK_DEPLOY = false;

        internal CdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            if (string.IsNullOrWhiteSpace(COGNITO_USERPOOL_DOMAIN_PREFIX))
            {
                throw new Exception("COGNITO_USERPOOL_DOMAIN_PREFIX value can not be empty.");
            }

            // Lambda Function Build Commands
            var buildCommands = new[]
            {
                "rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm",
                "yum -y install aspnetcore-runtime-6.0",
                "cd /asset-input",
                "export DOTNET_CLI_HOME=\"/tmp/DOTNET_CLI_HOME\"",
                "export PATH=\"$PATH:/tmp/DOTNET_CLI_HOME/.dotnet/tools\"",
                "dotnet tool install -g Amazon.Lambda.Tools",
                "dotnet lambda package -o output.zip",
                "unzip -o -d /asset-output output.zip"
            };

            var assetOptions = new AssetOptions
            {
                // Note: Asset path should point to the folder where .csproj file is present. Also,this path should be relative to cdk.json
                Bundling = new BundlingOptions
                {
                    Image = Runtime.DOTNET_6.BundlingImage,
                    Command = new[]
                        {
                            "bash", "-c", string.Join(" && ", buildCommands)
                        },
                    User = "root"
                }
            };

            #region Cognito userpool

            var cognitoUserPool = new UserPool(this, "user-pool", new UserPoolProps
            {
                UserPoolName = COGNITO_USERPOOL_NAME,
                SignInAliases = new SignInAliases
                {
                    Email = true,
                    Username = false,
                    PreferredUsername = false,
                    Phone = false
                },
                SelfSignUpEnabled = true,
                StandardAttributes = new StandardAttributes
                {
                    Fullname = new StandardAttribute
                    {
                        Required = true,
                        Mutable = true
                    },
                    Email = new StandardAttribute
                    {
                        Required = true,
                        Mutable = true
                    },
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
            });

            cognitoUserPool.AddDomain("cognito-domain", new UserPoolDomainOptions
            {
                CognitoDomain = new CognitoDomainOptions
                {
                    DomainPrefix = COGNITO_USERPOOL_DOMAIN_PREFIX
                }
            });

            var apiResourceServer = cognitoUserPool.AddResourceServer("photos-api-resource-server", new UserPoolResourceServerOptions
            {
                Identifier = RESOURCE_SERVER_IDENTIFIER,
                Scopes = new ResourceServerScope[]
                {
                    new ResourceServerScope(new ResourceServerScopeProps
                    {
                        ScopeName="basic",
                        ScopeDescription="Provides basic access to the API"
                    }),
                    new ResourceServerScope(new ResourceServerScopeProps
                    {
                        ScopeName="read",
                        ScopeDescription="Provides read access to the photos"
                    }),
                    new ResourceServerScope(new ResourceServerScopeProps
                    {
                        ScopeName="write",
                        ScopeDescription="Provides write access to the photos"
                    })
                }
            });

            var userPoolClient = new UserPoolClient(this, "public-app-client", new UserPoolClientProps
            {
                GenerateSecret = true,
                OAuth = new OAuthSettings
                {
                    CallbackUrls = new string[]
                    {
                        CALLBACK_URL
                    },
                    Flows = new OAuthFlows
                    {
                        AuthorizationCodeGrant = true,
                        ClientCredentials = false,
                        ImplicitCodeGrant = false
                    },
                    Scopes = new OAuthScope[]
                    {
                        OAuthScope.COGNITO_ADMIN,
                        OAuthScope.EMAIL,
                        OAuthScope.PROFILE,
                        OAuthScope.OPENID,
                        OAuthScope.PHONE,
                        OAuthScope.Custom($"{RESOURCE_SERVER_IDENTIFIER}/basic"),
                        OAuthScope.Custom($"{RESOURCE_SERVER_IDENTIFIER}/read"),
                        OAuthScope.Custom($"{RESOURCE_SERVER_IDENTIFIER}/write")
                    },
                },
                UserPool = cognitoUserPool,
                SupportedIdentityProviders = new[] { UserPoolClientIdentityProvider.COGNITO },
            });

            userPoolClient.Node.AddDependency(apiResourceServer);
            #endregion

            #region ASP.NET Core API - Lambda functions

            // ASP.NET Core API - Lambda function Anonymous Example
            var aspnetCoreAnonymousExample = new Function(this, nameof(ASPNETCoreWebAPI_AnonymousExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = ASPNETCoreWebAPI_AnonymousExample,
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AnonymousExample}", assetOptions)
                    : Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AnonymousExample}/output.zip"),
                Environment = new Dictionary<string, string>()
                {
                    {"AppSettings__AllowedOrigions", ALLOWED_ORIGINS},
                    {"AppSettings__Cognito__AppClientId", userPoolClient.UserPoolClientId},
                    {"AppSettings__Cognito__UserPoolId", cognitoUserPool.UserPoolId},
                    {"AppSettings__Cognito__AWSRegion", Of(this).Region}
                },
                Description = "ASP.NET Core API - Anonymous example"
            });

            // ASP.NET Core API - Lambda function Authentication Example
            var aspnetCoreAuthenticationExample = new Function(this, nameof(ASPNETCoreWebAPI_AuthenticationExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = ASPNETCoreWebAPI_AuthenticationExample,
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AuthenticationExample}", assetOptions)
                    : Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AuthenticationExample}/output.zip"),
                Environment = new Dictionary<string, string>()
                {
                    {"AppSettings__AllowedOrigions", ALLOWED_ORIGINS},
                    {"AppSettings__Cognito__AppClientId", userPoolClient.UserPoolClientId},
                    {"AppSettings__Cognito__UserPoolId", cognitoUserPool.UserPoolId},
                    {"AppSettings__Cognito__AWSRegion", Of(this).Region}
                },
                Description = "ASP.NET Core API - Authentication example"
            });

            // ASP.NET Core API - Lambda function Role Based Authorization Example
            var aspnetCoreRoleBasedAuthorizationExample = new Function(this, nameof(ASPNETCoreWebAPI_AuthorizationExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = ASPNETCoreWebAPI_AuthorizationExample,
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AuthorizationExample}", assetOptions)
                    : Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_AuthorizationExample}/output.zip"),
                Environment = new Dictionary<string, string>()
                {
                    {"AppSettings__AllowedOrigions", ALLOWED_ORIGINS},
                    {"AppSettings__Cognito__AppClientId", userPoolClient.UserPoolClientId},
                    {"AppSettings__Cognito__UserPoolId", cognitoUserPool.UserPoolId},
                    {"AppSettings__Cognito__AWSRegion", Of(this).Region}
                },
                Description = "ASP.NET Core API - Role based Authorization example"
            });

            // ASP.NET Core API - Lambda function Custom Scopes Authorization Example
            var aspnetCoreCustomScopesAuthorizationExample = new Function(this, nameof(ASPNETCoreWebAPI_CustomScopesAuthorizationExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = ASPNETCoreWebAPI_CustomScopesAuthorizationExample,
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_CustomScopesAuthorizationExample}", assetOptions)
                    : Code.FromAsset($"../src/aspnet-core-apis/{ASPNETCoreWebAPI_CustomScopesAuthorizationExample}/output.zip"),
                Environment = new Dictionary<string, string>()
                {
                    {"AppSettings__AllowedOrigions", ALLOWED_ORIGINS},
                    {"AppSettings__Cognito__AppClientId", userPoolClient.UserPoolClientId},
                    {"AppSettings__Cognito__UserPoolId", cognitoUserPool.UserPoolId},
                    {"AppSettings__Cognito__AWSRegion", Of(this).Region}
                },
                Description = "ASP.NET Core API - Custom Scopes Authorization example"
            });

            #endregion

            #region .NET Core API - Lambda functions

            // .NET Core API - Lambda function Anonymous Example
            var dotnetCoreAnonymousExample = new Function(this, nameof(DotNetCoreAPI_AnonymousExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = $"{DotNetCoreAPI_AnonymousExample}::{DotNetCoreAPI_AnonymousExample}.Function::FunctionHandler",
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/dotnet-core-apis/{DotNetCoreAPI_AnonymousExample}", assetOptions)
                    : Code.FromAsset($"../src/dotnet-core-apis/{DotNetCoreAPI_AnonymousExample}/output.zip"),
                Description = "Non-ASP.NET Core API - Anonymous example"
            });

            // .NET Core API - Lambda function Authorization example
            var dotnetCoreAuthorizationExample = new Function(this, nameof(DotNetCoreAPI_AuthorizationExample), new FunctionProps
            {
                MemorySize = 512,
                Timeout = Duration.Seconds(30),
                Runtime = Runtime.DOTNET_6,
                Handler = $"{DotNetCoreAPI_AuthorizationExample}::{DotNetCoreAPI_AuthorizationExample}.Function::FunctionHandler",
                Code = COMPILE_ON_CDK_DEPLOY
                    ? Code.FromAsset($"../src/dotnet-core-apis/{DotNetCoreAPI_AuthorizationExample}", assetOptions)
                    : Code.FromAsset($"../src/dotnet-core-apis/{DotNetCoreAPI_AuthorizationExample}/output.zip"),
                Environment = new Dictionary<string, string>()
                {
                    {"AppSettings__AllowedOrigions", ALLOWED_ORIGINS},
                    {"AppSettings__Cognito__AppClientId", userPoolClient.UserPoolClientId},
                    {"AppSettings__Cognito__UserPoolId", cognitoUserPool.UserPoolId},
                    {"AppSettings__Cognito__AWSRegion", Of(this).Region}
                },
                Description = "Non-ASP.NET Core API - Authorization example"
            });

            #endregion

            #region API Gateway REST API with Cognito Authorizer

            // Cognito Authorizer
            var authorizer = new CognitoUserPoolsAuthorizer(this, "authorizer", new CognitoUserPoolsAuthorizerProps
            {
                CognitoUserPools = new IUserPool[] { cognitoUserPool },
            });

            var restApi = new RestApi(this, "dotnet-rest-api",
                new RestApiProps { Description = "REST API Lambda integrations with Amazon Cognito user pool" });

            var aspnetResourceRoute = restApi.Root
                 .AddResource("aspnet");

            aspnetResourceRoute
                .AddResource("anonymous-example")
                .AddProxy(new ProxyResourceOptions
                {
                    AnyMethod = true,
                    DefaultIntegration = new LambdaIntegration(aspnetCoreAnonymousExample)
                });

            aspnetResourceRoute
                .AddResource("authentication-example")
                .AddProxy(new ProxyResourceOptions
                {
                    AnyMethod = true,
                    DefaultIntegration = new LambdaIntegration(aspnetCoreAuthenticationExample),
                    DefaultMethodOptions = new MethodOptions
                    {
                        AuthorizationType = AuthorizationType.COGNITO,
                        Authorizer = authorizer,
                    }
                });

            aspnetResourceRoute
                .AddResource("authorization-example")
                .AddProxy(new ProxyResourceOptions
                {
                    AnyMethod = true,
                    DefaultIntegration = new LambdaIntegration(aspnetCoreRoleBasedAuthorizationExample),
                    DefaultMethodOptions = new MethodOptions
                    {
                        AuthorizationType = AuthorizationType.COGNITO,
                        Authorizer = authorizer,
                    }
                });

            aspnetResourceRoute
                .AddResource("custom-scopes-authorization-example")
                .AddProxy(new ProxyResourceOptions
                {
                    AnyMethod = true,
                    DefaultIntegration = new LambdaIntegration(aspnetCoreCustomScopesAuthorizationExample),
                    DefaultMethodOptions = new MethodOptions
                    {
                        AuthorizationType = AuthorizationType.COGNITO,
                        Authorizer = authorizer,
                        AuthorizationScopes = new[] { $"{RESOURCE_SERVER_IDENTIFIER}/basic" }
                    }
                });

            var dotnetResourceRoute = restApi.Root
               .AddResource("dotnet");

            dotnetResourceRoute
                .AddResource("anonymous-example")
                .AddMethod("ANY", new LambdaIntegration(dotnetCoreAnonymousExample, new LambdaIntegrationOptions
                {
                    Proxy = true
                }));

            dotnetResourceRoute
              .AddResource("authorization-example")
              .AddMethod("ANY", new LambdaIntegration(dotnetCoreAuthorizationExample, new LambdaIntegrationOptions
              {
                  Proxy = true
              }),
                new MethodOptions
                {
                    AuthorizationType = AuthorizationType.COGNITO,
                    Authorizer = authorizer,
                });

            #endregion


            // Output the API Gateway REST API Url
            new CfnOutput(this, "REST API Url", new CfnOutputProps
            {
                Value = restApi.Url,
                Description = "REST API endpoint URL"
            });
        }
    }
}
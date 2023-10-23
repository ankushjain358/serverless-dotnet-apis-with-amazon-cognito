using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CustomScopesAuthorizationExample.CognitoHelpers
{
    public class CognitoScopeAuthorizationHandler : AuthorizationHandler<CognitoScopeAuthorizationRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CognitoScopeAuthorizationRequirement requirement)
        {
            bool success = false;
            Claim? scopeClaim = context.User.Claims.FirstOrDefault(item => item.Type.Equals("scope", StringComparison.OrdinalIgnoreCase));

            if (scopeClaim != null && !string.IsNullOrWhiteSpace(scopeClaim.Value))
            {
                List<string> availableScopes = scopeClaim.Value.Trim().Split(" ").ToList();

                success = requirement.RequiredScopes.All(item => availableScopes.Contains(item, StringComparer.OrdinalIgnoreCase));
            }

            if (success)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }

    }

}

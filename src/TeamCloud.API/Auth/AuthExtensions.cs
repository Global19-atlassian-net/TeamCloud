/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TeamCloud.API.Services;
using TeamCloud.Data;
using TeamCloud.Model.Data;

namespace TeamCloud.API.Auth
{
    internal static class AuthExtensions
    {

        internal static IServiceCollection AddTeamCloudAuthorization(this IServiceCollection services)
        {
            services
                .AddAuthorization(options =>
                {
                    options.AddPolicy(AuthPolicies.Default, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                    });

                    options.AddPolicy(AuthPolicies.Admin, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.UserRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           UserRolePolicies.UserReadPolicy,
                                           UserRolePolicies.UserWritePolicy);
                    });

                    options.AddPolicy(AuthPolicies.UserWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           UserRolePolicies.UserWritePolicy);
                    });

                    // options.AddPolicy(AuthPolicies.ProjectUserRead, policy =>
                    // {
                    //     policy.RequireRole(TeamCloudUserRole.Admin.PolicyRoleName(),
                    //                        ProjectUserRole.Owner.PolicyRoleName(),
                    //                        UserRolePolicies.UserReadPolicy,
                    //                        UserRolePolicies.UserWritePolicy);
                    // });

                    options.AddPolicy(AuthPolicies.ProjectUserWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           UserRolePolicies.UserWritePolicy);
                    });

                    options.AddPolicy(AuthPolicies.ProjectLinkWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           ProjectUserRole.Provider.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           ProjectUserRole.Member.AuthPolicy(),
                                           ProjectUserRole.Provider.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectCreate, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           OrganizationUserRole.Creator.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectIdentityRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           ProjectUserRole.Provider.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProviderDataRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProviderUserRoles.ProviderReadPolicyRoleName,
                                           ProviderUserRoles.ProviderWritePolicyRoleName);
                    });

                    options.AddPolicy(AuthPolicies.ProviderDataWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProviderUserRoles.ProviderWritePolicyRoleName);
                    });

                    options.AddPolicy(AuthPolicies.ProviderOfferRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                        ProviderUserRoles.ProviderWritePolicyRoleName);
                    });

                    options.AddPolicy(AuthPolicies.ProviderOfferWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                        ProviderUserRoles.ProviderWritePolicyRoleName);
                    });

                    options.AddPolicy(AuthPolicies.ProviderComponentWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                        ProviderUserRoles.ProviderWritePolicyRoleName);
                    });

                    options.AddPolicy(AuthPolicies.ProjectComponentRead, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           ProjectUserRole.Member.AuthPolicy(),
                                           ProjectUserRole.Provider.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectComponentWrite, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                           ProjectUserRole.Owner.AuthPolicy(),
                                           ProjectUserRole.Member.AuthPolicy(),
                                           ProjectUserRole.Provider.AuthPolicy());
                    });

                    options.AddPolicy(AuthPolicies.ProjectComponentUpdate, policy =>
                    {
                        policy.RequireRole(OrganizationUserRole.Admin.AuthPolicy(),
                                        UserRolePolicies.ComponentWritePolicy);
                    });
                });

            return services;
        }

        internal static async Task<IEnumerable<Claim>> ResolveClaimsAsync(this HttpContext httpContext, string userId)
        {
            var claims = new List<Claim>();

            if (httpContext.RequestPathStartsWithSegments("api/orgs"))
                return claims;

            var organization = httpContext.RouteValueOrDefault("Organization");

            if (string.IsNullOrEmpty(organization))
                return claims;

            var organizationRepository = httpContext.RequestServices
                .GetRequiredService<IOrganizationRepository>();

            var organizationId = await organizationRepository
                .ResolveIdAsync(organization)
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(organizationId))
                return claims;

            var userRepository = httpContext.RequestServices
                .GetRequiredService<IUserRepository>();

            var user = await userRepository
                .GetAsync(organizationId, userId)
                .ConfigureAwait(false);

            if (user is null)
                return claims;

            claims.Add(new Claim(ClaimTypes.Role, user.AuthPolicy()));

            var organizationPath = $"/api/{organization}";

            if (httpContext.RequestPathStartsWithSegments($"{organizationPath}/projects"))
            {
                claims.AddRange(await httpContext.ResolveProjectClaimsAsync(organizationPath, user).ConfigureAwait(false));
            }
            else if (httpContext.RequestPathStartsWithSegments($"{organizationPath}/users")
                  || httpContext.RequestPathStartsWithSegments($"{organizationPath}/me"))
            {
                claims.AddRange(await httpContext.ResolveUserClaimsAsync(organizationPath, user).ConfigureAwait(false));
            }

            return claims;
        }

        private static async Task<IEnumerable<Claim>> ResolveProjectClaimsAsync(this HttpContext httpContext, string organizationPath, User user)
        {
            var claims = new List<Claim>();

            var projectId = httpContext.RouteValueOrDefault("ProjectId");

            if (string.IsNullOrEmpty(projectId))
                projectId = await httpContext.ResolveProjectIdFromNameOrIdRouteAsync(user.Organization).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(projectId))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.AuthPolicy(projectId)));

                if (httpContext.RequestPathStartsWithSegments($"{organizationPath}/projects/{projectId}/users"))
                    claims.AddRange(await httpContext.ResolveUserClaimsAsync(organizationPath, user).ConfigureAwait(false));

                if (httpContext.RequestPathStartsWithSegments($"{organizationPath}/projects/{projectId}/components"))
                    claims.AddRange(await httpContext.ResolveComponentClaimsAsync(projectId, user).ConfigureAwait(false));
            }

            return claims;
        }

        private static async Task<IEnumerable<Claim>> ResolveUserClaimsAsync(this HttpContext httpContext, string organizationPath, User user)
        {
            var claims = new List<Claim>();

            string userId;

            if (httpContext.RequestPathStartsWithSegments($"{organizationPath}/me")
            || (httpContext.RequestPathStartsWithSegments(organizationPath) && httpContext.RequestPathEndsWith("/me")))
            {
                userId = user.Id;
            }
            else
            {
                userId = httpContext.RouteValueOrDefault("UserId");

                if (string.IsNullOrEmpty(userId))
                    userId = await httpContext.ResolveUserIdFromNameOrIdRouteAsync().ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(userId) && userId.Equals(user.Id, StringComparison.OrdinalIgnoreCase))
            {
                claims.Add(new Claim(ClaimTypes.Role, UserRolePolicies.UserWritePolicy));
            }

            return claims;
        }

        private static async Task<IEnumerable<Claim>> ResolveComponentClaimsAsync(this HttpContext httpContext, string projectId, User user)
        {
            var claims = new List<Claim>();

            if (httpContext.Request.Method == HttpMethods.Get)
                return claims;

            var componentId = httpContext.RouteValueOrDefault("ComponentId");

            if (!string.IsNullOrEmpty(componentId))
            {
                var componentRepository = httpContext.RequestServices
                    .GetRequiredService<IComponentRepository>();

                var component = await componentRepository
                    .GetAsync(projectId, componentId)
                    .ConfigureAwait(false);

                if (!string.IsNullOrEmpty(component?.RequestedBy) && component.RequestedBy.Equals(user.Id, StringComparison.OrdinalIgnoreCase))
                    claims.Add(new Claim(ClaimTypes.Role, UserRolePolicies.ComponentWritePolicy));
            }

            return claims;
        }

        private static async Task<string> ResolveProjectIdFromNameOrIdRouteAsync(this HttpContext httpContext, string organizationId)
        {
            var projectNameOrId = httpContext.RouteValueOrDefault("ProjectNameOrId");

            if (string.IsNullOrEmpty(projectNameOrId) || projectNameOrId.IsGuid())
                return projectNameOrId;

            var projectsRepository = httpContext.RequestServices
                .GetRequiredService<IProjectRepository>();

            var project = await projectsRepository
                .GetAsync(organizationId, projectNameOrId)
                .ConfigureAwait(false);

            return project?.Id;
        }

        private static async Task<string> ResolveUserIdFromNameOrIdRouteAsync(this HttpContext httpContext)
        {
            var userNameOrId = httpContext.RouteValueOrDefault("UsertNameOrId");

            if (string.IsNullOrEmpty(userNameOrId))
                return userNameOrId;

            var userService = httpContext.RequestServices
                .GetRequiredService<UserService>();

            var userId = await userService
                .GetUserIdAsync(userNameOrId)
                .ConfigureAwait(false);

            return userId;
        }

        private static bool RequestPathStartsWithSegments(this HttpContext httpContext, PathString other, bool ignoreCase = true)
            => httpContext.Request.Path.StartsWithSegments(other, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private static bool RequestPathEndsWith(this HttpContext httpContext, string value, bool ignoreCase = true)
            => httpContext.Request.Path.Value.EndsWith(value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        private static string RouteValueOrDefault(this HttpContext httpContext, string key, bool ignoreCase = true)
            => httpContext.GetRouteData().Values.GetValueOrDefault(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)?.ToString();
    }
}

/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Schema;
using TeamCloud.Git.Data;
using TeamCloud.Model.Data;

namespace TeamCloud.Git
{
    public static class GlobalExtensions
    {
        public static RepositoryReference ParseUrl(this RepositoryReference repository)
        {
            if (repository is null)
                throw new ArgumentNullException(nameof(repository));

            if (repository.IsGitHub())
            {
                repository.Provider = RepositoryProvider.GitHub;
                repository.ParseGitHubUrl();
            }
            else if (repository.IsDevOps())
            {
                repository.Provider = RepositoryProvider.DevOps;
                repository.ParseDevOpsUrl();
            }
            else
            {
                throw new NotSupportedException("Only GitHub and Azure DevOps git repositories are supported. Generic git repositories are not supported.");
            }

            return repository;
        }

        private static RepositoryReference ParseGitHubUrl(this RepositoryReference repository)
        {
            repository.Url = repository.Url
                .Replace("git@", "https://", StringComparison.OrdinalIgnoreCase)
                .Replace("github.com:", "github.com/", StringComparison.OrdinalIgnoreCase);

            if (repository.Url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                repository.Url = repository.Url[0..^4];

            var parts = repository.Url.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            var index = parts.FindIndex(p => p.Contains("github.com", StringComparison.OrdinalIgnoreCase));

            if (index == -1 || parts.Count < index + 3)
                throw new Exception("Invalid GitHub Repository Url");

            repository.Organization = parts[index + 1];
            repository.Repository = parts[index + 2];
            repository.BaselUrl = repository.Url.Split(repository.Organization).First().TrimEnd('/');

            return repository;
        }

        private static RepositoryReference ParseDevOpsUrl(this RepositoryReference repository)
        {
            repository.Url = repository.Url
                .Replace("git@ssh.", "https://", StringComparison.OrdinalIgnoreCase)
                .Replace(":v3/", "/", StringComparison.OrdinalIgnoreCase);

            if (repository.Url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                repository.Url = repository.Url[0..^4];

            var parts = repository.Url.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
            var index = parts.FindIndex(p => p.Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase)
                                          || p.Contains("visualstudio.com", StringComparison.OrdinalIgnoreCase));

            if (index == -1)
                throw new Exception("Invalid Azure DevOps Repository Url");

            if (!parts.Remove("_git"))
                repository.Url = repository.Url
                    .Replace($"/{parts.Last()}", $"/_git/{parts.Last()}", StringComparison.Ordinal);

            if (parts[index].Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase))
                ++index;

            if (parts.Count < index + 3)
                throw new Exception("Invalid Azure DevOps Repository Url");

            repository.Organization = parts[index].Replace(".visualstudio.com", "", StringComparison.OrdinalIgnoreCase);
            repository.Project = parts[index + 1];
            repository.Repository = parts[index + 2];
            repository.BaselUrl = repository.Url.Split(repository.Project).First().TrimEnd('/');

            return repository;
        }

        private static bool IsGitHub(this RepositoryReference repo)
            => repo?.Url.Contains("github.com", StringComparison.OrdinalIgnoreCase) ?? throw new ArgumentNullException(nameof(repo));

        private static bool IsDevOps(this RepositoryReference repo)
            => repo is null
             ? throw new ArgumentNullException(nameof(repo))
             : repo.Url.Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase)
            || repo.Url.Contains("visualstudio.com", StringComparison.OrdinalIgnoreCase);

        internal static bool IsBranch(this Microsoft.TeamFoundation.SourceControl.WebApi.GitRef gitRef)
            => gitRef?.Name?.StartsWith("refs/heads/", StringComparison.Ordinal) ?? throw new ArgumentNullException(nameof(gitRef));

        internal static bool IsTag(this Microsoft.TeamFoundation.SourceControl.WebApi.GitRef gitRef)
            => gitRef?.Name?.StartsWith("refs/tags/", StringComparison.Ordinal) ?? throw new ArgumentNullException(nameof(gitRef));

        internal static JSchema ToSchema(this IEnumerable<YamlParameter<dynamic>> parameters)
        {
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));

            var schema = new JSchema
            {
                Type = JSchemaType.Object
            };

            foreach (var parameter in parameters)
            {
                var parameterSchema = new JSchema
                {
                    Type = parameter.Type,
                    Default = parameter.Value ?? parameter.Default,
                    ReadOnly = parameter.Readonly,
                    Description = parameter.Name
                };

                parameter.Allowed?.ForEach(a => parameterSchema.Enum.Add(a));

                schema.Properties.Add(parameter.Id, parameterSchema);
            }

            parameters
                .Where(p => p.Required)
                .ToList()
                .ForEach(p => schema.Required.Add(p.Id));

            return schema;
        }

        internal static ComponentOffer ToOffer(this ComponentYaml yaml, RepositoryReference repo, string folder)
        {
            if (yaml is null)
                throw new ArgumentNullException(nameof(yaml));

            if (repo is null)
                throw new ArgumentNullException(nameof(repo));

            if (folder is null)
                throw new ArgumentNullException(nameof(folder));

            return new ComponentOffer
            {
                Id = $"{repo}.{folder.Replace(' ', '_').Replace('-', '_')}",
                ProviderId = yaml.Provider,
                DisplayName = folder,
                Description = yaml.Description,
                Scope = yaml.Scope,
                Type = yaml.Type,
                InputJsonSchema = yaml.Parameters.ToSchema().ToString()
            };
        }

        internal static ProjectTemplate ToProjectTemplate(this ProjectYaml yaml, RepositoryReference repo)
        {
            if (yaml is null)
                throw new ArgumentNullException(nameof(yaml));

            if (repo is null)
                throw new ArgumentNullException(nameof(repo));

            return new ProjectTemplate
            {
                Id = repo.Url,
                Repository = repo,
                DisplayName = yaml.Name,
                Description = yaml.Description,
                Components = yaml.Components,
                InputJsonSchema = yaml.Parameters.ToSchema().ToString()
            };
        }
    }
}

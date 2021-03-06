/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Data;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Orchestrator.Entities;

namespace TeamCloud.Orchestrator.Activities
{
    public class ProjectSetActivity
    {
        private readonly IProjectRepository projectRepository;

        public ProjectSetActivity(IProjectRepository projectRepository)
        {
            this.projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
        }

        [FunctionName(nameof(ProjectSetActivity))]
        public async Task<ProjectDocument> RunActivity(
            [ActivityTrigger] IDurableActivityContext activityContext)
        {
            if (activityContext is null)
                throw new ArgumentNullException(nameof(activityContext));

            var project = activityContext.GetInput<ProjectDocument>();

            var newProject = await projectRepository
                .SetAsync(project)
                .ConfigureAwait(false);

            return newProject;
        }
    }

    internal static class ProjectSetExtension
    {
        public static Task<ProjectDocument> SetProjectAsync(this IDurableOrchestrationContext orchestrationContext, ProjectDocument project, bool allowUnsafe = false)
            => orchestrationContext.IsLockedByContainerDocument(project) || allowUnsafe
            ? orchestrationContext.CallActivityWithRetryAsync<ProjectDocument>(nameof(ProjectSetActivity), project)
            : throw new NotSupportedException($"Unable to set project '{project.Id}' without acquired lock");
    }
}

﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;

namespace TeamCloud.Orchestrator.Orchestrations.Projects.Activities
{
    internal static class ProjectListExtension
    {
        public static Task<IEnumerable<Project>> GetTeamCloudProjectsAsync(this IDurableOrchestrationContext durableOrchestrationContext)
            => durableOrchestrationContext.CallActivityWithRetryAsync<IEnumerable<Project>>(nameof(ProjectListActivity), null);
    }
}

﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Data;
using TeamCloud.Model.Data;
using TeamCloud.Orchestration;
using TeamCloud.Serialization;

namespace TeamCloud.Orchestrator.Operations.Activities
{
    public sealed class ComponentStorageActivity
    {
        private readonly IProjectRepository projectRepository;
        private readonly IAzureResourceService azureResourceService;

        public ComponentStorageActivity(IProjectRepository projectRepository, IAzureResourceService azureResourceService)
        {
            this.projectRepository = projectRepository ?? throw new ArgumentNullException(nameof(projectRepository));
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ComponentStorageActivity))]
        [RetryOptions(3)]
        public async Task<Component> Run(
            [ActivityTrigger] IDurableActivityContext context,
            ILogger log)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (log is null)
                throw new ArgumentNullException(nameof(log));

            var component = context.GetInput<Input>().Component;

            try
            {
                var project = await projectRepository
                    .GetAsync(component.Organization, component.ProjectId)
                    .ConfigureAwait(false);

                if (AzureResourceIdentifier.TryParse(project?.ResourceId, out var projectResourceId))
                {
                    var projectResourceGroup = await azureResourceService
                        .GetResourceGroupAsync(projectResourceId.SubscriptionId, projectResourceId.ResourceGroup)
                        .ConfigureAwait(false);

                    var projectStorageResource = await projectResourceGroup
                        .GetResourcesByTypeAsync("Microsoft.Storage/storageAccounts")
                        .SingleOrDefaultAsync()
                        .ConfigureAwait(false);

                    component.StorageId = projectStorageResource?.ResourceId.ToString();
                }
            }
            catch (Exception exc)
            {
                throw exc.AsSerializable();
            }

            return component;
        }

        internal struct Input
        {
            public Component Component { get; set; }
        }
    }
}

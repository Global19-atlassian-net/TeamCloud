/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TeamCloud.Model.Data
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public sealed class TeamCloudInstance : IContainerDocument, ITags
    {
        public static string DefaultId => Constants.CosmosDb.TenantName;

        public string Id => DefaultId;

        public string PartitionKey => Constants.CosmosDb.DatabaseName;

        [JsonIgnore]
        public IList<string> UniqueKeys => new List<string> { };

        public IDictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();

        public IList<Provider> Providers { get; set; } = new List<Provider>();
    }
}

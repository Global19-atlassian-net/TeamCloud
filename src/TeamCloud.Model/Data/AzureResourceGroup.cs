/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using Newtonsoft.Json;
using TeamCloud.Serialization;

namespace TeamCloud.Model.Data
{
    [JsonObject(NamingStrategyType = typeof(TeamCloudNamingStrategy))]
    public class AzureResourceGroup : IEquatable<AzureResourceGroup>
    {
        [JsonProperty(Required = Required.Always)]
        public string Id { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        [JsonProperty(Required = Required.Always)]
        public Guid SubscriptionId { get; set; } = Guid.Empty;

        [JsonProperty(Required = Required.Always)]
        public string Region { get; set; }


        public bool Equals(AzureResourceGroup other)
            => Id?.Equals(other?.Id, StringComparison.OrdinalIgnoreCase) ?? false;

        public override bool Equals(object obj)
            => base.Equals(obj) || Equals(obj as AzureResourceGroup);

        public override int GetHashCode()
            => Id?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? base.GetHashCode();
    }
}

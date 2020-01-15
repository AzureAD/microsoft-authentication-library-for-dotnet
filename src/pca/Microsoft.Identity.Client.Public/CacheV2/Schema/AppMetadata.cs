// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    internal class AppMetadata
    {
        public AppMetadata(string environment, string clientId, string familyId)
        {
            Environment = environment;
            ClientId = clientId;
            FamilyId = familyId;
        }

        public string Environment { get; }
        public string ClientId { get; }
        public string FamilyId { get; }
    }
}

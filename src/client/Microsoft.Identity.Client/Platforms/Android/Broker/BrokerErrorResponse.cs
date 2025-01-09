// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Identity.Client.Internal.Broker;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    [Preserve(AllMembers = true)]
    internal class BrokerErrorResponse
    {
        [JsonPropertyName(BrokerResponseConst.BrokerErrorCode)]
        public string BrokerErrorCode { get; set; }

        [JsonPropertyName(BrokerResponseConst.BrokerErrorMessage)]
        public string BrokerErrorMessage { get; set; }
    }
}

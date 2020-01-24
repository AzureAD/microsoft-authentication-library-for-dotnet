// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Android.Runtime;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [DataContract]
    [JsonObject]
    [Preserve]
    internal class BrokerRequest
    {
        [JsonProperty("authority")]
        public string Authority { get; set; }
        [JsonProperty("scopes")]
        public string Scopes { get; set; }
        [JsonProperty("redirect_uri")]
        public string RedirectUri { get; set; }
        [JsonProperty("client_id")]
        public string ClientId { get; set; }
        [JsonProperty("home_account_id")]
        public string HomeAccountId { get; set; }
        [JsonProperty("local_account_id")]
        public string LocalAccountId { get; set; }
        [JsonProperty("username")]
        public string UserName { get; set; }
        [JsonProperty("extra_query_param")]
        public string ExtraQueryParameters { get; set; }
        [JsonProperty("correlation_id")]
        public string CorrelationId { get; set; }
        [JsonProperty("prompt")]
        public string Prompt { get; set; }
        [JsonProperty("claims")]
        public string Claims { get; set; }
        [JsonProperty("force_refresh")]
        public string ForceRefresh { get; set; }
        [JsonProperty("client_app_name")]
        public string ClientAppName { get; set; }
        [JsonProperty("client_app_version")]
        public string ClientAppVersion { get; set; }
        [JsonProperty("client_version")]
        public string ClientVersion { get; set; }
        [JsonProperty("environment")]
        public string Environment { get; set; }
    }
}

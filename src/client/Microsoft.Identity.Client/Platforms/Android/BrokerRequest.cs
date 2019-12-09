// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.Serialization;

namespace Microsoft.Identity.Client.Platforms.Android
{
    [DataContract]
    internal class BrokerRequest
    {
        [DataMember(Name = "authority")]
        public string Authority { get; set; }
        [DataMember(Name = "scopes")]
        public string Scopes { get; set; }
        [DataMember(Name = "redirect_uri")]
        public string RedirectUri { get; set; }
        [DataMember(Name = "client_id")]
        public string ClientId { get; set; }
        [DataMember(Name = "home_account_id")]
        public string HomeAccountId { get; set; }
        [DataMember(Name = "local_account_id")]
        public string LocalAccountId { get; set; }
        [DataMember(Name = "username")]
        public string UserName { get; set; }
        [DataMember(Name = "extra_query_param")]
        public string ExtraQueryParameters { get; set; }
        [DataMember(Name = "correlation_id")]
        public string CorrelationId { get; set; }
        [DataMember(Name = "prompt")]
        public string Prompt { get; set; }
        [DataMember(Name = "claims")]
        public string Claims { get; set; }
        [DataMember(Name = "force_refresh")]
        public string ForceRefresh { get; set; }
        [DataMember(Name = "client_app_name")]
        public string ClientAppName { get; set; }
        [DataMember(Name = "client_app_version")]
        public string ClientAppVersion { get; set; }
        [DataMember(Name = "client_version")]
        public string ClientVersion { get; set; }
        [DataMember(Name = "environment")]
        public string Environment { get; set; }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace MSIHelperService
{
    [JsonObject]
    public class ManagedIdentityResponse
    {
        private string? _token;

        [JsonProperty("access_token")]
        public string? AccessToken
        {
            get { return _token; }
            set => _token = value?.Substring(0, 20) + "-trimmed";
        }

        [JsonProperty("expires_on")]
        public string? ExpiresOn { get; set; }

        [JsonProperty("resource")]
        public string? Resource { get; set; }

        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        [JsonProperty("client_id")]
        public string? ClientId { get; set; }
    }
}

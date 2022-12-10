// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace MSIHelperService
{
    /// <summary>
    /// ManagedIdentityResponse
    /// </summary>
    [JsonObject]
    public class ManagedIdentityResponse
    {
        private string? _token;

        /// <summary>
        /// AccessToken
        /// </summary>
        [JsonProperty("access_token")]
        public string? AccessToken
        {
            get { return _token; }
            set => _token = value?.Substring(0, 20) + "-trimmed";
        }

        /// <summary>
        /// ExpiresOn
        /// </summary>
        [JsonProperty("expires_on")]
        public string? ExpiresOn { get; set; }

        /// <summary>
        /// Resource
        /// </summary>
        [JsonProperty("resource")]
        public string? Resource { get; set; }

        /// <summary>
        /// TokenType
        /// </summary>
        [JsonProperty("token_type")]
        public string? TokenType { get; set; }

        /// <summary>
        /// ClientId
        /// </summary>
        [JsonProperty("client_id")]
        public string? ClientId { get; set; }
    }
}

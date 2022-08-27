// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net6;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Cache
{
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal class AdalResultWrapper
    {

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public AdalResult Result { get; set; }

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string RawClientInfo { get; set; }

        /// <summary>
        /// Gets the Refresh Token associated with the requested Access Token. Note: not all operations will return a Refresh Token.
        /// </summary>
#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets a value indicating whether the refresh token can be used for requesting access token for other resources.
        /// </summary>
        internal bool IsMultipleResourceRefreshToken => !string.IsNullOrWhiteSpace(RefreshToken) && !string.IsNullOrWhiteSpace(ResourceInResponse);

        // This is only needed for AcquireTokenByAuthorizationCode in which parameter resource is optional and we need
        // to get it from the STS response.
#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        internal string ResourceInResponse { get; set; }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Deserialized authentication result</returns>
        public static AdalResultWrapper Deserialize(string serializedObject)
        {
            return JsonHelper.DeserializeFromJson<AdalResultWrapper>(serializedObject);
        }

        /// <summary>
        /// Serializes the object to a JSON string
        /// </summary>
        /// <returns>Serialized authentication result</returns>
        public string Serialize()
        {
            return JsonHelper.SerializeToJson(this);
        }

#if !SUPPORTS_SYSTEM_TEXT_JSON
        [JsonProperty]
#endif
        public string UserAssertionHash { get; set; }

        internal AdalResultWrapper Clone()
        {
            return Deserialize(Serialize());
        }
    }
}

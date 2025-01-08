﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
#if SUPPORTS_SYSTEM_TEXT_JSON
using Microsoft.Identity.Client.Platforms.net;
using JsonProperty = System.Text.Json.Serialization.JsonIncludeAttribute;
#else
using Microsoft.Identity.Json;
#endif

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Contains information of a single user. This information is used for token cache lookup. Also if created with userId, userId is sent to the service when login_hint is accepted.
    /// </summary>
    [JsonObject]
    [Preserve(AllMembers = true)]
    internal sealed class AdalUserInfo
    {
        /// <summary>
        /// Create user information for token cache lookup
        /// </summary>
        public AdalUserInfo()
        {
        }

        /// <summary>
        /// Create user information copied from another UserInfo object
        /// </summary>
        public AdalUserInfo(AdalUserInfo other)
        {
            if (other != null)
            {
                UniqueId = other.UniqueId;
                DisplayableId = other.DisplayableId;
                GivenName = other.GivenName;
                FamilyName = other.FamilyName;
                IdentityProvider = other.IdentityProvider;
                PasswordChangeUrl = other.PasswordChangeUrl;
                PasswordExpiresOn = other.PasswordExpiresOn;
            }
        }

        /// <summary>
        /// Gets identifier of the user authenticated during token acquisition.
        /// </summary>
        [JsonProperty]
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets a displayable value in UserPrincipalName (UPN) format. The value can be null.
        /// </summary>
        [JsonProperty]
        public string DisplayableId { get; internal set; }

        /// <summary>
        /// Gets given name of the user if provided by the service. If not, the value is null.
        /// </summary>
        [JsonProperty]
        public string GivenName { get; internal set; }

        /// <summary>
        /// Gets family name of the user if provided by the service. If not, the value is null.
        /// </summary>
        [JsonProperty]
        public string FamilyName { get; internal set; }

        /// <summary>
        /// Gets the time when the password expires. Default value is 0.
        /// </summary>
        [JsonProperty]
        public DateTimeOffset? PasswordExpiresOn { get; internal set; }

        /// <summary>
        /// Gets the URL where the user can change the expiring password. The value can be null.
        /// </summary>
        [JsonProperty]
        public Uri PasswordChangeUrl { get; internal set; }

        /// <summary>
        /// Gets identity provider if returned by the service. If not, the value is null.
        /// </summary>
        [JsonProperty]
        public string IdentityProvider { get; internal set; }
    }
}

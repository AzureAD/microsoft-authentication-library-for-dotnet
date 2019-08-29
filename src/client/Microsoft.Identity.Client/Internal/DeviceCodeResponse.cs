// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.OAuth2;

namespace Microsoft.Identity.Client.Internal
{
    [DataContract]
    internal class DeviceCodeResponse : OAuth2ResponseBase
    {
        [DataMember(Name = "user_code", IsRequired = false)]
        public string UserCode { get; internal set; }

        [DataMember(Name = "device_code", IsRequired = false)]
        public string DeviceCode { get; internal set; }

        [DataMember(Name = "verification_url", IsRequired = false)]
        public string VerificationUrl { get; internal set; }

        // This is the OAuth2 standards compliant value.
        // It should be used if it's present, if it's not then fallback to VerificiationUrl
        [DataMember(Name = "verification_uri", IsRequired = false)]
        public string VerificationUri { get; internal set; }

        [DataMember(Name = "expires_in", IsRequired = false)]
        public long ExpiresIn { get; internal set; }

        [DataMember(Name = "interval", IsRequired = false)]
        public long Interval { get; internal set; }

        [DataMember(Name = "message", IsRequired = false)]
        public string Message { get; internal set; }

        public DeviceCodeResult GetResult(string clientId, ISet<string> scopes)
        {
            // VerificationUri should be used if it's present, and if not fall back to VerificationUrl
            string verification = string.IsNullOrWhiteSpace(VerificationUri) ? VerificationUrl : VerificationUri;

            return new DeviceCodeResult(
                UserCode,
                DeviceCode,
                verification,
                DateTime.UtcNow.AddSeconds(ExpiresIn),
                Interval,
                Message,
                clientId,
                scopes);
        }
    }
}

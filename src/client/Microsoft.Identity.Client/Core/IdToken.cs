// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;

namespace Microsoft.Identity.Client.Core
{
    internal class IdTokenClaim
    {
        public const string Issuer = "iss";
        public const string ObjectId = "oid";
        public const string Subject = "sub";
        public const string TenantId = "tid";
        public const string Version = "ver";
        public const string PreferredUsername = "preferred_username";
        public const string Name = "name";
        public const string HomeObjectId = "home_oid";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string Upn = "upn";
    }

    [JsonObject]
    [DataContract]
    [Preserve]
    internal class IdToken
    {
        [JsonProperty(PropertyName = IdTokenClaim.Issuer)]
        public string Issuer { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.ObjectId)]
        public string ObjectId { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.Subject)]
        public string Subject { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.TenantId)]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.Version)]
        public string Version { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.PreferredUsername)]
        public string PreferredUsername { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.Name)]
        public string Name { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.HomeObjectId)]
        public string HomeObjectId { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.Upn)]
        public string Upn { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.GivenName)]
        public string GivenName { get; set; }

        [JsonProperty(PropertyName = IdTokenClaim.FamilyName)]
        public string FamilyName { get; set; }

        public static IdToken Parse(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            IdToken idTokenBody = null;
            string[] idTokenSegments = idToken.Split(new[] {'.'});

            if (idTokenSegments.Length < 2)
            {
                throw new MsalClientException(
                    MsalError.InvalidJwtError,
                    MsalErrorMessage.IDTokenMustHaveTwoParts);
            }

            try
            {
                byte[] idTokenBytes = Base64UrlHelpers.DecodeToBytes(idTokenSegments[1]);
                idTokenBody = JsonHelper.DeserializeFromJson<IdToken>(idTokenBytes);
            }
            catch (JsonException exc)
            {
                throw new MsalClientException(
                    MsalError.JsonParseError,
                    MsalErrorMessage.FailedToParseIDToken,
                    exc);
            }

            return idTokenBody;
        }

        public string GetUniqueId()
        {
            return ObjectId ?? Subject;
        }
    }
}

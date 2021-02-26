// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Json;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.Internal
{
    internal static class IdTokenClaim
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
    [Preserve(AllMembers = true)]
    internal class IdToken : IJsonSerializable<IdToken>
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

        public IdToken DeserializeFromJson(string json) => DeserializeFromJObject(JObject.Parse(json));

        public IdToken DeserializeFromJObject(JObject jObject)
        {
            Issuer = jObject[IdTokenClaim.Issuer]?.ToString();
            ObjectId = jObject[IdTokenClaim.ObjectId]?.ToString();
            Subject = jObject[IdTokenClaim.Subject]?.ToString();
            TenantId = jObject[IdTokenClaim.TenantId]?.ToString();
            Version = jObject[IdTokenClaim.Version]?.ToString();
            PreferredUsername = jObject[IdTokenClaim.PreferredUsername]?.ToString();
            Name = jObject[IdTokenClaim.Name]?.ToString();
            HomeObjectId = jObject[IdTokenClaim.HomeObjectId]?.ToString();
            Upn = jObject[IdTokenClaim.Upn]?.ToString();
            GivenName = jObject[IdTokenClaim.GivenName]?.ToString();
            FamilyName = jObject[IdTokenClaim.FamilyName]?.ToString();

            return this;
        }

        public string SerializeToJson() => SerializeToJObject().ToString(Formatting.None);

        public JObject SerializeToJObject()
        {
            return new JObject(
                new JProperty(IdTokenClaim.Issuer, Issuer),
                new JProperty(IdTokenClaim.ObjectId, ObjectId),
                new JProperty(IdTokenClaim.Subject, Subject),
                new JProperty(IdTokenClaim.TenantId, TenantId),
                new JProperty(IdTokenClaim.Version, Version),
                new JProperty(IdTokenClaim.PreferredUsername, PreferredUsername),
                new JProperty(IdTokenClaim.Name, Name),
                new JProperty(IdTokenClaim.HomeObjectId, HomeObjectId),
                new JProperty(IdTokenClaim.Upn, Upn),
                new JProperty(IdTokenClaim.GivenName, GivenName),
                new JProperty(IdTokenClaim.FamilyName, FamilyName));
        }

        public static IdToken Parse(string idToken)
        {
            if (string.IsNullOrEmpty(idToken))
            {
                return null;
            }

            IdToken idTokenBody = null;
            string[] idTokenSegments = idToken.Split(new[] { '.' });

            if (idTokenSegments.Length < 2)
            {
                throw new MsalClientException(
                    MsalError.InvalidJwtError,
                    MsalErrorMessage.IDTokenMustHaveTwoParts);
            }

            try
            {
                idTokenBody = JsonHelper.DeserializeNew<IdToken>(Base64UrlHelpers.DecodeToString(idTokenSegments[1]));
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

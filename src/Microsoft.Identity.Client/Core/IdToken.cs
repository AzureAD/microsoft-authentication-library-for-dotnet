// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Utils;

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
    }

    [DataContract]
    internal class IdToken
    {
        [DataMember(Name = IdTokenClaim.Issuer, IsRequired = false)]
        public string Issuer { get; set; }

        [DataMember(Name = IdTokenClaim.ObjectId, IsRequired = false)]
        public string ObjectId { get; set; }

        [DataMember(Name = IdTokenClaim.Subject, IsRequired = false)]
        public string Subject { get; set; }

        [DataMember(Name = IdTokenClaim.TenantId, IsRequired = false)]
        public string TenantId { get; set; }

        [DataMember(Name = IdTokenClaim.Version, IsRequired = false)]
        public string Version { get; set; }

        [DataMember(Name = IdTokenClaim.PreferredUsername, IsRequired = false)]
        public string PreferredUsername { get; set; }

        [DataMember(Name = IdTokenClaim.Name, IsRequired = false)]
        public string Name { get; set; }

        [DataMember(Name = IdTokenClaim.HomeObjectId, IsRequired = false)]
        public string HomeObjectId { get; set; }

        [DataMember(Name = IdTokenClaim.GivenName)]
        public string GivenName { get; set; }

        [DataMember(Name = IdTokenClaim.FamilyName, IsRequired = false)]
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
                using (var stream = new MemoryStream(idTokenBytes))
                {
                    var serializer = new DataContractJsonSerializer(typeof(IdToken));
                    idTokenBody = (IdToken) serializer.ReadObject(stream);
                }
            }
            catch (Exception exc)
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

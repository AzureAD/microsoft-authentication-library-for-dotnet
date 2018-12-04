// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Json.Linq;

namespace Microsoft.Identity.Client.CacheV2.Schema
{
    /// <summary>
    /// This is the object we will serialize (using StorageJson* classes for specific field names) for Credential information.
    /// Credentials include Access Tokens, Refresh Tokens, etc.
    /// If you're modifying this object and the related (de)serialization, you're modifying the cache persistence
    /// model and need to ensure it's compatible and compliant with the other cache implementations.
    /// </summary>
    internal class Credential : IEquatable<Credential>
    {
        private string _additionalFieldsJson;
        public string HomeAccountId { get; set; }
        public string Environment { get; set; }
        public string Realm { get; set; }
        public CredentialType CredentialType { get; set; }
        public string ClientId { get; set; }
        public string FamilyId { get; set; }
        public string Target { get; set; }
        public long CachedAt { get; set; }
        public long ExpiresOn { get; set; }
        public long ExtendedExpiresOn { get; set; }
        public string Secret { get; set; }

        public string AdditionalFieldsJson
        {
            get => _additionalFieldsJson;
            set => _additionalFieldsJson = string.IsNullOrWhiteSpace(value) ? string.Empty : JObject.Parse(value).ToString();
        }

        public bool Equals(Credential other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(HomeAccountId, other.HomeAccountId) && string.Equals(Environment, other.Environment) &&
                   string.Equals(Realm, other.Realm) && CredentialType == other.CredentialType &&
                   string.Equals(ClientId, other.ClientId) && string.Equals(FamilyId, other.FamilyId) &&
                   string.Equals(Target, other.Target) && CachedAt == other.CachedAt && ExpiresOn == other.ExpiresOn &&
                   ExtendedExpiresOn == other.ExtendedExpiresOn && string.Equals(Secret, other.Secret) &&
                   string.Equals(AdditionalFieldsJson, other.AdditionalFieldsJson);
        }

        public static Credential CreateEmpty()
        {
            return new Credential();
        }

        public static Credential CreateAccessToken(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            string target,
            long cachedAt,
            long expiresOn,
            long extendedExpiresOn,
            string secret,
            string additionalFieldsJson)
        {
            return new Credential
            {
                CredentialType = CredentialType.OAuth2AccessToken,
                HomeAccountId = homeAccountId,
                Environment = environment,
                Realm = realm,
                ClientId = clientId,
                Target = target,
                CachedAt = cachedAt,
                ExpiresOn = expiresOn,
                ExtendedExpiresOn = extendedExpiresOn,
                Secret = secret,
                AdditionalFieldsJson = additionalFieldsJson
            };
        }

        public static Credential CreateIdToken(
            string homeAccountId,
            string environment,
            string realm,
            string clientId,
            long cachedAt,
            string secret,
            string additionalFieldsJson)
        {
            return new Credential
            {
                CredentialType = CredentialType.OidcIdToken,
                HomeAccountId = homeAccountId,
                Environment = environment,
                Realm = realm,
                ClientId = clientId,
                CachedAt = cachedAt,
                Secret = secret,
                AdditionalFieldsJson = additionalFieldsJson
            };
        }

        public static Credential CreateRefreshToken(
            string homeAccountId,
            string environment,
            string clientId,
            long cachedAt,
            string secret,
            string additionalFieldsJson)
        {
            return new Credential
            {
                CredentialType = CredentialType.OAuth2RefreshToken,
                HomeAccountId = homeAccountId,
                Environment = environment,
                ClientId = clientId,
                CachedAt = cachedAt,
                Secret = secret,
                AdditionalFieldsJson = additionalFieldsJson
            };
        }

        public static Credential CreateFamilyRefreshToken(
            string homeAccountId,
            string environment,
            string clientId,
            string familyId,
            long cachedAt,
            string secret,
            string additionalFieldsJson)
        {
            return new Credential
            {
                CredentialType = CredentialType.OAuth2RefreshToken,
                HomeAccountId = homeAccountId,
                Environment = environment,
                ClientId = clientId,
                FamilyId = familyId,
                CachedAt = cachedAt,
                Secret = secret,
                AdditionalFieldsJson = additionalFieldsJson
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Credential)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = HomeAccountId != null ? HomeAccountId.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Environment != null ? Environment.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Realm != null ? Realm.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)CredentialType;
                hashCode = (hashCode * 397) ^ (ClientId != null ? ClientId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FamilyId != null ? FamilyId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Target != null ? Target.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ CachedAt.GetHashCode();
                hashCode = (hashCode * 397) ^ ExpiresOn.GetHashCode();
                hashCode = (hashCode * 397) ^ ExtendedExpiresOn.GetHashCode();
                hashCode = (hashCode * 397) ^ (Secret != null ? Secret.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AdditionalFieldsJson != null ? AdditionalFieldsJson.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(Credential left, Credential right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Credential left, Credential right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Credential(\n");
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  HomeAccountId: {0}\n", HomeAccountId));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  Environment: {0}\n", Environment));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  Realm: {0}\n", Realm));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  CredentialType: {0}\n", CredentialType));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  ClientId: {0}\n", ClientId));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  FamilyId: {0}\n", FamilyId));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  Target: {0}\n", Target));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  CachedAt: {0}\n", CachedAt));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  ExpiresOn: {0}\n", ExpiresOn));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  ExtendedExpiresOn: {0}\n", ExtendedExpiresOn));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  Secret: {0}\n", Secret));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "  AdditionalFieldsJson: {0}\n", AdditionalFieldsJson));
            sb.Append(")");
            return sb.ToString();
        }
    }
}
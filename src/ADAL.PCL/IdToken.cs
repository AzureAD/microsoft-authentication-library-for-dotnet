//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class IdTokenClaim
    {
        public const string ObjectId = "oid";
        public const string Subject = "sub";
        public const string TenantId = "tid";
        public const string UPN = "upn";
        public const string Email = "email";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string IdentityProvider = "idp";
        public const string Issuer = "iss";
        public const string PasswordExpiration = "pwd_exp";
        public const string PasswordChangeUrl = "pwd_url";
    }

    [DataContract]
    internal class IdToken
    {
        [DataMember(Name = IdTokenClaim.ObjectId, IsRequired = false)]
        public string ObjectId { get; set; }

        [DataMember(Name = IdTokenClaim.Subject, IsRequired = false)]
        public string Subject { get; set; }

        [DataMember(Name = IdTokenClaim.TenantId, IsRequired = false)]
        public string TenantId { get; set; }

        [DataMember(Name = IdTokenClaim.UPN, IsRequired = false)]
        public string UPN { get; set; }

        [DataMember(Name = IdTokenClaim.GivenName, IsRequired = false)]
        public string GivenName { get; set; }

        [DataMember(Name = IdTokenClaim.FamilyName, IsRequired = false)]
        public string FamilyName { get; set; }

        [DataMember(Name = IdTokenClaim.Email, IsRequired = false)]
        public string Email { get; set; }

        [DataMember(Name = IdTokenClaim.PasswordExpiration, IsRequired = false)]
        public long PasswordExpiration { get; set; }

        [DataMember(Name = IdTokenClaim.PasswordChangeUrl, IsRequired = false)]
        public string PasswordChangeUrl { get; set; }

        [DataMember(Name = IdTokenClaim.IdentityProvider, IsRequired = false)]
        public string IdentityProvider { get; set; }

        [DataMember(Name = IdTokenClaim.Issuer, IsRequired = false)]
        public string Issuer { get; set; }

        public static IdToken Parse(string idToken)
        {
            IdToken idTokenBody = null;
            if (!string.IsNullOrWhiteSpace(idToken))
            {
                string[] idTokenSegments = idToken.Split(new[] { '.' });

                // If Id token format is invalid, we silently ignore the id token
                if (idTokenSegments.Length == 3)
                {
                    try
                    {
                        byte[] idTokenBytes = Base64UrlEncoder.DecodeBytes(idTokenSegments[1]);
                        using (var stream = new MemoryStream(idTokenBytes))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(IdToken));
                            idTokenBody = (IdToken)serializer.ReadObject(stream);
                        }
                    }
                    catch (SerializationException)
                    {
                        // We silently ignore the id token if exception occurs.   
                    }
                    catch (ArgumentException)
                    {
                        // Again, we silently ignore the id token if exception occurs.   
                    }
                }
            }

            return idTokenBody;
        }
    }
}

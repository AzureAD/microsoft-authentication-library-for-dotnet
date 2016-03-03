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

namespace Microsoft.Identity.Client.Internal
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

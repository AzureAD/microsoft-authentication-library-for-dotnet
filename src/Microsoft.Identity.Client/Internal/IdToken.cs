//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

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
                string[] idTokenSegments = idToken.Split(new[] {'.'});

                // If Id token format is invalid, we silently ignore the id token
                if (idTokenSegments.Length == 3)
                {
                    byte[] idTokenBytes = Base64UrlEncoder.DecodeBytes(idTokenSegments[1]);
                    using (var stream = new MemoryStream(idTokenBytes))
                    {
                        var serializer = new DataContractJsonSerializer(typeof(IdToken));
                        idTokenBody = (IdToken) serializer.ReadObject(stream);
                    }
                }
            }

            return idTokenBody;
        }

        public string GetUniqueId()
        {
            return ObjectId ?? Subject;
        }
    }
}
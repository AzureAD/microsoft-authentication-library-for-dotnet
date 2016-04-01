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
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.Common
{
    internal enum TokenCacheType
    {
        Null,
        InMemory
    }

    internal enum PromptBehaviorProxy
    {
        NotSpecified,
        Always,
        Auto,
        AccessCodeOnly,
        Never,
        RefreshSession
    }

    internal enum AuthenticationStatusProxy
    {
        Success = 0,
        ClientError = -1,
        ServiceError = -2
    }

    [DataContract]
    internal class AuthenticationResultProxy
    {
        [DataMember]
        public string AccessTokenType { get; set; }

        [DataMember]
        public string AccessToken { get; set; }

        [DataMember]
        public string IdToken { get; set; }

        [DataMember]
        public DateTimeOffset ExpiresOn { get; set; }

        [DataMember]
        public string TenantId { get; set; }

        [DataMember]
        public UserInfo UserInfo { get; set; }

        [DataMember]
        public AuthenticationStatusProxy Status { get; set; }

        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorDescription { get; set; }

        [DataMember]
        public string AuthenticationParametersAuthority { get; internal set; }

        [DataMember]
        public string AuthenticationParametersResource { get; internal set; }

        public Exception Exception { get; set; }

        public int ExceptionStatusCode { get; set; }

        public string[] ExceptionServiceErrorCodes { get; set; }

        internal static AuthenticationResultProxy Deserialize(string obj)
        {
            AuthenticationResultProxy output = null;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultProxy));
            byte[] serializedObjectBytes = Encoding.UTF8.GetBytes(obj);
            using (var stream = new MemoryStream(serializedObjectBytes))
            {
                output = (AuthenticationResultProxy)serializer.ReadObject(stream);
            }

            return output;
        }

        internal string Serialize()
        {
            string output = string.Empty;
            var serializer = new DataContractJsonSerializer(typeof(AuthenticationResultProxy));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, this);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    output = reader.ReadToEnd();
                }
            }

            return output;
        }
    }
}

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

using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.NET.Friend;

namespace Test.ADAL.Common
{
    public sealed class ClientAssertionProxy
    {
        public ClientAssertionProxy(string assertion)
        {
            this.Credential = new ClientAssertion(assertion);
        }

        public ClientAssertion Credential { get; set; }

        public static ClientAssertionProxy CreateFromCertificate(string authority, string clientId, string certificateName, string certificatePassword)
        {
            authority = authority.Replace("login", "sts");

            // Test fails with out this
            if (!authority.EndsWith(@"/"))
            {
                authority += @"/";
            }

            ClientAssertion credential = AdalFriend.CreateJwt(new X509Certificate2(certificateName + ".pfx", certificatePassword), clientId, authority);
            return new ClientAssertionProxy(credential.Assertion);
        }
    }

}

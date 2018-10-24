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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Core.Cache;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.ClientCreds;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.OAuth2;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;
using Test.Microsoft.Identity.Core.Unit;
using AuthorityType = Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Instance.AuthorityType;


namespace Test.ADAL.NET.Unit
{
#if DESKTOP
    [TestClass]
    public class SecureClientTests
    {
        [TestMethod]
        [Description("Tests for SecureClientSecret")]
        public void SecureClientSecretTest()
        {
            SecureString str = new SecureString();
            str.AppendChar('x');
            str.MakeReadOnly();
            SecureClientSecret secret = new SecureClientSecret(str);
            IDictionary<string, string> paramStr = new Dictionary<string, string>();
            secret.ApplyTo(paramStr);
            Assert.IsTrue(paramStr.ContainsKey("client_secret"));
            Assert.AreEqual("x", paramStr["client_secret"]);

            str = new SecureString();
            str.AppendChar('x');
            secret = new SecureClientSecret(str);
            paramStr = new Dictionary<string, string>();
            secret.ApplyTo(paramStr);
            Assert.IsTrue(paramStr.ContainsKey("client_secret"));
            Assert.AreEqual("x", paramStr["client_secret"]);
        }
    }
#endif
}

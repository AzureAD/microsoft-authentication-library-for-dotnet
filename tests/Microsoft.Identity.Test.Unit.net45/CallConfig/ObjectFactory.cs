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

using System.Collections.Generic;
using Microsoft.Identity.Client.CallConfig;
using Microsoft.Identity.Test.Common.Core.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CallConfig
{
    public static class ObjectFactory
    {
        internal static IEnumerable<string> CreateScopes()
        {
            return new List<string>
            {
                "r1/scope",
                "r2/scope2"
            };
        }

        internal static void ValidateAcquireTokenParameters(AcquireTokenParameters expected, AcquireTokenParameters actual)
        {
            ValidateDefaults(expected, actual);
        }

        private static void ValidateDefaults(AcquireTokenParameters expected, AcquireTokenParameters actual)
        {
            Assert.AreEqual(expected.Account, actual.Account);

            Assert.AreEqual(expected.AuthorityOverride, actual.AuthorityOverride);
            Assert.AreEqual(expected.ExtraQueryParameters, actual.ExtraQueryParameters);
            Assert.AreEqual(expected.LoginHint, actual.LoginHint);

            if (expected.ExtraScopesToConsent == null)
            {
                Assert.IsNull(actual.ExtraScopesToConsent);
            }
            else
            {
                CoreAssert.AreScopesEqual(
                    string.Join(" ", expected.ExtraScopesToConsent),
                    string.Join(" ", actual.ExtraScopesToConsent));
            }

            CoreAssert.AreScopesEqual(string.Join(" ", expected.Scopes), string.Join(" ", actual.Scopes));

            Assert.AreEqual(expected.ForceRefresh, actual.ForceRefresh);
            Assert.AreEqual(expected.WithForClientCertificate, actual.WithForClientCertificate);

            Assert.AreEqual(expected.Username, actual.Username);
            Assert.AreEqual(expected.Password, actual.Password);
            Assert.AreEqual(expected.AuthorizationCode, actual.AuthorizationCode);
            Assert.AreEqual(expected.DeviceCodeResultCallback, actual.DeviceCodeResultCallback);
            Assert.AreEqual(expected.RedirectUri, actual.RedirectUri);
            Assert.AreEqual(expected.UiBehavior, actual.UiBehavior);
            Assert.IsNotNull(actual.UiParent);
            Assert.AreEqual(expected.UseEmbeddedWebView, actual.UseEmbeddedWebView);
            Assert.AreEqual(expected.WithOnBehalfOfCertificate, actual.WithOnBehalfOfCertificate);
        }
    }
}
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
using System.Collections.Generic;
using System.Security;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.ApiConfig
{
    internal class AcquireTokenParameters : IAcquireTokenInteractiveParameters,
                                            IAcquireTokenWithDeviceCodeParameters,
                                            IAcquireTokenWithIntegratedWindowsAuthParameters,
                                            IAcquireTokenWithUsernamePasswordParameters,
                                            IAcquireTokenOnBehalfOfParameters,
                                            IAcquireTokenByAuthorizationCodeParameters,
                                            IAcquireTokenForClientParameters,
                                            IGetAuthorizationRequestUrlParameters,
                                            IAcquireTokenSilentParameters
    {
        public string AuthorizationCode { get; internal set; }
        public bool ForceRefresh { get; internal set; }
        public bool SendX5C { get; internal set; }

        // Interactive Parameters
        public bool UseEmbeddedWebView { get; internal set; }
        public UIBehavior UiBehavior { get; internal set; }
        public OwnerUiParent UiParent { get; internal set; } = new OwnerUiParent();

        // Common Parameters
        public IEnumerable<string> Scopes { get; internal set; }
        public string LoginHint { get; internal set; }

        /// <inheritdoc />
        public Dictionary<string, string> ExtraQueryParameters { get; internal set; }

        /// <inheritdoc />
        public IEnumerable<string> ExtraScopesToConsent { get; internal set; }

        public IAccount Account { get; internal set; }
        public string AuthorityOverride { get; internal set; }
        public UserAssertion UserAssertion { get; internal set; }
        public bool WithOnBehalfOfCertificate { get; internal set; }

        // DeviceCode Parameters
        public Func<DeviceCodeResult, Task> DeviceCodeResultCallback { get; internal set; }

        /// <inheritdoc />
        public string Username { get; internal set; }

        /// <inheritdoc />
        /// TODO(migration): DO NOT USE SECURESTRING -- https://github.com/dotnet/platform-compat/blob/master/docs/DE0001.md
        public SecureString Password { get; internal set; }

        public string RedirectUri { get; internal set; }
    }
}
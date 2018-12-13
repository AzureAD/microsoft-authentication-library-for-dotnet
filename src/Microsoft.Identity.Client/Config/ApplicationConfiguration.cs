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
using System.Linq;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Http;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.Config
{
    internal sealed class ApplicationConfiguration : IApplicationConfiguration
    {
        private readonly List<AuthorityInfo> _authorityInfos = new List<AuthorityInfo>();

        internal ApplicationConfiguration()
        {
            InitializeDefaults();
        }

        public string ClientId { get; internal set; }
        public string Tenant { get; internal set; }
        public string RedirectUri { get; internal set; } = Constants.DefaultRedirectUri;
        public bool EnablePiiLogging { get; internal set; }
        public LogLevel LogLevel { get; internal set; } = LogLevel.Warning;
        public bool IsDefaultPlatformLoggingEnabled { get; internal set; }
        public IMsalHttpClientFactory HttpClientFactory { get; internal set; }
        public IHttpManager HttpManager { get; set; }
        public bool IsExtendedTokenLifetimeEnabled { get; set; }
#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME // Hide confidential client on mobile platforms
        public ClientCredential ClientCredential { get; internal set; }
#endif
        public TokenCache AppTokenCache { get; internal set; }
        public TokenCache UserTokenCache { get; internal set; }
        public string SliceParameters { get; internal set; }

        public ITelemetryReceiver TelemetryReceiver { get; internal set; }
        public LogCallback LoggingCallback { get; internal set; }

        public IEnumerable<AuthorityInfo> Authorities => _authorityInfos.AsEnumerable();

        public AuthorityInfo DefaultAuthorityInfo => Authorities.Single(x => x.IsDefault);

        internal void AddAuthorityInfo(AuthorityInfo authorityInfo)
        {
            _authorityInfos.Add(authorityInfo);
        }

        private void InitializeDefaults()
        {
        }
    }
}
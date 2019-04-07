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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Core;

namespace Microsoft.Identity.Client.Instance
{
    internal abstract class Authority
    {
        internal static readonly HashSet<string> TenantlessTenantNames = new HashSet<string>(
            new[]
            {
                "common",
                "organizations",
                "consumers"
            });

        protected Authority(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            ServiceBundle = serviceBundle;
            AuthorityInfo = authorityInfo;
        }

        public AuthorityInfo AuthorityInfo { get; }

        protected IServiceBundle ServiceBundle { get; }

        public static Authority CreateAuthorityWithOverride(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            switch (authorityInfo.AuthorityType)
            {
            case AuthorityType.Adfs:
                throw new MsalClientException(
                    MsalError.InvalidAuthorityType,
                    "ADFS is not a supported authority");

            case AuthorityType.B2C:
                CheckB2CAuthorityHost(serviceBundle, authorityInfo);
                return new B2CAuthority(serviceBundle, authorityInfo);

            case AuthorityType.Aad:
                return new AadAuthority(serviceBundle, authorityInfo);

            default:
                throw new MsalClientException(
                    MsalError.InvalidAuthorityType,
                    "Unsupported authority type");
            }
        }

        public static Authority CreateAuthority(IServiceBundle serviceBundle, string authority, bool validateAuthority = false)
        {
            return CreateAuthorityWithOverride(
                serviceBundle,
                AuthorityInfo.FromAuthorityUri(authority, validateAuthority));
        }

        public static Authority CreateAuthority(IServiceBundle serviceBundle)
        {
            return CreateAuthorityWithOverride(serviceBundle, serviceBundle.Config.AuthorityInfo);
        }

        internal virtual Task UpdateCanonicalAuthorityAsync(RequestContext requestContext)
        {
            return Task.FromResult(0);
        }

        internal static string GetFirstPathSegment(string authority)
        {
            var uri = new Uri(authority);
            if (uri.Segments.Length > 1)
            {
                return uri.Segments[1].TrimEnd('/');
            }
            return string.Empty;
        }

        internal static AuthorityType GetAuthorityType(string authority)
        {
            string firstPathSegment = GetFirstPathSegment(authority);

            if (string.Equals(firstPathSegment, "adfs", StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.Adfs;
            }
            else if (string.Equals(firstPathSegment, B2CAuthority.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return AuthorityType.B2C;
            }
            else
            {
                return AuthorityType.Aad;
            }
        }

        internal abstract string GetTenantId();
        internal abstract void UpdateTenantId(string tenantId);

        internal static string CreateAuthorityUriWithHost(string authority, string host)
        {
            var uriBuilder = new UriBuilder(authority)
            {
                Host = host
            };

            return uriBuilder.Uri.AbsoluteUri;
        }

        private static void CheckB2CAuthorityHost(IServiceBundle serviceBundle, AuthorityInfo authorityInfo)
        {
            if (serviceBundle.Config.AuthorityInfo.Host != authorityInfo.Host)
            {
                throw new MsalClientException(MsalError.B2CAuthorityHostMismatch, MsalErrorMessage.B2CAuthorityHostMisMatch);
            }
        }

        internal static string GetEnviroment(string authority)
        {
            return new Uri(authority).Host;
        }
    }
}

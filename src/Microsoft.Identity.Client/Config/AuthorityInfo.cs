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
using System.Globalization;

namespace Microsoft.Identity.Client.Config
{
    internal class AuthorityInfo
    {
        public AuthorityInfo(AuthorityType authorityType, string authority, bool validateAuthority, bool isDefault)
        {
            AuthorityType = authorityType;
            ValidateAuthority = authorityType != AuthorityType.B2C && validateAuthority;
            IsDefault = isDefault;

            Host = new UriBuilder(authority).Host;

            UserRealmUriPrefix = string.Format(CultureInfo.InvariantCulture, "https://{0}/common/userrealm/", Host);

            if (AuthorityType == AuthorityType.B2C)
            {
                Uri authorityUri = new Uri(authority);
                string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (pathSegments.Length < 3)
                {
                    throw new ArgumentException(CoreErrorMessages.B2cAuthorityUriInvalidPath);
                }

                CanonicalAuthority = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/{2}/{3}/",
                    authorityUri.Authority,
                    pathSegments[0],
                    pathSegments[1],
                    pathSegments[2]);
            }
            else
            {
                var authorityUri = new UriBuilder(authority);
                CanonicalAuthority = string.Format(
                    CultureInfo.InvariantCulture,
                    "https://{0}/{1}/",
                    authorityUri.Uri.Authority,
                    GetFirstPathSegment(authority));
            }
        }

        internal static AuthorityInfo FromAuthorityUri(string authorityUri, bool validateAuthority, bool isDefaultAuthority)
        {
            string canonicalUri = CanonicalizeAuthorityUri(authorityUri);
            ValidateAuthorityUri(canonicalUri);

            var authorityType = Instance.Authority.GetAuthorityType(canonicalUri);
            return new AuthorityInfo(authorityType, canonicalUri, validateAuthority, isDefaultAuthority);
        }

        public string Host { get; }
        public string CanonicalAuthority { get; set; }

        public AuthorityType AuthorityType { get; }
        public bool ValidateAuthority { get; }
        public bool IsDefault { get; }

        public string UserRealmUriPrefix { get; }

        // TODO: consolidate this with the same method in Authority.cs
        private static string GetFirstPathSegment(string authority)
        {
            return new Uri(authority).Segments[1].TrimEnd('/');
        }

        private static string CanonicalizeAuthorityUri(string uri)
        {
            if (!string.IsNullOrWhiteSpace(uri) && !uri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                uri = uri + "/";
            }

            return uri.ToLowerInvariant();
        }

        private static void ValidateAuthorityUri(string authority)
        {
            if (string.IsNullOrWhiteSpace(authority))
            {
                throw new ArgumentNullException(nameof(authority));
            }

            if (!Uri.IsWellFormedUriString(authority, UriKind.Absolute))
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityInvalidUriFormat, nameof(authority));
            }

            var authorityUri = new Uri(authority);
            if (authorityUri.Scheme != "https")
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInsecure, nameof(authority));
            }

            string path = authorityUri.AbsolutePath.Substring(1);
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInvalidPath, nameof(authority));
            }

            string[] pathSegments = authorityUri.AbsolutePath.Substring(1).Split('/');
            if (pathSegments == null || pathSegments.Length == 0)
            {
                throw new ArgumentException(CoreErrorMessages.AuthorityUriInvalidPath);
            }
        }
    }
}
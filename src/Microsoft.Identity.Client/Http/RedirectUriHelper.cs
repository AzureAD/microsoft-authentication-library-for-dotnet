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
using System.Globalization;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Exceptions;

namespace Microsoft.Identity.Client.Http
{
    internal class RedirectUriHelper
    {
        /// <summary>
        /// Check common redirect uri problems. 
        /// Optionally check that the redirect uri is not the OAuth2 standard redirect uri urn:ietf:wg:oauth:2.0:oob
        /// when using a system browser, because the browser cannot redirect back to the app.
        /// </summary>
        public static void Validate(Uri redirectUri, bool usesSystemBrowser = false)
        {
            if (redirectUri == null)
            {
                throw MsalExceptionFactory.GetClientException(
                    MsalError.NoRedirectUri,
                    MsalErrorMessage.NoRedirectUri);

            }

            if (!string.IsNullOrWhiteSpace(redirectUri.Fragment))
            {
                throw new ArgumentException(
                    MsalErrorMessage.RedirectUriContainsFragment,
                    nameof(redirectUri));
            }

            if (usesSystemBrowser &&
                Constants.DefaultRedirectUri.Equals(redirectUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
            {
                throw MsalExceptionFactory.GetClientException(
                    MsalError.DefaultRedirectUriIsInvalid,
                    string.Format(
                        CultureInfo.InvariantCulture,
                        MsalErrorMessage.DefaultRedirectUriIsInvalid,
                        Constants.DefaultRedirectUri));
            }
        }

    }
}

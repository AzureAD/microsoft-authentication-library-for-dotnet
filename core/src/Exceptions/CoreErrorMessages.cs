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


namespace Microsoft.Identity.Core
{
    internal class CoreErrorMessages
    {
        public const string AuthorityInvalidUriFormat = "'authority' should be in Uri format";

        public const string AuthorityNotSupported = "'authority' is not supported";

        public const string AuthorityValidationFailed = "Authority validation failed";

        public const string AuthorityUriInsecure = "'authority' should use the 'https' scheme";

        public const string AuthorityUriInvalidPath =
         "'authority' Uri should have at least one segment in the path (i.e. https://<host>/<path>/...)";

        public const string B2cAuthorityUriInvalidPath =
          "B2C 'authority' Uri should have at least 3 segments in the path (i.e. https://<host>/tfp/<tenant>/<policy>/...)";

        public const string UnsupportedAuthorityValidation =
            "Authority validation is not supported for this type of authority";

        public const string AuthenticationCanceled = "User canceled authentication";

        public const string Unknown = "Unknown error";

        public const string AuthorizationServerInvalidResponse = "The authorization server returned an invalid response";

        public const string NonHttpsRedirectNotSupported = "Non-HTTPS url redirect is not supported in webview";

        public const string IDTokenMustHaveTwoParts = "ID Token must contain at least 2 parts.";
        public const string FailedToParseIDToken = "Failed to parse the returned id token.";

        public const string InvalidAuthroityOpenId = "invalid authority while getting the open id config endpoint";
        public const string UpnRequiredForAuthroityValidation = "UPN is required for ADFS authority validation.";
        public const string CannotFindTheAuthEndpont = "Cannot find the auth endpoint";
    }
}

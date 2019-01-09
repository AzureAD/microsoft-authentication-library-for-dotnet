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

namespace Microsoft.Identity.Client.ApiConfig
{
    /// <summary>
    ///     NOTE:  a few of the methods in AbstractAcquireTokenParameterBuilder (e.g. account) don't make sense here.
    ///     Do we want to create a further base that contains ALL of the common methods, and then have another one including
    ///     account, etc
    ///     that are only used for AcquireToken?
    /// </summary>
    public sealed class GetAuthorizationRequestUrlParameterBuilder :
        AbstractAcquireTokenParameterBuilder<GetAuthorizationRequestUrlParameterBuilder, IGetAuthorizationRequestUrlParameters>
    {
        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        internal static GetAuthorizationRequestUrlParameterBuilder Create(IEnumerable<string> scopes)
        {
            return new GetAuthorizationRequestUrlParameterBuilder().WithScopes(scopes);
        }

        /// <summary>
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        public GetAuthorizationRequestUrlParameterBuilder WithRedirectUri(string redirectUri)
        {
            Parameters.RedirectUri = redirectUri;
            return this;
        }
    }
}
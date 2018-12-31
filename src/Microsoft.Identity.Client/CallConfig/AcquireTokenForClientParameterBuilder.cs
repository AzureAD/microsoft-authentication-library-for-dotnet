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

namespace Microsoft.Identity.Client.CallConfig
{
    /// <summary>
    /// </summary>
    public sealed class AcquireTokenForClientParameterBuilder :
        AbstractAcquireTokenParameterBuilder<AcquireTokenForClientParameterBuilder>
    {
        /// <summary>
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public static AcquireTokenForClientParameterBuilder Create(IEnumerable<string> scopes)
        {
            return new AcquireTokenForClientParameterBuilder().WithScopes(scopes);
        }

        /// <summary>
        /// </summary>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public AcquireTokenForClientParameterBuilder WithForceRefresh(bool forceRefresh)
        {
            Parameters.ForceRefresh = forceRefresh;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="withCertificate"></param>
        /// <returns></returns>
        public AcquireTokenForClientParameterBuilder WithCertificate(bool withCertificate)
        {
            Parameters.WithForClientCertificate = withCertificate;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IAcquireTokenForClientParameters Build()
        {
            // validate

            return Parameters;
        }
    }
}
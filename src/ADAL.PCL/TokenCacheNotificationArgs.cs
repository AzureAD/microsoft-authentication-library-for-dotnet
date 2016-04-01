//------------------------------------------------------------------------------
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

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Contains parameters used by the ADAL call accessing the cache.
    /// </summary>
    public sealed class TokenCacheNotificationArgs
    {
        /// <summary>
        /// Gets the TokenCache
        /// </summary>
        public TokenCache TokenCache { get; internal set; }

        /// <summary>
        /// Gets the ClientId.
        /// </summary>
        public string ClientId { get; internal set; }

        /// <summary>
        /// Gets the Resource.
        /// </summary>
        public string Resource { get; internal set; }

        /// <summary>
        /// Gets the user's unique Id.
        /// </summary>
        public string UniqueId { get; internal set; }

        /// <summary>
        /// Gets the user's displayable Id.
        /// </summary>
        public string DisplayableId { get; internal set; }
    }
}

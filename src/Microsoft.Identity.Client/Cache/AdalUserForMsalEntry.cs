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

namespace Microsoft.Identity.Client.Cache
{
    internal class AdalUserForMsalEntry
    {
        public AdalUserForMsalEntry(string clientId, string authority, string clientInfo, AdalUserInfo userInfo)
        {
            ClientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            Authority = authority;
            ClientInfo = clientInfo;
            UserInfo = userInfo ?? throw new ArgumentNullException(nameof(userInfo));
        }

        public string ClientId { get; }
        public string Authority { get; }
        public string ClientInfo { get; } // optional, ADAL v3 doesn't have this
        public AdalUserInfo UserInfo { get; }
    }
}

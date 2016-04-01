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
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Test.ADAL.WinRT.Unit
{
    class ReplayerWebUI : ReplayerBase, IWebUI
    {
        private const string Delimiter = ":::";

        public ReplayerWebUI(IPlatformParameters parameters)
        {
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri authorizationUri, Uri redirectUri, CallState callState)
        {
            return await Task.Factory.StartNew(() =>
            {
                string key = authorizationUri.AbsoluteUri + redirectUri.OriginalString;

                if (IOMap.ContainsKey(key))
                {
                    string value = IOMap[key];
                    if (value[0] == 'P')
                    {
                        value = value.Substring(1);
                        string[] valueSegments = value.Split(new string[] {"::"}, StringSplitOptions.None);
                        return
                            new AuthorizationResult(
                                (AuthorizationStatus) Enum.Parse(typeof (AuthorizationStatus), valueSegments[0]),
                                valueSegments[1]);
                    }

                    if (value[0] == 'A')
                    {
                        string[] segments = value.Substring(1)
                            .Split(new[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries);
                        return new AuthorizationResult(AuthorizationStatus.Success,
                            string.Format(CultureInfo.CurrentCulture, " https://dummy?error={0}&error_description={1}", segments[0], segments[1]));
                    }
                }

                return null;
            });
        }
    }
}

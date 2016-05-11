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

namespace Test.ADAL.NET.Friend
{
    internal class RecorderWebUI : RecorderBase, IWebUI
    {
        private const string Delimiter = ":::";
        private readonly IWebUI internalWebUI;

        static RecorderWebUI()
        {
            Initialize();
        }

        public RecorderWebUI(IPlatformParameters parameters)
        {
            this.internalWebUI = (new WebUIFactory()).CreateAuthenticationDialog(parameters);
        }

        public async Task<AuthorizationResult> AcquireAuthorizationAsync(Uri requestUri, Uri callbackUri, CallState callState)
        {
            string key = requestUri.AbsoluteUri + callbackUri.OriginalString;
            string value = null;
            key = key.Replace("&haschrome=1","");

            if (IOMap.ContainsKey(key))
            {
                value = IOMap[key];
                if (value[0] == 'P')
                {
                    value = value.Substring(1);
                    string[] valueSegments = value.Split(new [] {"::"}, StringSplitOptions.None);
                    return new AuthorizationResult((AuthorizationStatus)Enum.Parse(typeof(AuthorizationStatus), valueSegments[0]), valueSegments[1]);
                }               
                
                if (value[0] == 'A')
                {
                    string []segments = value.Substring(1).Split(new [] { Delimiter }, StringSplitOptions.RemoveEmptyEntries);
                    throw new AdalServiceException(errorCode: segments[0], message: segments[1])
                          {
                              StatusCode = int.Parse(segments[2])
                          };
                }
            }

            try
            {
                AuthorizationResult result = await this.internalWebUI.AcquireAuthorizationAsync(requestUri, callbackUri, callState);
                const string DummyUri = "https://temp_uri";
                switch (result.Status)
                {
                    case AuthorizationStatus.Success: value = string.Format(CultureInfo.CurrentCulture, "{0}?code={1}", DummyUri, result.Code); break;
                    case AuthorizationStatus.UserCancel: value = string.Empty; break;
                    case AuthorizationStatus.ProtocolError: value = string.Format(CultureInfo.CurrentCulture, "{0}?error={1}&error_description={2}", DummyUri, result.Error, result.ErrorDescription); break;
                    default: value = string.Empty; break;
                }

                value = "P" + result.Status + "::" + value;

                return result;
            }
            catch (AdalException ex)
            {
                AdalServiceException serviceException = ex as AdalServiceException;
                if (serviceException != null && serviceException.StatusCode == 503)
                {
                    value = null;
                }
                else
                {
                    value = 'A' + string.Format(CultureInfo.CurrentCulture, "{0}{1}{2}{3}{4}", ex.ErrorCode, Delimiter, ex.Message, Delimiter, 
                        (serviceException != null) ? serviceException.StatusCode : 0);
                }
                
                throw;
            }
            finally
            {
                if (value != null)
                {
                    IOMap.Add(key, value);
                }
            }
        }
    }
}

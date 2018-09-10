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
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Http;

namespace Microsoft.Identity.Core.WsTrust
{
    internal static class WsTrustRequest
    {
        private const int MaxExpectedMessageSize = 1024;
        private const int ExpiryInMinutes = 10;

        public static async Task<WsTrustResponse> SendRequestAsync(
            WsTrustAddress wsTrustAddress, 
            string wsTrustRequest, 
            RequestContext requestContext)
        {
            var headers = new Dictionary<string, string>
            {
                { "ContentType", "application/soap+xml" },
                {"SOAPAction", (wsTrustAddress.Version == WsTrustVersion.WsTrust2005) ? XmlNamespace.Issue2005.ToString() : XmlNamespace.Issue.ToString() }
            };
            var body = new StringContent(
                wsTrustRequest,
                Encoding.UTF8, headers["ContentType"]);
            var resp = await HttpRequest.SendPostAsync(wsTrustAddress.Uri, headers, body, requestContext).ConfigureAwait(false);
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                string errorMessage = null;
                try
                {
                    errorMessage = WsTrustResponse.ReadErrorResponse(XDocument.Parse(resp.Body, LoadOptions.None), requestContext);
                }
                catch (System.Xml.XmlException)
                {
                    errorMessage = resp.Body;
                }
                throw CoreExceptionFactory.Instance.GetServiceException(
                    CoreErrorCodes.FederatedServiceReturnedError,
                    string.Format(CultureInfo.CurrentCulture, CoreErrorMessages.FederatedServiceReturnedErrorTemplate, wsTrustAddress.Uri, errorMessage)
                );
            }
            try
            {
                return WsTrustResponse.CreateFromResponse(resp.Body, wsTrustAddress.Version);
            }
            catch (System.Xml.XmlException ex)
            {
                throw CoreExceptionFactory.Instance.GetServiceException(
                    CoreErrorCodes.ParsingWsTrustResponseFailed, CoreErrorCodes.ParsingWsTrustResponseFailed, ex);
            }
        }
    }
}
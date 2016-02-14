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

using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Interfaces;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory.Internal
{
    internal static class WsTrustRequest
    {
        private const int MaxExpectedMessageSize = 1024;

        // appliesTo like urn:federation:MicrosoftOnline. Either wst:TokenType or wst:AppliesTo should be defined in the token request message. 
        // If both are specified, the wst:AppliesTo field takes precedence.
        // If we don't specify TokenType, it will return SAML v1.1
        private const string WsTrustEnvelopeTemplate =
            @"<s:Envelope xmlns:s='http://www.w3.org/2003/05/soap-envelope' xmlns:a='http://www.w3.org/2005/08/addressing' xmlns:u='{0}'>
              <s:Header>
              <a:Action s:mustUnderstand='1'>{1}</a:Action>
              <a:messageID>urn:uuid:{2}</a:messageID>
              <a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>
              <a:To s:mustUnderstand='1'>{3}</a:To>
              {4}
              </s:Header>
              <s:Body>
              <trust:RequestSecurityToken xmlns:trust='{5}'>
              <wsp:AppliesTo xmlns:wsp='http://schemas.xmlsoap.org/ws/2004/09/policy'>
              <a:EndpointReference>
              <a:Address>{6}</a:Address>
              </a:EndpointReference>
              </wsp:AppliesTo>
              <trust:KeyType>{7}</trust:KeyType>
              <trust:RequestType>{8}</trust:RequestType>
              </trust:RequestSecurityToken>
              </s:Body>
              </s:Envelope>";

        // We currently send this for all requests. We may need to change it in the future.
        private const string DefaultAppliesTo = "urn:federation:MicrosoftOnline";

        public static async Task<WsTrustResponse> SendRequestAsync(WsTrustAddress wsTrustAddress, UserCredential credential, CallState callState)
        {
            HttpClientWrapper request = new HttpClientWrapper(wsTrustAddress.Uri.AbsoluteUri, callState);
            request.ContentType = "application/soap+xml";
            if (credential.UserAuthType == UserAuthType.IntegratedAuth)
            {
                SetKerberosOption(request);
            }

            StringBuilder messageBuilder = BuildMessage(DefaultAppliesTo, wsTrustAddress, credential);
            WsTrustResponse wstResponse;

            try
            {
                request.BodyParameters = new StringRequestParameters(messageBuilder);
                IHttpWebResponse response = await request.GetResponseAsync().ConfigureAwait(false);
                wstResponse = WsTrustResponse.CreateFromResponse(response.ResponseStream, wsTrustAddress.Version);
            }
            catch (WebException ex)
            {
                string errorMessage;

                try
                {
                    XDocument responseDocument = WsTrustResponse.ReadDocumentFromResponse(ex.Response.GetResponseStream());
                    errorMessage = WsTrustResponse.ReadErrorResponse(responseDocument, callState);
                }
                catch (MsalException)
                {
                    errorMessage = "See inner exception for detail.";
                }

                throw new MsalServiceException(
                    MsalError.FederatedServiceReturnedError,
                    string.Format(MsalErrorMessage.FederatedServiceReturnedErrorTemplate, wsTrustAddress.Uri, errorMessage),
                    null,
                    ex);
            }

            return wstResponse;
        }

        private static void SetKerberosOption(HttpClientWrapper request)
        {
            request.UseDefaultCredentials = true;
        }

        public static StringBuilder BuildMessage(string appliesTo, WsTrustAddress wsTrustAddress,
            UserCredential credential)
        {
            // securityHeader will be empty string for Kerberos.
            StringBuilder securityHeaderBuilder = new StringBuilder(MaxExpectedMessageSize);

            string guid = Guid.NewGuid().ToString();
            StringBuilder messageBuilder = new StringBuilder(MaxExpectedMessageSize);
            string schemaLocation = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            string soapAction = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";
            string rstTrustNamespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
            string keyType = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer";
            string requestType = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue";

            if (wsTrustAddress.Version == WsTrustVersion.WsTrust2005)
            {
                soapAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";
                rstTrustNamespace = "http://schemas.xmlsoap.org/ws/2005/02/trust";
                keyType = "http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey";
                requestType = "http://schemas.xmlsoap.org/ws/2005/02/trust/Issue";
            }

            messageBuilder.AppendFormat(WsTrustEnvelopeTemplate,
                schemaLocation, soapAction,
                                guid, wsTrustAddress.Uri, securityHeaderBuilder,
                                rstTrustNamespace, appliesTo, keyType,
                                requestType);
            securityHeaderBuilder.SecureClear();

            return messageBuilder;
        }

        internal static string XmlEscape(string escapeStr)
        {
            escapeStr = escapeStr.Replace("&", "&amp;");
            escapeStr = escapeStr.Replace("\"", "&quot;");
            escapeStr = escapeStr.Replace("'", "&apos;");
            escapeStr = escapeStr.Replace("<", "&lt;");
            escapeStr = escapeStr.Replace(">", "&gt;");
            return escapeStr;
        }

        private static string BuildTimeString(DateTime utcTime)
        {
            return utcTime.ToString("yyyy-MM-ddTHH:mm:ss.068Z", CultureInfo.InvariantCulture);
        }
    }
}
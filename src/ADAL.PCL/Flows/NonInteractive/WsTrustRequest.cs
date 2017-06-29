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
using System.IO;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
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
              <a:MessageID>urn:uuid:{2}</a:MessageID>
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

        private const string defaultAppliesTo = "urn:federation:MicrosoftOnline";

        public static async Task<WsTrustResponse> SendRequestAsync(WsTrustAddress wsTrustAddress, UserCredential credential, CallState callState, string cloudAudience)
        {
            IHttpClient request = PlatformPlugin.HttpClientFactory.Create(wsTrustAddress.Uri.AbsoluteUri, callState);
            request.ContentType = "application/soap+xml";
            if (credential.UserAuthType == UserAuthType.IntegratedAuth)
            {
                SetKerberosOption(request);
            }

            if (string.IsNullOrEmpty(cloudAudience))
            {
                cloudAudience = defaultAppliesTo;
            }

            StringBuilder messageBuilder = BuildMessage(cloudAudience, wsTrustAddress, credential);
            string soapAction = XmlNamespace.Issue.ToString();
            if (wsTrustAddress.Version == WsTrustVersion.WsTrust2005)
            {
                soapAction = XmlNamespace.Issue2005.ToString();
            }

            WsTrustResponse wstResponse;

            try
            {
                request.BodyParameters = new StringRequestParameters(messageBuilder);
                request.Headers["SOAPAction"] = soapAction;
                IHttpWebResponse response = await request.GetResponseAsync().ConfigureAwait(false);
                wstResponse = WsTrustResponse.CreateFromResponse(EncodingHelper.GenerateStreamFromString(response.ResponseString), wsTrustAddress.Version);
            }
            catch (HttpRequestWrapperException ex)
            {
                string errorMessage;

                try
                {
                    using (Stream stream = EncodingHelper.GenerateStreamFromString(ex.WebResponse.ResponseString))
                    {
                        XDocument responseDocument = WsTrustResponse.ReadDocumentFromResponse(stream);
                        errorMessage = WsTrustResponse.ReadErrorResponse(responseDocument, callState);
                    }
                }
                catch (AdalException)
                {
                    errorMessage = "See inner exception for detail.";
                }

                throw new AdalServiceException(
                    AdalError.FederatedServiceReturnedError,
                    string.Format(CultureInfo.CurrentCulture, AdalErrorMessage.FederatedServiceReturnedErrorTemplate, wsTrustAddress.Uri, errorMessage),
                    null,
                    ex);
            }

            return wstResponse;
        }

        private static void SetKerberosOption(IHttpClient request)
        {
            request.UseDefaultCredentials = true;
        }

        public static StringBuilder BuildMessage(string appliesTo, WsTrustAddress wsTrustAddress,
            UserCredential credential)
        {
            // securityHeader will be empty string for Kerberos.
            StringBuilder securityHeaderBuilder = BuildSecurityHeader(wsTrustAddress, credential);

            string guid = Guid.NewGuid().ToString();
            StringBuilder messageBuilder = new StringBuilder(MaxExpectedMessageSize);
            String schemaLocation = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
            String soapAction = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue";

            // Note: the real namespace has a trailing slash, but the server doesn't expect this so we have to use
            // the following version
            String rstTrustNamespace = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";

            String keyType = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer";
            String requestType = "http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue";

            if (wsTrustAddress.Version == WsTrustVersion.WsTrust2005)
            {
                soapAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue";
                rstTrustNamespace = "http://schemas.xmlsoap.org/ws/2005/02/trust";
                keyType = "http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey";
                requestType = "http://schemas.xmlsoap.org/ws/2005/02/trust/Issue";
            }

            messageBuilder.AppendFormat(CultureInfo.CurrentCulture, WsTrustEnvelopeTemplate,
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

        private static StringBuilder BuildSecurityHeader(WsTrustAddress address, UserCredential credential)
        {
            StringBuilder securityHeaderBuilder = new StringBuilder(MaxExpectedMessageSize);

            // Not add <Security> element if the credential type is kerberos
            if (credential.UserAuthType == UserAuthType.UsernamePassword)
            {
                StringBuilder messageCredentialsBuilder = new StringBuilder(MaxExpectedMessageSize);
                string guid = Guid.NewGuid().ToString();
                messageCredentialsBuilder.AppendFormat(CultureInfo.CurrentCulture,
                    "<o:UsernameToken u:Id='uuid-{0}'><o:Username>{1}</o:Username><o:Password>", guid,
                    credential.UserName);
                char[] passwordChars = null;
                try
                {
                    passwordChars = credential.PasswordToCharArray();
                    string escapeStr = XmlEscape(new string(passwordChars));
                    messageCredentialsBuilder.Append(escapeStr);
                    escapeStr = "";
                }
                finally
                {
                    passwordChars.SecureClear();
                }

                messageCredentialsBuilder.AppendFormat(CultureInfo.CurrentCulture, "</o:Password></o:UsernameToken>");

                //
                // Timestamp the message
                //
                DateTime currentTime = DateTime.UtcNow;
                string currentTimeString = BuildTimeString(currentTime);

                // Expiry is 10 minutes after creation
                DateTime expiryTime = currentTime.AddMinutes(10);
                string expiryTimeString = BuildTimeString(expiryTime);

                securityHeaderBuilder.AppendFormat(CultureInfo.CurrentCulture,
                    "<o:Security s:mustUnderstand='1' xmlns:o='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'><u:Timestamp u:Id='_0'><u:Created>{0}</u:Created><u:Expires>{1}</u:Expires></u:Timestamp>{2}</o:Security>",
                    currentTimeString,
                    expiryTimeString,
                    messageCredentialsBuilder);

                messageCredentialsBuilder.SecureClear();
            }

            return securityHeaderBuilder;
        }

        private static string BuildTimeString(DateTime utcTime)
        {
            return utcTime.ToString("yyyy-MM-ddTHH:mm:ss.068Z", CultureInfo.InvariantCulture);
        }
    }
}
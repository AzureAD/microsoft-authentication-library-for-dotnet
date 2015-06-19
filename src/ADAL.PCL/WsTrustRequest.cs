//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Globalization;
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
            @"<s:Envelope xmlns:s='http://www.w3.org/2003/05/soap-envelope' xmlns:a='http://www.w3.org/2005/08/addressing' xmlns:u='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd'>
              <s:Header>
              <a:Action s:mustUnderstand='1'>http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</a:Action>
              <a:messageID>urn:uuid:{0}</a:messageID>
              <a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>
              <a:To s:mustUnderstand='1'>{1}</a:To>
              {2}
              </s:Header>
              <s:Body>
              <trust:RequestSecurityToken xmlns:trust='http://docs.oasis-open.org/ws-sx/ws-trust/200512'>
              <wsp:AppliesTo xmlns:wsp='http://schemas.xmlsoap.org/ws/2004/09/policy'>
              <a:EndpointReference>
              <a:Address>{3}</a:Address>
              </a:EndpointReference>
              </wsp:AppliesTo>
              <trust:KeyType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer</trust:KeyType>
              <trust:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</trust:RequestType>
              </trust:RequestSecurityToken>
              </s:Body>
              </s:Envelope>";

        // We currently send this for all requests. We may need to change it in the future.
        private const string DefaultAppliesTo = "urn:federation:MicrosoftOnline";

        public static async Task<WsTrustResponse> SendRequestAsync(Uri url, UserCredential credential, CallState callState)
        {
            IHttpClient request = PlatformPlugin.HttpClientFactory.Create(url.AbsoluteUri, callState);
            request.ContentType = "application/soap+xml";
            if (credential.UserAuthType == UserAuthType.IntegratedAuth)
            {
                SetKerberosOption(request);
            }

            StringBuilder messageBuilder = BuildMessage(DefaultAppliesTo, url.AbsoluteUri, credential);
            request.Headers["SOAPAction"] = XmlNamespace.Issue.ToString();

            WsTrustResponse wstResponse;

            try
            {
                request.BodyParameters = new StringRequestParameters(messageBuilder);
                IHttpWebResponse response = await request.GetResponseAsync();
                wstResponse = WsTrustResponse.CreateFromResponse(response.ResponseStream);
            }
            catch (HttpRequestWrapperException ex)
            {
                string errorMessage;

                try
                {
                    XDocument responseDocument = WsTrustResponse.ReadDocumentFromResponse(ex.WebResponse.ResponseStream);
                    errorMessage = WsTrustResponse.ReadErrorResponse(responseDocument, callState);
                }
                catch (AdalException)
                {
                    errorMessage = "See inner exception for detail.";
                }

                throw new AdalServiceException(
                    AdalError.FederatedServiceReturnedError,
                    string.Format(AdalErrorMessage.FederatedServiceReturnedErrorTemplate, url, errorMessage),
                    null,
                    ex);
            }

            return wstResponse;
        }

        private static void SetKerberosOption(IHttpClient request)
        {
            request.UseDefaultCredentials = true;
        }

        public static StringBuilder BuildMessage(string appliesTo, string resource, UserCredential credential)
        {
            // securityHeader will be empty string for Kerberos.
            StringBuilder securityHeaderBuilder = BuildSecurityHeader(credential);

            string guid = Guid.NewGuid().ToString();
            StringBuilder messageBuilder = new StringBuilder(MaxExpectedMessageSize);
            messageBuilder.AppendFormat(WsTrustEnvelopeTemplate, guid, resource, securityHeaderBuilder, appliesTo);

            securityHeaderBuilder.SecureClear();

            return messageBuilder;
        }

        private static StringBuilder BuildSecurityHeader(UserCredential credential)
        {
            StringBuilder securityHeaderBuilder = new StringBuilder(MaxExpectedMessageSize);

            // Not add <Security> element if the credential type is kerberos
            if (credential.UserAuthType == UserAuthType.UsernamePassword)
            {
                StringBuilder messageCredentialsBuilder = new StringBuilder(MaxExpectedMessageSize);
                string guid = Guid.NewGuid().ToString();

                messageCredentialsBuilder.AppendFormat("<o:UsernameToken u:Id='uuid-{0}'><o:Username>{1}</o:Username><o:Password>", guid, credential.UserName);

                char[] passwordChars = null;
                try
                {
                    passwordChars = credential.EscapedPasswordToCharArray();
                    messageCredentialsBuilder.Append(passwordChars);
                }
                finally
                {
                    passwordChars.SecureClear();
                }

                messageCredentialsBuilder.AppendFormat("</o:Password></o:UsernameToken>");

                //
                // Timestamp the message
                //
                DateTime currentTime = DateTime.UtcNow;
                string currentTimeString = BuildTimeString(currentTime);

                // Expiry is 10 minutes after creation
                DateTime expiryTime = currentTime.AddMinutes(10);    
                string expiryTimString = BuildTimeString(expiryTime);

                securityHeaderBuilder.AppendFormat(
                    "<o:Security s:mustUnderstand='1' xmlns:o='http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd'><u:Timestamp u:Id='_0'><u:Created>{0}</u:Created><u:Expires>{1}</u:Expires></u:Timestamp>{2}</o:Security>", 
                    currentTimeString, 
                    expiryTimString, 
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

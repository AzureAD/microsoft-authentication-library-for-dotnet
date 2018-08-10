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
using System.Text;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Helpers;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.OAuth2;

namespace Microsoft.Identity.Client.Internal.Requests
{
    internal class AuthenticationRequestParameters
    {
        public RequestContext RequestContext { get; set; }

        public Authority Authority { get; set; }

        public string TenantUpdatedCanonicalAuthority { get; set; }

        public bool ValidateAuthority { get; set; }

        public TokenCache TokenCache { get; set; }

        public SortedSet<string> Scope { get; set; }

        public string ClientId { get; set; }

        public string AuthorizationCode { get; set; }

        public Uri RedirectUri { get; set; }

        public string LoginHint { get; set; }

        public string ExtraQueryParameters { get; set; }

        public IAccount Account { get; set; }

        public ClientInfo ClientInfo { get; set; }

        public UserAssertion UserAssertion { get; set; }

        public bool IsClientCredentialRequest { get; set; } = false;

        public string SliceParameters { get; set; }

        public bool SendCertificate { get; set; }

#if DESKTOP || NETSTANDARD1_3 || NET_CORE
        public ClientCredential ClientCredential { get; set; }
#endif

        public IDictionary<string, string> ToParameters()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();
#if DESKTOP || NETSTANDARD1_3 || NET_CORE
            if (ClientCredential != null)
            {
                if (!string.IsNullOrEmpty(ClientCredential.Secret))
                {
                    parameters[OAuth2Parameter.ClientSecret] = ClientCredential.Secret;
                }
                else
                {
                    if (ClientCredential.Assertion == null || ClientCredential.ValidTo != 0)
                    {
                        if (!RequestValidationHelper.ValidateClientAssertion(this))
                        {
                            const string msg = "Client Assertion does not exist or near expiry.";
                            RequestContext.Logger.Info(msg);
                            RequestContext.Logger.InfoPii(msg);
                            Jwt.JsonWebToken jwtToken = new Jwt.JsonWebToken(ClientId,
                                Authority.SelfSignedJwtAudience);
                            ClientCredential.Assertion = jwtToken.Sign(ClientCredential.Certificate, SendCertificate);
                            ClientCredential.ValidTo = jwtToken.Payload.ValidTo;
                            ClientCredential.ContainsX5C = SendCertificate;
                            ClientCredential.Audience = Authority.SelfSignedJwtAudience;
                        }
                        else
                        {
                            const string msg = "Reusing the unexpired Client Assertion...";
                            RequestContext.Logger.Info(msg);
                            RequestContext.Logger.InfoPii(msg);
                        }
                    }

                    parameters[OAuth2Parameter.ClientAssertionType] = OAuth2AssertionType.JwtBearer;
                    parameters[OAuth2Parameter.ClientAssertion] = ClientCredential.Assertion;
                }
            }
#endif
            return parameters;
        }

        public void LogState()
        {
            // Create Pii enabled string builder
            StringBuilder builder = new StringBuilder(Environment.NewLine + "=== Request Data ===" + Environment.NewLine +
                                                      "Authority Provided? - " + (Authority != null) + Environment.NewLine);
            builder.AppendLine("Client Id - " + ClientId);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Redirect Uri - " + RedirectUri?.OriginalString);
            builder.AppendLine("Validate Authority? - " + ValidateAuthority);
            builder.AppendLine("LoginHint provided? - " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("User provided? - " + (Account != null));
            var dict = CoreHelpers.ParseKeyValueList(ExtraQueryParameters, '&', true, RequestContext);
            builder.AppendLine("Extra Query Params Keys (space separated) - " + dict.Keys.AsSingleString());
            dict = CoreHelpers.ParseKeyValueList(SliceParameters, '&', true, RequestContext);
            builder.AppendLine("Slice Parameters Keys(space separated) - " + dict.Keys.AsSingleString());
#if DESKTOP || NETSTANDARD1_3 || NET_CORE
            builder.AppendLine("Confidential Client? - " + (ClientCredential != null));
            builder.AppendLine("Client Credential Request? - " + IsClientCredentialRequest);
            if (IsClientCredentialRequest)
            {
                builder.AppendLine("Client Certificate Provided? - " + (ClientCredential.Certificate != null));
            }
#endif
            RequestContext.Logger.InfoPii(builder.ToString());

            // Create no Pii enabled string builder
            builder = new StringBuilder(Environment.NewLine + "=== Request Data ===" + Environment.NewLine +
                                        "Authority Provided? - " + (Authority != null) + Environment.NewLine);
            builder.AppendLine("Scopes - " + Scope?.AsSingleString());
            builder.AppendLine("Validate Authority? - " + ValidateAuthority);
            builder.AppendLine("LoginHint provided? - " + !string.IsNullOrEmpty(LoginHint));
            builder.AppendLine("User provided? - " + (Account != null));
            dict = CoreHelpers.ParseKeyValueList(ExtraQueryParameters, '&', true, RequestContext);
            builder.AppendLine("Extra Query Params Keys (space separated) - " + dict.Keys.AsSingleString());
            dict = CoreHelpers.ParseKeyValueList(SliceParameters, '&', true, RequestContext);
            builder.AppendLine("Slice Parameters Keys(space separated) - " + dict.Keys.AsSingleString());
#if DESKTOP || NETSTANDARD1_3 || NET_CORE
            builder.AppendLine("Confidential Client? - " + (ClientCredential != null));
            builder.AppendLine("Client Credential Request? - " + IsClientCredentialRequest);
            if (IsClientCredentialRequest)
            {
                builder.AppendLine("Client Certificate Provided? - " + (ClientCredential.Certificate != null));
            }
#endif
            RequestContext.Logger.Info(builder.ToString());
        }
    }
}
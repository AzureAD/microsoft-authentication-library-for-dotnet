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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal class WsTrustResponse
    {
        public const string Saml1Assertion = "urn:oasis:names:tc:SAML:1.0:assertion";

        public string Token { get; private set; }

        public string TokenType { get; private set; }

        public static WsTrustResponse CreateFromResponse(Stream responseStream, WsTrustVersion version)
        {
            XDocument responseDocument = ReadDocumentFromResponse(responseStream);
            return CreateFromResponseDocument(responseDocument, version);
        }

        public static string ReadErrorResponse(XDocument responseDocument, CallState callState)
        {
            string errorMessage = null;

            try
            {
                XElement body = responseDocument.Descendants(XmlNamespace.SoapEnvelope + "Body").FirstOrDefault();

                if (body != null)
                {
                    XElement fault = body.Elements(XmlNamespace.SoapEnvelope + "Fault").FirstOrDefault();
                    if (fault != null)
                    {
                        XElement reason = fault.Elements(XmlNamespace.SoapEnvelope + "Reason").FirstOrDefault();
                        if (reason != null)
                        {
                            XElement text = reason.Elements(XmlNamespace.SoapEnvelope + "Text").FirstOrDefault();
                            if (text != null)
                            {
                                using (var reader = text.CreateReader())
                                {
                                    reader.MoveToContent();
                                    errorMessage = reader.ReadInnerXml();
                                }
                            }
                        }
                    }
                }
            }
            catch (XmlException ex)
            {
                throw new AdalException(AdalError.ParsingWsTrustResponseFailed, ex);
            }

            return errorMessage;
        }

        internal static XDocument ReadDocumentFromResponse(Stream responseStream)
        {
            try
            {
                return XDocument.Load(responseStream, LoadOptions.None);
            }
            catch (XmlException ex)
            {
                throw new AdalException(AdalError.ParsingWsTrustResponseFailed, ex);
            }
        }

        internal static WsTrustResponse CreateFromResponseDocument(XDocument responseDocument, WsTrustVersion version)
        {
            Dictionary<string, string> tokenResponseDictionary = new Dictionary<string, string>();

            try
            {
                XNamespace t = XmlNamespace.Trust;
                if (version == WsTrustVersion.WsTrust2005)
                {
                    t = XmlNamespace.Trust2005;
                }
                bool parseResponse = true;
                if (version == WsTrustVersion.WsTrust13)
                {
                    XElement requestSecurityTokenResponseCollection =
                        responseDocument.Descendants(t + "RequestSecurityTokenResponseCollection").FirstOrDefault();

                    if (requestSecurityTokenResponseCollection == null)
                    {
                        parseResponse = false;
                    }
                }

                if (parseResponse)
                {
                    IEnumerable<XElement> tokenResponses =
                        responseDocument.Descendants(t + "RequestSecurityTokenResponse");
                    foreach (var tokenResponse in tokenResponses)
                    {
                        XElement tokenTypeElement = tokenResponse.Elements(t + "TokenType").FirstOrDefault();
                        if (tokenTypeElement == null)
                        {
                            continue;
                        }

                        XElement requestedSecurityToken =
                            tokenResponse.Elements(t + "RequestedSecurityToken").FirstOrDefault();
                        if (requestedSecurityToken == null)
                        {
                            continue;
                        }

                        // TODO #123622: We need to disable formatting due to a potential service bug. Remove the ToString argument when problem is fixed.
                        tokenResponseDictionary.Add(tokenTypeElement.Value,
                            requestedSecurityToken.FirstNode.ToString(SaveOptions.DisableFormatting));
                    }
                }
            }
            catch (XmlException ex)
            {
                throw new AdalException(AdalError.ParsingWsTrustResponseFailed, ex);
            }

            if (tokenResponseDictionary.Count == 0)
            {
                throw new AdalException(AdalError.ParsingWsTrustResponseFailed);
            }

            string tokenType = tokenResponseDictionary.ContainsKey(Saml1Assertion) ? Saml1Assertion : tokenResponseDictionary.Keys.First();

            WsTrustResponse wsTrustResponse = new WsTrustResponse
            {
                TokenType = tokenType,
                Token = tokenResponseDictionary[tokenType]
            };

            return wsTrustResponse;
        }
    }
}

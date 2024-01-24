// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Identity.Client.Internal;

namespace Microsoft.Identity.Client.WsTrust
{
    internal class WsTrustResponse
    {
        public const string Saml1Assertion = "urn:oasis:names:tc:SAML:1.0:assertion";
        public string Token { get; private set; }
        public string TokenType { get; private set; }

        public static WsTrustResponse CreateFromResponse(string response, WsTrustVersion version)
        {
            XDocument responseDocument = XDocument.Parse(response, LoadOptions.PreserveWhitespace); // Could throw XmlException
            return CreateFromResponseDocument(responseDocument, version);
        }

        public static string ReadErrorResponse(XDocument responseDocument)
        {
            string errorMessage = null;
            XElement body = responseDocument.Descendants(XmlNamespace.SoapEnvelope + "Body").FirstOrDefault();
            if (body != null)
            {
                XElement fault = body.Elements(XmlNamespace.SoapEnvelope + "Fault").FirstOrDefault();
                if (fault != null)
                {
                    errorMessage = GetFaultMessage(fault);
                }
            }

            // If the parse fails for whatever reason we should include the entire body otherwise
            // there is no indication what went wrong
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                errorMessage = responseDocument.ToString();
            }

            return errorMessage;
        }

        private static string GetFaultMessage(XElement fault)
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
                        return reader.ReadInnerXml();
                    }
                }
            }

            return null;
        }

        internal static WsTrustResponse CreateFromResponseDocument(XDocument responseDocument, WsTrustVersion version)
        {
            Dictionary<string, string> tokenResponseDictionary = new Dictionary<string, string>();

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

            if (!parseResponse)
            {
                return null;
            }

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

                var token = new System.Text.StringBuilder();
                foreach (var node in requestedSecurityToken.Nodes())
                {
                    // Since we moved from XDocument.Load(..., LoadOptions.None) to Load(..., LoadOptions.PreserveWhitespace),
                    // requestedSecurityToken can contain multiple nodes, and the first node is possibly just whitespaces e.g. "\n   ",
                    // so we concatenate all the sub-nodes to include everything
                    token.Append(node.ToString(SaveOptions.DisableFormatting));
                }

                tokenResponseDictionary.Add(tokenTypeElement.Value, token.ToString());
            }

            if (tokenResponseDictionary.Count == 0)
            {
                return null;
            }

            string tokenType = tokenResponseDictionary.ContainsKey(Saml1Assertion)
                ? Saml1Assertion
                : tokenResponseDictionary.Keys.First();

            WsTrustResponse wsTrustResponse = new WsTrustResponse
            {
                TokenType = tokenType,
                Token = tokenResponseDictionary[tokenType]
            };

            return wsTrustResponse;
        }
    }
}

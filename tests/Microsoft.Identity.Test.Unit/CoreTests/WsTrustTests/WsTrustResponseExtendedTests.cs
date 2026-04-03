// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Xml.Linq;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    public class WsTrustResponseExtendedTests : TestBase
    {
        private const string SoapNs = "http://www.w3.org/2003/05/soap-envelope";
        private const string TrustNs = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
        private const string Trust2005Ns = "http://schemas.xmlsoap.org/ws/2005/02/trust";

        [TestMethod]
        public void ReadErrorResponse_ReturnsFaultReason()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"">
                <s:Body>
                    <s:Fault>
                        <s:Reason>
                            <s:Text>Authentication failed</s:Text>
                        </s:Reason>
                    </s:Fault>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            string error = WsTrustResponse.ReadErrorResponse(doc);

            Assert.AreEqual("Authentication failed", error);
        }

        [TestMethod]
        public void ReadErrorResponse_NoFault_ReturnsFullXml()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"">
                <s:Body>
                    <SomeOtherElement>Content</SomeOtherElement>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            string error = WsTrustResponse.ReadErrorResponse(doc);

            Assert.Contains("SomeOtherElement", error);
            Assert.Contains("Content", error);
        }

        [TestMethod]
        public void ReadErrorResponse_NoBody_ReturnsFullXml()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"">
                <s:Header/>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            string error = WsTrustResponse.ReadErrorResponse(doc);

            Assert.IsNotNull(error);
            Assert.Contains("Envelope", error);
        }

        [TestMethod]
        public void ReadErrorResponse_FaultWithNoReason_ReturnsFullXml()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"">
                <s:Body>
                    <s:Fault>
                        <s:Code><s:Value>SomeCode</s:Value></s:Code>
                    </s:Fault>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            string error = WsTrustResponse.ReadErrorResponse(doc);

            // Falls back to full XML when reason extraction returns null
            Assert.IsNotNull(error);
            Assert.Contains("Envelope", error);
        }

        [TestMethod]
        public void ReadErrorResponse_FaultReasonNoText_ReturnsFullXml()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"">
                <s:Body>
                    <s:Fault>
                        <s:Reason/>
                    </s:Fault>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            string error = WsTrustResponse.ReadErrorResponse(doc);

            Assert.IsNotNull(error);
            Assert.Contains("Envelope", error);
        }

        [TestMethod]
        public void CreateFromResponseDocument_WsTrust13_MissingCollection_ReturnsNull()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponse>
                        <t:TokenType>some:token:type</t:TokenType>
                        <t:RequestedSecurityToken><Token>value</Token></t:RequestedSecurityToken>
                    </t:RequestSecurityTokenResponse>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNull(result, "Should return null when RequestSecurityTokenResponseCollection is missing for WsTrust13");
        }

        [TestMethod]
        public void CreateFromResponseDocument_WsTrust13_ValidResponse()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>
                            <t:RequestedSecurityToken><Assertion>saml1token</Assertion></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNotNull(result);
            Assert.AreEqual(WsTrustResponse.Saml1Assertion, result.TokenType);
            Assert.Contains("saml1token", result.Token);
        }

        [TestMethod]
        public void CreateFromResponseDocument_WsTrust2005_ValidResponse()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{Trust2005Ns}"">
                <s:Body>
                    <t:RequestSecurityTokenResponse>
                        <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>
                        <t:RequestedSecurityToken><Assertion>saml1token</Assertion></t:RequestedSecurityToken>
                    </t:RequestSecurityTokenResponse>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust2005);

            Assert.IsNotNull(result);
            Assert.AreEqual(WsTrustResponse.Saml1Assertion, result.TokenType);
        }

        [TestMethod]
        public void CreateFromResponseDocument_PrefersSaml1Assertion()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:other:token:type</t:TokenType>
                            <t:RequestedSecurityToken><Token>othertoken</Token></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>
                            <t:RequestedSecurityToken><Assertion>saml1token</Assertion></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNotNull(result);
            Assert.AreEqual(WsTrustResponse.Saml1Assertion, result.TokenType);
            Assert.Contains("saml1token", result.Token);
        }

        [TestMethod]
        public void CreateFromResponseDocument_NoSaml1_UsesFirstTokenType()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:custom:type</t:TokenType>
                            <t:RequestedSecurityToken><Token>customtoken</Token></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNotNull(result);
            Assert.AreEqual("urn:custom:type", result.TokenType);
            Assert.Contains("customtoken", result.Token);
        }

        [TestMethod]
        public void CreateFromResponseDocument_SkipsResponsesWithMissingTokenType()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:RequestedSecurityToken><Token>notoken</Token></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:valid:type</t:TokenType>
                            <t:RequestedSecurityToken><Token>validtoken</Token></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNotNull(result);
            Assert.AreEqual("urn:valid:type", result.TokenType);
        }

        [TestMethod]
        public void CreateFromResponseDocument_SkipsResponsesWithMissingRequestedSecurityToken()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:skip:type</t:TokenType>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var doc = XDocument.Parse(xml);
            var result = WsTrustResponse.CreateFromResponseDocument(doc, WsTrustVersion.WsTrust13);

            Assert.IsNull(result, "Should return null when no valid token responses found");
        }

        [TestMethod]
        public void CreateFromResponse_ParsesXmlString()
        {
            string xml = $@"<s:Envelope xmlns:s=""{SoapNs}"" xmlns:t=""{TrustNs}"">
                <s:Body>
                    <t:RequestSecurityTokenResponseCollection>
                        <t:RequestSecurityTokenResponse>
                            <t:TokenType>urn:oasis:names:tc:SAML:1.0:assertion</t:TokenType>
                            <t:RequestedSecurityToken><Assertion>token</Assertion></t:RequestedSecurityToken>
                        </t:RequestSecurityTokenResponse>
                    </t:RequestSecurityTokenResponseCollection>
                </s:Body>
            </s:Envelope>";

            var result = WsTrustResponse.CreateFromResponse(xml, WsTrustVersion.WsTrust13);

            Assert.IsNotNull(result);
            Assert.AreEqual(WsTrustResponse.Saml1Assertion, result.TokenType);
        }
    }
}

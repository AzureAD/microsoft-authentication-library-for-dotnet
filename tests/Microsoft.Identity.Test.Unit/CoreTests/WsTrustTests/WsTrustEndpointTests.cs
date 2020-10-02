// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.WsTrust;
using Microsoft.Identity.Test.Common;
using Microsoft.Identity.Test.Common.Core.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.CoreTests.WsTrustTests
{
    [TestClass]
    public class WsTrustEndpointTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        private readonly Uri _uri = new Uri("https://windowsorusernamepasswordendpointurl");
        private readonly string _cloudAudienceUri = "https://cloudAudienceUrn";

        private readonly string _username = "the_username";
        private readonly string _password = "the_password";

        // October 2, 2018 at 10:15:30
        private readonly TestTimeService _testTimeService = new TestTimeService(new DateTime(2018, 10, 2, 10, 15, 30));

        // there should be one GUID values in the document.  so we'll make sure it only pulls out two.
        private readonly Guid _guid1 = new Guid("b052e0d8-349c-4d73-9ddb-0782043a440e");
        private readonly Guid _guid2 = new Guid("d9d9bd71-1cfd-4dbf-8d9d-0bc5cfdbbe72");

        [TestMethod]
        public void TestWsTrustEndpointWindowsIntegratedAuthWsTrust2005()
        {
            const string expectedMessage =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:wsa=\"http://www.w3.org/2005/08/addressing\" " +
                "xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" " +
                "xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Header><wsa:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</wsa:Action>" +
                "<wsa:messageID>urn:uuid:b052e0d8-349c-4d73-9ddb-0782043a440e</wsa:messageID>" +
                "<wsa:ReplyTo><wsa:Address>http://www.w3.org/2005/08/addressing/anonymous</wsa:Address>" +
                "</wsa:ReplyTo><wsa:To s:mustUnderstand=\"1\">https://windowsorusernamepasswordendpointurl/</wsa:To>" +
                "</s:Header><s:Body><wst:RequestSecurityToken xmlns:wst=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">" +
                "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\"><wsa:EndpointReference>" +
                "<wsa:Address>https://cloudAudienceUrn</wsa:Address></wsa:EndpointReference></wsp:AppliesTo>" +
                "<wst:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</wst:KeyType>" +
                "<wst:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</wst:RequestType>" +
                "</wst:RequestSecurityToken></s:Body></s:Envelope>";

            var guidFactory = new TestGuidQueueFactory(new List<Guid> { _guid1 });
            var wsTrustEndpoint = new WsTrustEndpoint(_uri, WsTrustVersion.WsTrust2005, _testTimeService, guidFactory);

            string requestMessage = wsTrustEndpoint.BuildTokenRequestMessageWindowsIntegratedAuth(_cloudAudienceUri);
            CheckStringEqualityAtEachCharacter(expectedMessage, requestMessage);
            Assert.AreEqual(expectedMessage, requestMessage);
        }

        [TestMethod]
        public void TestWsTrustEndpointWindowsIntegratedAuthWsTrust13()
        {
            const string expectedMessage =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:wsa=\"http://www.w3.org/2005/08/addressing\" " +
                "xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" " +
                "xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Header><wsa:Action s:mustUnderstand=\"1\">http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</wsa:Action>" +
                "<wsa:messageID>urn:uuid:b052e0d8-349c-4d73-9ddb-0782043a440e</wsa:messageID>" +
                "<wsa:ReplyTo><wsa:Address>http://www.w3.org/2005/08/addressing/anonymous</wsa:Address>" +
                "</wsa:ReplyTo><wsa:To s:mustUnderstand=\"1\">https://windowsorusernamepasswordendpointurl/</wsa:To>" +
                "</s:Header><s:Body><wst:RequestSecurityToken xmlns:wst=\"http://docs.oasis-open.org/ws-sx/ws-trust/200512\">" +
                "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\"><wsa:EndpointReference>" +
                "<wsa:Address>https://cloudAudienceUrn</wsa:Address></wsa:EndpointReference></wsp:AppliesTo>" +
                "<wst:KeyType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer</wst:KeyType>" +
                "<wst:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</wst:RequestType>" +
                "</wst:RequestSecurityToken></s:Body></s:Envelope>";

            var guidFactory = new TestGuidQueueFactory(new List<Guid> { _guid1 });
            var wsTrustEndpoint = new WsTrustEndpoint(_uri, WsTrustVersion.WsTrust13, _testTimeService, guidFactory);

            string requestMessage = wsTrustEndpoint.BuildTokenRequestMessageWindowsIntegratedAuth(_cloudAudienceUri);
            Assert.AreEqual(expectedMessage, requestMessage);
        }

        [TestMethod]
        public void TestWsTrustEndpointUsernamePasswordWsTrust2005()
        {
            const string expectedMessage =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:wsa=\"http://www.w3.org/2005/08/addressing\" " +
                "xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" " +
                "xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Header><wsa:Action s:mustUnderstand=\"1\">http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue</wsa:Action>" +
                "<wsa:messageID>urn:uuid:b052e0d8-349c-4d73-9ddb-0782043a440e</wsa:messageID>" +
                "<wsa:ReplyTo><wsa:Address>http://www.w3.org/2005/08/addressing/anonymous</wsa:Address>" +
                "</wsa:ReplyTo><wsa:To s:mustUnderstand=\"1\">https://windowsorusernamepasswordendpointurl/</wsa:To>" +
                "<wsse:Security s:mustUnderstand=\"1\" xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">" +
                "<wsu:Timestamp wsu:Id=\"MSATimeStamp\"><wsu:Created>2018-10-02T10:15:30.068Z</wsu:Created>" +
                "<wsu:Expires>2018-10-02T10:25:30.068Z</wsu:Expires></wsu:Timestamp>" +
                "<wsse:UsernameToken wsu:Id=\"UnPwSecTok2005-d9d9bd71-1cfd-4dbf-8d9d-0bc5cfdbbe72\">" +
                "<wsse:Username>the_username</wsse:Username><wsse:Password>the_password</wsse:Password></wsse:UsernameToken></wsse:Security>" +
                "</s:Header><s:Body><wst:RequestSecurityToken xmlns:wst=\"http://schemas.xmlsoap.org/ws/2005/02/trust\">" +
                "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\"><wsa:EndpointReference>" +
                "<wsa:Address>https://cloudAudienceUrn</wsa:Address></wsa:EndpointReference></wsp:AppliesTo>" +
                "<wst:KeyType>http://schemas.xmlsoap.org/ws/2005/05/identity/NoProofKey</wst:KeyType>" +
                "<wst:RequestType>http://schemas.xmlsoap.org/ws/2005/02/trust/Issue</wst:RequestType>" +
                "</wst:RequestSecurityToken></s:Body></s:Envelope>";

            var guidFactory = new TestGuidQueueFactory(new List<Guid> { _guid1, _guid2 });
            var wsTrustEndpoint = new WsTrustEndpoint(_uri, WsTrustVersion.WsTrust2005, _testTimeService, guidFactory);

            string requestMessage = wsTrustEndpoint.BuildTokenRequestMessageUsernamePassword(_cloudAudienceUri, _username, _password);
            Assert.AreEqual(expectedMessage, requestMessage);
        }

        [TestMethod]
        public void TestWsTrustEndpointUsernamePasswordWsTrust13()
        {
            const string expectedMessage =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<s:Envelope xmlns:wsa=\"http://www.w3.org/2005/08/addressing\" " +
                "xmlns:wsu=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd\" " +
                "xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\">" +
                "<s:Header><wsa:Action s:mustUnderstand=\"1\">http://docs.oasis-open.org/ws-sx/ws-trust/200512/RST/Issue</wsa:Action>" +
                "<wsa:messageID>urn:uuid:b052e0d8-349c-4d73-9ddb-0782043a440e</wsa:messageID>" +
                "<wsa:ReplyTo><wsa:Address>http://www.w3.org/2005/08/addressing/anonymous</wsa:Address>" +
                "</wsa:ReplyTo><wsa:To s:mustUnderstand=\"1\">https://windowsorusernamepasswordendpointurl/</wsa:To>" +
                "<wsse:Security s:mustUnderstand=\"1\" xmlns:wsse=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\">" +
                "<wsu:Timestamp wsu:Id=\"MSATimeStamp\"><wsu:Created>2018-10-02T10:15:30.068Z</wsu:Created>" +
                "<wsu:Expires>2018-10-02T10:25:30.068Z</wsu:Expires></wsu:Timestamp>" +
                "<wsse:UsernameToken wsu:Id=\"UnPwSecTok13-d9d9bd71-1cfd-4dbf-8d9d-0bc5cfdbbe72\">" +
                "<wsse:Username>the_username</wsse:Username><wsse:Password>the_password</wsse:Password></wsse:UsernameToken></wsse:Security>" +
                "</s:Header><s:Body><wst:RequestSecurityToken xmlns:wst=\"http://docs.oasis-open.org/ws-sx/ws-trust/200512\">" +
                "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\"><wsa:EndpointReference>" +
                "<wsa:Address>https://cloudAudienceUrn</wsa:Address></wsa:EndpointReference></wsp:AppliesTo>" +
                "<wst:KeyType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Bearer</wst:KeyType>" +
                "<wst:RequestType>http://docs.oasis-open.org/ws-sx/ws-trust/200512/Issue</wst:RequestType>" +
                "</wst:RequestSecurityToken></s:Body></s:Envelope>";

            var guidFactory = new TestGuidQueueFactory(new List<Guid> { _guid1, _guid2 });
            var wsTrustEndpoint = new WsTrustEndpoint(_uri, WsTrustVersion.WsTrust13, _testTimeService, guidFactory);

            string requestMessage = wsTrustEndpoint.BuildTokenRequestMessageUsernamePassword(_cloudAudienceUri, _username, _password);
            Assert.AreEqual(expectedMessage, requestMessage);
        }

        // If there's ever a bug where these fail, this will help to debug the specific character that's missing
        // since this payload is ~1k of data.
        private void CheckStringEqualityAtEachCharacter(string expected, string actual)
        {
            int maxLength = Math.Max(expected.Length, actual.Length);
            for (int i = 0; i < maxLength; i++)
            {
                if (expected[i] != actual[i])
                {
                    Assert.IsFalse(true);
                }
            }
        }
    }
}

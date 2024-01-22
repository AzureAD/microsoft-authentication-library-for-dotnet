// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if NETFRAMEWORK
using Microsoft.Identity.Client.Kerberos;
using Microsoft.Identity.Client.Utils;
using Microsoft.Identity.Test.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.Kerberos
{
    /// <summary>
    /// For an overview of Kerberos and testing please see: 
    /// https://identitydivision.visualstudio.com/IdentityWiki/_wiki/wikis/IdentityWiki.wiki/20501/-AADK-AAD-Kerberos
    /// https://identitydivision.visualstudio.com/IdentityWiki/_wiki/wikis/IdentityWiki.wiki/20601/AAD-Kerberos-for-MSAL 
    /// </summary>
    [TestClass]
    public class KerberosSupplementalTicketManagerTests
    {
        /// <summary>
        /// Service principal name for testing.
        /// </summary>
        private static readonly string _testServicePrincipalName = "HTTP/prod.aadkreberos.msal.com";

        /// <summary>
        /// Username within the ID token.
        /// </summary>
        private static readonly string _testClientName = "localAdmin@aadktest.onmicrosoft.com";

        /// <summary>
        /// Sample ID Token without Kerbero Service Ticket.
        /// </summary>
        private static readonly string _testIdToken =
            "eyJ0eXAiOiJKV1QiLCJyaCI6IjAuQWdBQXI0R0lRckdhczBDQldEWVJOWV9fYUlLMElWSlJKck5NbXRqQW1uamszcDRzQU5NLiIsImFsZyI6IlJTMjU2"
            + "Iiwia2lkIjoibk9vM1pEck9EWEVLMWpLV2hYc2xIUl9LWEVnIn0.eyJhdWQiOiI1MjIxYjQ4Mi0yNjUxLTRjYjMtOWFkOC1jMDlhNzhlNGRlOWUiLCJp"
            + "c3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNDI4ODgxYWYtOWFiMS00MGIzLTgxNTgtMzYxMTM1OGZmZjY4L3YyLjAiLCJpYXQi"
            + "OjE2MjAwNjU1NjksIm5iZiI6MTYyMDA2NTU2OSwiZXhwIjoxNjIwMDY5NDY5LCJhaW8iOiJBVFFBeS84VEFBQUErU2NsWnFRQ1hWeFUvTER2Zi9MbDMv"
            + "bk8rRGlJczFNek1NbTdoYUNoeEdIa1MycFFVaVBEVlFzd0Qyd1Vvd2t5IiwibmFtZSI6ImxvY2FsQWRtaW4iLCJvaWQiOiIyZGEyYmNmZi03YzVmLTQ4"
            + "OTEtYTFlNC1kYTU1Yjg2NmNjZDgiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJsb2NhbEFkbWluQGFhZGt0ZXN0Lm9ubWljcm9zb2Z0LmNvbSIsInJoIjoi"
            + "SSIsInN1YiI6IkZGbjhTTENyZXdEcDdBTEZ0OF9fUnNkUWJHV0VLOV9JbzhHcW1QTWVVX1EiLCJ0aWQiOiI0Mjg4ODFhZi05YWIxLTQwYjMtODE1OC0z"
            + "NjExMzU4ZmZmNjgiLCJ1dGkiOiJTNDJOTUlVQkMwU0RhVkxtQVJBZkFBIiwidmVyIjoiMi4wIn0.LONtuRER9-wKeaAkH7qfqrWPd6fZNEC4KqibSewb"
            + "xz1bwscP37_HQs-mEAKcK20txLgnHhyBG9JESllnWrEhEjrRYwYhWMN9NxlZCaMm2elFh-CfMBNHxTRFcQaHKEATN07gNZmEFLHOTDHn9s1wmSLIHpM7"
            + "UzMdLY9ifSWRcBesmi4kv3VVPHuMP8PruO0jQIVkDUyuEs9BvHh1mvO2cpmR_q2ICpMnREUd2KrrM8PU3yDkmhxIZpXwwDO_MGNFyt4hMlAY01qTiT2V"
            + "G7KmTjWnsxUZq3ozyZWiSYAMgmbDqEPs0dYwniV0HnR4MTvpkoOPc3ohowUve-qNRT8brQ";

        /// <summary>
        /// Sample ID token sample with Kerberos Service Ticket.
        /// </summary>
        private static readonly string _testIdTokenWithKerberosTicketClaim =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI1MjIxYjQ4Mi0yNjUxLTRj"
            + "YjMtOWFkOC1jMDlhNzhlNGRlOWUiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNDI4ODgxYWYtOWFiMS00MGIzLTgxNTgtMz"
            + "YxMTM1OGZmZjY4L3YyLjAiLCJpYXQiOjE2MTk4MTg1MTgsIm5iZiI6MTYxOTgxODUxOCwiZXhwIjoxNjE5ODIyNDE4LCJhaW8iOiJBVFFBeS84VEFBQUFP"
            + "em02eVhVWVoxM0xCV0l0ak14KytaMzVLZ3NiSlJJYkpVdzY1em9uWXRaR1dscWlWbXJjcXBwUkpoRDc5bGpRIiwibmFtZSI6ImxvY2FsQWRtaW4iLCJvaW"
            + "QiOiIyZGEyYmNmZi03YzVmLTQ4OTEtYTFlNC1kYTU1Yjg2NmNjZDgiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJsb2NhbEFkbWluQGFhZGt0ZXN0Lm9ubWlj"
            + "cm9zb2Z0LmNvbSIsInJoIjoiMC5BU3dBcjRHSVFyR2FzMENCV0RZUk5ZX19hSUswSVZKUkpyTk1tdGpBbW5qazNwNHNBTk0uIiwic3ViIjoiRkZuOFNMQ3"
            + "Jld0RwN0FMRnQ4X19Sc2RRYkdXRUs5X0lvOEdxbVBNZVVfUSIsInRpZCI6IjQyODg4MWFmLTlhYjEtNDBiMy04MTU4LTM2MTEzNThmZmY2OCIsInV0aSI6"
            + "InM3V3lid29PN2tLUEpQNS1iZTFSQWciLCJ2ZXIiOiIyLjAiLCJ4bXNfYXNfcmVwIjoie1wia2V5VHlwZVwiOjE4LFwibWVzc2FnZUJ1ZmZlclwiOlwiZG"
            + "9JSEFUQ0NCdjJnQXdJQkJhRURBZ0VXb29JRmlqQ0NCWVpoZ2dXQ01JSUZmcUFEQWdFRm9SNGJIRXRGVWtKRlVrOVRMazFKUTFKUFUwOUdWRTlPVEVsT1JT"
            + "NURUMDJpTERBcW9BTUNBUUtoSXpBaEd3UklWRlJRR3hsd2NtOWtMbUZoWkd0eVpXSmxjbTl6TG0xellXd3VZMjl0bzRJRkp6Q0NCU09nQXdJQkVxS0NCUm"
            + "9FZ2dVV05JWklyd29jaEx5NHBaS3ZUbmZPcTZpWUJGd0l5dFFseXF0R2N6a1d2V2RMcFhzN3JMeUsxUmhKK2FOMHBLUXlPWXFBZ1YwTFdOekYzaGJQYWgz"
            + "aGMyd01yZUxmb25Gdnl3akVrNisva2w4Q0VEbGxhL0hlV2ZRL0paYWNJKzVkZDBRM3B2NzBHOVBOMnRpTFNtVnF6UXZoTUtpTUVHTk5JTDk3dWIrRzhOVW"
            + "tkT25RSUNTVXdsNXdyTUQ2anNjK0N2M2cvSVdIZm53d2htQUFqa3EyTjNzUkU2SWN6bTdGSW1uSEtLZE5YU1Y3WDdRc1l4cE1DdWYrOVhqRVZmQmRyTmF0"
            + "bFNxaE43N3crc3NWS25odFZQdm9YZVFHUUJtVkgyWGVPVDFyVjFwRDFuZDV3TmVBdkJPdzg0Y1pSNFBud05KVCtJZ0F4alJOR0Izbjc4c2pZQyt4MFlJbG"
            + "FoWHQ2VFVvU2Q5U3Q2UWFNbjdhWmxlenNIa0FYVVljUlpCSGxsUmpqQ3o0SGpmWWpGTjF1cFo5Ymo5TVhINXJJL1Yrbm52ZzJ2d284RjU4N2RyMGJQYUVT"
            + "cVUvVytwUGNJTWs2UUlwOTBVc3Q4bVJ3NDhTRjhPY0k2QjQwZEgvKzU3UmlSTDMxUERiTUZhMi9BOXIzSjhJSGFiUndpQm1hYk5Ld2pOVlExQXhpWjR3Yk"
            + "JpNWFXZkdjdFJCY1dvU2FOY2l3bFUvYWVYWGZuRXFObzlsN2pmeXJLOWRRQ1RIOTYvdGZVdDAyMzdBR0pYZnNjVUhhQU5McnFodUhMZnJLeElwRUFFNkdW"
            + "RTJBdW5sTjRsSVNtaXoyWVo2aVYvZmVpSXdFVVloa0cybFVxY2ZJbTVYNFNJbE9BYnkwOUJQMTRrVjlUc0Q5ZXBlNDMyaHR1dE1YTEs0TUVDSVlndjBFYT"
            + "hTOGk2R2tINmk1WWJOOXBZbVUvenJtQStlaXROQ1Q1ZEFUdHRBSHpIbHhZMVFxWU1pMU0yd2Y4MEJCOVFGcGFBS0NyeEx6T0dRMmFIeGNEb3RIUk53MTZl"
            + "RlZCSHdFN3pubUw2ZlRkRWhFK1dKZG1hMnNMcUVJdWNraDdJayt1R09ieldwZU1oNklKa0tYT0FOS05EbERUd2ZEMDNlbEg5SFNFUjZ2Tk0rbjdwdExDZX"
            + "RoZFYzcWs2bFIwSzVvODlqeUEweFBwMUpxdFhWQVMvRXMyd1BzN01tczh2UXZ1ZlBPUVVyeHJFWk1xLzlYZDA2WlRMY0xTQXZBbXRzL2ZJb05ZZUxCK0Vj"
            + "V1A0UitwemdaL3RQS3ZFT1VJVUNQTVJlUnY0cDFlZmpjQm51NlZyUU9vSGdPRkFDblZHSnZZN0tNMk9iU1JxK3hxVkVnL3dock1QbUdUTEtJTkFWYkpOQW"
            + "44V2MrTVRRZFc0M1dHNjRRRDMxTjVVZnJWY29Sc1dSb0dwYWM3UmNYb1d4cVliaHJIV29lRWRMR3Z6eTZ2Nm9FREt2S2VWZndnS3FKQ3g3QnVweFBuamlM"
            + "RW56UXdBdDFRNVByamFFTDNFY2hQN0VJWkVmczBudnNpTUo2ZFp0VWovMHNuVHhieXQyRDB1L2JlVitKbm1tT1ZNa2FXS3FJWjRvQnJwWit2SS9KcFc4dU"
            + "o3Z0N0YWJjM0RpZUxPRm5HY0NHODltVHdWczhEYlBuSVlzTVE2dm9neE5QZUpyYnJyQVVCS2w2NU41TmhJeGVpQjVoMGI1dVJFKzlBaWZpd1ViOERUYzdr"
            + "Z2x4b094NEk0MnFrOEFZZ0lWR0xuNjVRU25rT1AyVThBNkpVdVNNeTBVRHdxbzNrUEN2dFJJUE9ybHpTUkRpMG5KeStMcHQzZEtyTjNlRFJJeE1rcm1jZV"
            + "NxRnVDcGgwMkV3Lzc2eVQwazJnYXVrTWk1V2lCb2NRM3lnbm9nY1hRNnlydzYwanJVcmU3UHZTcWpGeGxlTHg3OEc0bVBEaWw0NWluVTdtd09GRHk3Visx"
            + "Tkw4OW9KampBdll6TEwweFovaUFVbDdIQjF6OXZSSW5DUWcrUHRPeTNKYW0vK29nL2hmbjAzQnJQakxvcU1oR1cvZlBxd2h4NHhsQTVRMEgzRWk5MHh6Sn"
            + "FmUHREbHN3WHp4aFNsaXB5UW81LzFJTHdlUlY0M3Q1YjV5K2lma1NveXlWaTdCWHZiTDZZZVVxeTlMNHY4Wjl2N2hKUkEzTEpKR2xTZjgzNVJ2STgzR0lz"
            + "aFVyVUVCS2t2TGhxNStZV09GWlA2UzYrN2lScXl0dUw2a3E4Mm9Ddjl0SFpzWUN2ejByNng1KzMvWXNNbzRJQllUQ0NBVjJnQXdJQkFLS0NBVlFFZ2dGUW"
            + "ZZSUJURENDQVVpZ2dnRXFNSUlCSmpDQ0FTS2dLekFwb0FNQ0FSS2hJZ1FnR1lCbzg5bndwblgxeEphRW82aCs2QmlrRlFad2ZEYXFqT1dmUG0wNXFOaWhI"
            + "aHNjUzBWU1FrVlNUMU11VFVsRFVrOVRUMFpVVDA1TVNVNUZMa05QVGFJd01DNmdBd0lCQWFFbk1DVWJJMnh2WTJGc1FXUnRhVzVBWVdGa2EzUmxjM1F1Yj"
            + "I1dGFXTnliM052Wm5RdVkyOXRvd2NEQlFBQWdBQUFwQkVZRHpJd01qRXdORE13TWpFME1ERTVXcVVSR0E4eU1ESXhNRFF6TURJeE5EQXhPVnFtRVJnUE1q"
            + "QXlNVEEwTXpBeU1qUXdNVGxhcHhFWUR6SXdNakV3TlRBM01qRTBNREU1V3FnZUd4eExSVkpDUlZKUFV5NU5TVU5TVDFOUFJsUlBUa3hKVGtVdVEwOU5xU3"
            + "d3S3FBREFnRUNvU013SVJzRVNGUlVVQnNaY0hKdlpDNWhZV1JyY21WaVpYSnZjeTV0YzJGc0xtTnZiYUlSR0E4eU1ESXhNRFF6TURJeE5EQXhPVnFqQlFJ"
            + "RDlIZUxcIixcInJlYWxtXCI6XCJLRVJCRVJPUy5NSUNST1NPRlRPTkxJTkUuQ09NXCIsXCJzblwiOlwiSFRUUC9wcm9kLmFhZGtyZWJlcm9zLm1zYWwuY2"
            + "9tXCIsXCJjblwiOlwibG9jYWxBZG1pbkBhYWRrdGVzdC5vbm1pY3Jvc29mdC5jb21cIixcImFjY291bnRUeXBlXCI6Mn0ifQ.j3LGWzeEDAmzJrRXSWK41"
            + "HACEAIPr5g3j7Df0xC2V0FszD9e8GgC_GjNFhaSl0uNXzPoKnI7zwl90zlvJNx4NUh-ZzBbY59JDL6B2o1i9Mb-K3KrGJLRf6s1Mp1Z2lFve6d57eri3EF"
            + "P0lxMESvknYs0zk9Z9yTDxdadAO9R46mrJhPcZSpuip6yexOeT-XoxRZwIdOZVMd1EwXao26q_3BeQ3N19kbkv6Dr9EPCT36_1sTzytcHBein9h4Yixmk9"
            + "sPtueCF3vqdO5Yl3Q0bBrksqFelwZB8sxz9y1vOQ5cfraYJc6JkWRiRy26YFrZe2UnuBGV2ss_1sSm7aE1gaw";

        [TestInitialize]
        public void TestInit()
        {
            TestCommon.ResetInternalStaticCaches();
        }

        [TestMethod]
        public void FromIdToken_WithKerberosTicket()
        {
            KerberosSupplementalTicket ticket = KerberosSupplementalTicketManager.FromIdToken(_testIdTokenWithKerberosTicketClaim);

            Assert.IsNotNull(ticket);
            Assert.IsTrue(string.IsNullOrEmpty(ticket.ErrorMessage));
            Assert.IsFalse(string.IsNullOrEmpty(ticket.KerberosMessageBuffer));
            Assert.AreEqual(_testServicePrincipalName, ticket.ServicePrincipalName, "Service principal name is not matched.");
            Assert.AreEqual(_testClientName, ticket.ClientName, "Client name is not matched.");
        }

        [TestMethod]
        public void FromIdToken_WithoutKerberosTicket()
        {
            KerberosSupplementalTicket ticket = KerberosSupplementalTicketManager.FromIdToken(_testIdToken);

            Assert.IsNull(ticket);
        }

        [TestMethod]
        public void GetKrbCred()
        {
            KerberosSupplementalTicket ticket = KerberosSupplementalTicketManager.FromIdToken(_testIdTokenWithKerberosTicketClaim);
            byte[] krbCred = KerberosSupplementalTicketManager.GetKrbCred(ticket);

            Assert.IsNotNull(krbCred);
        }

        [TestMethod]
        public void GetKerberosTicketClaim_IdToken()
        {
            string kerberosClaim
                = KerberosSupplementalTicketManager.GetKerberosTicketClaim(_testServicePrincipalName, KerberosTicketContainer.IdToken);

            Assert.IsFalse(string.IsNullOrEmpty(kerberosClaim));
            //JsonHelper.DeserializeFromJson<JObject>(kerberosClaim);

            //JsonObject claim = JsonObject.Parse(kerberosClaim);
            //Assert.IsNotNull(claim);

            //Assert.IsTrue(claim.ContainsKey("id_token"));
            //JsonObject idToken = claim.GetNamedObject("id_token");
            //Assert.IsNotNull(idToken);

            //CheckKerberosClaim(idToken);
        }

        //[TestMethod]
        //public void GetKerberosTicketClaim_AccessToken()
        //{
        //    string kerberosClaim
        //        = KerberosSupplementalTicketManager.GetKerberosTicketClaim(_testServicePrincipalName, KerberosTicketContainer.AccessToken);

        //    Assert.IsFalse(string.IsNullOrEmpty(kerberosClaim));

        //    JsonObject claim = JsonObject.Parse(kerberosClaim);
        //    Assert.IsNotNull(claim);

        //    Assert.IsTrue(claim.ContainsKey("access_token"));
        //    JsonObject accessToken = claim.GetNamedObject("access_token");
        //    Assert.IsNotNull(accessToken);

        //    CheckKerberosClaim(accessToken);
        //}

        //private void CheckKerberosClaim(JsonObject claim)
        //{
        //    Assert.IsTrue(claim.ContainsKey("xms_as_rep"));
        //    JsonObject asRep = claim.GetNamedObject("xms_as_rep");
        //    Assert.IsNotNull(asRep);

        //    Assert.IsTrue(asRep.ContainsKey("essential"));
        //    Assert.AreEqual("false", asRep.GetNamedString("essential"), "essential field is not matched.");

        //    Assert.IsTrue(asRep.ContainsKey("value"));
        //    Assert.AreEqual(_testServicePrincipalName, asRep.GetNamedString("value"), "Service principal name is not matched.");
        //}
    }
}
#endif

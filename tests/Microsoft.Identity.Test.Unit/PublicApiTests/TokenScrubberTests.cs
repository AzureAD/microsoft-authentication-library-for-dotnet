// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Identity.Client.Internal.Logger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.PublicApiTests
{
    [TestClass]
    public class TokenScrubberTests
    {
        private const string Offset0 = "RXZvU3RzQXJ0aWZhY3Rz";
        private const string Offset1 = "V2b1N0c0FydGlmYWN0c";
        private const string Offset2 = "dm9TdHNBcnRpZmFjdH";
        private const string MsaLiteral = "MsaArtifacts";

        [TestMethod]
        [DataRow(Offset0)]
        [DataRow(Offset1)]
        [DataRow(Offset2)]
        public void Scrub_EstsOffsetPattern_RedactsToken(string pattern)
        {
            // Arrange
            string message = $"Header AQABAAEAAAD{pattern}bodybodybodyTOKEN== trailing text";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(pattern, result);
            Assert.Contains(TokenScrubber.Placeholder, result);
            Assert.AreEqual("Header [Redacted opaque token] trailing text", result);
        }

        [TestMethod]
        public void Scrub_MsaLiteral_RedactsToken()
        {
            // Arrange
            string message = $"prefix ABCdef{MsaLiteral}12345 suffix";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(MsaLiteral, result);
            Assert.AreEqual("prefix [Redacted opaque token] suffix", result);
        }

        [TestMethod]
        public void Scrub_TokenInsideJson_RedactsToken()
        {
            // Arrange
            string message = $"{{\"access_token\":\"eyJ0{Offset0}abc-DEF_123\",\"expires_in\":3600}}";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.AreEqual("{\"access_token\":\"[Redacted opaque token]\",\"expires_in\":3600}", result);
        }

        [TestMethod]
        public void Scrub_SetCookieEsctxLine_RedactsToken()
        {
            // Arrange
            string message = "Set-Cookie: esctx-uYBOwwKr8Sg=AQABCQEAAAAdDD7nC9b5Q7JPd_okEQRFRXZvU3RzQXJ0aWZhY3RzDQAAAAAAwEvgY46VLqZk6xV4zJodl7RTZLwd1OotrpcUmu2Sk7nGwPKY--1D1oJptRZTR32ppc7bucqiQCsEY8qoYH56_5009mgGcrTkn4mQwWddzehxJCyoLjD2jNT322cq4JDFbyAXA7e7q_V1MtQ_kqa0hyAA; domain=.login.microsoftonline.com; path=/; secure; HttpOnly; SameSite=None";
            string expected = "Set-Cookie: [Redacted opaque token]; domain=.login.microsoftonline.com; path=/; secure; HttpOnly; SameSite=None";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.AreEqual(expected, result);
            Assert.DoesNotContain(Offset0, result);
        }

        [TestMethod]
        public void Scrub_OrdinaryLogLine_ReturnsSameReference()
        {
            // Arrange
            string message = "2024-01-01 MSAL 4.0.0 acquiring token for scope User.Read";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.AreSame(message, result);
        }

        [TestMethod]
        public void Scrub_LookAlikeString_NotRedacted()
        {
            // Arrange - contains the substring "Artifacts" and base64-ish text, but no tagged pattern.
            string message = "Loading build Artifacts from RXZvU3Rz cache directory";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.AreSame(message, result);
            Assert.DoesNotContain(TokenScrubber.Placeholder, result);
        }

        [TestMethod]
        public void Scrub_MultipleTokens_AllRedacted()
        {
            // Arrange
            string message = $"first AAA{Offset0}BBB then second CCC{MsaLiteral}DDD end";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.DoesNotContain(MsaLiteral, result);
            Assert.AreEqual("first [Redacted opaque token] then second [Redacted opaque token] end", result);
        }
    }
}

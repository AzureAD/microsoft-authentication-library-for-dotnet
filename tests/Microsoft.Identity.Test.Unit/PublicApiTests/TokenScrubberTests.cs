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

        [TestMethod]
        public void Scrub_TokenAtStartOfString_RedactsToken()
        {
            // Arrange
            string message = $"AAABBB{Offset1}CCCddd rest of line";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset1, result);
            Assert.AreEqual("[Redacted opaque token] rest of line", result);
        }

        [TestMethod]
        public void Scrub_TokenAtEndOfString_RedactsToken()
        {
            // Arrange
            string message = $"log line ends with token AAABBB{Offset2}CCCddd==";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset2, result);
            Assert.AreEqual("log line ends with token [Redacted opaque token]", result);
        }

        [TestMethod]
        public void Scrub_EntireStringIsToken_RedactsToken()
        {
            // Arrange
            string message = $"AAABBB{Offset0}CCCddd==";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.AreEqual(TokenScrubber.Placeholder, result);
        }

        [TestMethod]
        public void Scrub_Base64UrlToken_RedactsAcrossDashAndUnderscore()
        {
            // Arrange - base64url token using '-' and '_' on both sides of the tag. Left expansion
            // crosses '=' (a token char), so the leading "token=" is folded into the redaction.
            string message = $"token=ab-cd_ef{Offset0}gh-ij_kl-mn end";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.AreEqual("[Redacted opaque token] end", result);
        }

        [TestMethod]
        public void Scrub_BearerAuthorizationHeader_RedactsToken()
        {
            // Arrange
            string message = $"Authorization: Bearer eyJhbGci{Offset0}payloadPart-signature_part after";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.AreEqual("Authorization: Bearer [Redacted opaque token] after", result);
        }

        [TestMethod]
        public void Scrub_MultipleSetCookieTokensOnOneLine_RedactsAll()
        {
            // Arrange - two tagged esctx cookies on one folded header line, an untagged fpc cookie in between.
            string message = $"Set-Cookie: esctx-A=hdr{Offset0}body; path=/, fpc=Ag2n43XSImFA; expires=x, Set-Cookie: esctx-B=hdr{Offset1}body; path=/";
            string expected = "Set-Cookie: [Redacted opaque token]; path=/, fpc=Ag2n43XSImFA; expires=x, Set-Cookie: [Redacted opaque token]; path=/";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.DoesNotContain(Offset1, result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Scrub_AdjacentTokensBackToBack_RedactsBoth()
        {
            // Arrange - two token runs separated only by a single space.
            string message = $"AAA{Offset0}BBB {Offset1}CCC";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.DoesNotContain(Offset1, result);
            Assert.AreEqual("[Redacted opaque token] [Redacted opaque token]", result);
        }

        [TestMethod]
        public void Scrub_MixedEstsAndMsaTokens_RedactsAll()
        {
            // Arrange - all four detection patterns in a single blob.
            string message = $"a AAA{Offset0}xxx b BBB{Offset1}yyy c CCC{Offset2}zzz d DDD{MsaLiteral}www e";
            string expected = "a [Redacted opaque token] b [Redacted opaque token] c [Redacted opaque token] d [Redacted opaque token] e";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.DoesNotContain(Offset1, result);
            Assert.DoesNotContain(Offset2, result);
            Assert.DoesNotContain(MsaLiteral, result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Scrub_TokenInsideHttpResponseErrorBlob_RedactsToken()
        {
            // Arrange - simulates a whole HttpResponseMessage embedded inside an error string.
            string message =
                "MsalServiceException: StatusCode=200, ResponseBody={StatusCode: 200, " +
                $"Headers:{{ Set-Cookie: esctx-x=AQAB{Offset0}Q_kqa0hyAA; path=/ }}, " +
                "Content: text/html }";
            string expected =
                "MsalServiceException: StatusCode=200, ResponseBody={StatusCode: 200, " +
                "Headers:{ Set-Cookie: [Redacted opaque token]; path=/ }, " +
                "Content: text/html }";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(Offset0, result);
            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void Scrub_NullInput_ReturnsNull()
        {
            // Act
            string result = TokenScrubber.Scrub(null);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Scrub_EmptyInput_ReturnsSameReference()
        {
            // Arrange
            string message = string.Empty;

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.AreSame(message, result);
        }

        [TestMethod]
        public void Scrub_MsaLiteralLookAlikeWithoutTokenChars_StillRedactsLiteralOnly()
        {
            // Arrange - the MSA literal surrounded by non-token characters (spaces) redacts just the literal.
            string message = $"value is {MsaLiteral} here";

            // Act
            string result = TokenScrubber.Scrub(message);

            // Assert
            Assert.DoesNotContain(MsaLiteral, result);
            Assert.AreEqual("value is [Redacted opaque token] here", result);
        }
    }
}
